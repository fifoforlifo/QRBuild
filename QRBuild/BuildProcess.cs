using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using QRBuild.IO;
using QRBuild.Linq;

namespace QRBuild
{
    internal class BuildProcess
    {
        ///-----------------------------------------------------------------
        /// Public interface

        public BuildProcess(BuildGraph buildGraph, BuildOptions buildOptions, BuildResults buildResults)
        {
            m_buildGraph = buildGraph;
            m_buildOptions = buildOptions;
            m_buildResults = buildResults;
        }

        public bool Run(BuildAction action, HashSet<BuildNode> buildNodes, bool processDependencies)
        {
            bool initialized = InitializeBuildProcess(buildNodes, processDependencies);
            if (!initialized) {
                return false;
            }

            bool result = Run(action);
            m_buildResults.TranslationCount = m_requiredNodes.Count;
            return result;
        }


        ///-----------------------------------------------------------------
        /// Helpers

        /// Determines the initial runList.
        /// Also establishes consumer relationships for the specific nodes
        /// that are required for this build.
        private bool InitializeBuildProcess(IEnumerable<BuildNode> buildNodes, bool processDependencies)
        {
            m_requiredNodes.Clear();
            m_runSet.Clear();
            m_runList.Clear();
            
            //  If not processing dependencies, then simply add the requested
            //  buildNodes to the runList and return.
            //  Since no consumers will be written, no dependencies will be
            //  built or cleaned.
            if (!processDependencies) {
                m_requiredNodes.AddRange(buildNodes);
                m_runList.AddRange(buildNodes);
                return true;
            }

            var stack = new Stack<BuildNode>();
            stack.AddRange(buildNodes);

            HashSet<BuildNode> requiredNodes = new HashSet<BuildNode>();

            while (stack.Count > 0) {
                BuildNode currentNode = stack.Pop();
                requiredNodes.Add(currentNode);

                if (AllDependenciesExecuted(currentNode)) {
                    m_runSet.Add(currentNode);
                }

                //  Recurse through all dependent nodes.
                foreach (BuildNode dependency in currentNode.Dependencies) {
                    if (!requiredNodes.Contains(dependency)) {
                        stack.Push(dependency);
                    }
                }
            }

            //  Copy requiredNodes into m_requiredNodes.
            m_requiredNodes.AddRange(requiredNodes);
            //  Copy runSet into m_runList.
            foreach (var buildNode in m_runSet) {
                m_runList.Enqueue(buildNode);
            }

            TraceBuildNodes();

            return true;
        }

        private void TraceBuildNodes()
        {
            foreach (var buildNode in m_requiredNodes) {
                Trace.TraceInformation("BuildNode [{0}] : WaitCount = {1}", buildNode.Translation.DepsCacheFilePath, buildNode.Dependencies.Count);
                foreach (var dependency in buildNode.Dependencies) {
                    Trace.TraceInformation("    Dependency [{0}]", dependency.Translation.DepsCacheFilePath);
                }
                foreach (var consumer in buildNode.Consumers) {
                    Trace.TraceInformation("    Consumer   [{0}]", consumer.Translation.DepsCacheFilePath);
                }
            }
        }

        private bool Run(BuildAction action)
        {
            Queue<BuildNode> localRunList = new Queue<BuildNode>();

            m_issuedNodeCount = 0;
            m_requiredNodeCount = m_requiredNodes.Count;
            m_completedNodeCount = 0;
            m_pendingNodeCount = 0;

            while (m_issuedNodeCount < m_requiredNodeCount) {
                //  if things in runList, copy them to localRunList
                lock (m_runListMutex)
                {
                    //  If it's impossible to make progress, wait for more runList entries.
                    if (m_runList.Count == 0 && localRunList.Count == 0) {
                        Monitor.Wait(m_runListMutex);
                    }

                    //  Greedily transfer all elements to localRunList.
                    while (m_runList.Count > 0) {
                        BuildNode buildNode = m_runList.Dequeue();
                        m_runSet.Remove(buildNode);
                        localRunList.Enqueue(buildNode);
                    }
                }

                //  while (thingsToIssue  AND  allowedToLaunch)
                while ((localRunList.Count > 0) && (m_pendingNodeCount < m_buildOptions.MaxConcurrency)) {
                    //  launch one thing
                    BuildNode buildNode = localRunList.Dequeue();
                    m_issuedNodeCount += 1;
                    m_pendingNodeCount += 1;
                    ThreadPool.QueueUserWorkItem(unused => DoAction(action, buildNode));
                }

                bool completionSuccess = WaitForOneOrMorePendingNodes();
                if (!completionSuccess) {
                    return false;
                }
            }

            //  We have no more BuildNodes left to issue.
            //  Wait for remaining pending nodes to complete.
            while (m_pendingNodeCount > 0) {
                bool completionSuccess = WaitForOneOrMorePendingNodes();
                if (!completionSuccess) {
                    return false;
                }
            }

            return true;
        }

        //  Wait for at least one BuildNode to complete, update state, and return.
        private bool WaitForOneOrMorePendingNodes()
        {
            lock (m_executedListMutex)
            {
                //  Wait for m_completedList to gain one or more elements.
                if (m_executedList.Count == 0) {
                    Monitor.Wait(m_executedListMutex);
                }

                //  Quickly drain the executedList.
                //  All completed BuildNodes will be processed through here.
                while (m_executedList.Count > 0) {
                    BuildNode completedNode = m_executedList.Dequeue();
                    m_completedNodeCount += 1;
                    m_pendingNodeCount -= 1;

                    //  Update counters.
                    if (completedNode.Status == BuildStatus.ExecuteSucceeded ||
                        completedNode.Status == BuildStatus.ExecuteFailed) {
                        m_buildResults.ExecutedCount += 1;
                    }
                    else if (completedNode.Status == BuildStatus.TranslationUpToDate) {
                        m_buildResults.UpToDateCount += 1;
                    }
                    else if (completedNode.Status == BuildStatus.ImplicitIONotReady) {
                        m_buildResults.UpdateImplicitIOCount += 1;
                    }

                    if (completedNode.Status.Succeeded()) {
                        //  Trace creation of each output file.
                        foreach (var output in completedNode.Translation.ExplicitOutputs) {
                            Trace.TraceInformation("Generated Output {0}", output);
                        }
                    }
                    else if (completedNode.Status.Failed()) {
                        Trace.TraceError("Error while running Translation for {0}.", completedNode.Translation.BuildFileBaseName);
                        if (!m_buildOptions.ContinueOnError) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool AllDependenciesExecuted(BuildNode buildNode)
        {
            foreach (BuildNode dependency in buildNode.Dependencies) {
                if (!dependency.Status.Executed()) {
                    return false; 
                }
            }
            return true;
        }

        private void NotifyBuildNodeCompletion(BuildNode buildNode)
        {
            lock (m_buildNodesMutex)
            {
                if (buildNode.Status.Executed())
                {
                    foreach (var consumer in buildNode.Consumers) {
                        bool consumerReady = AllDependenciesExecuted(consumer);
                        if (consumerReady) {
                            lock (m_runListMutex) {
                                m_runSet.Add(consumer);
                                m_runList.Enqueue(consumer);
                                Monitor.Pulse(m_runListMutex);
                            }
                        }
                    }
                }

                lock (m_executedListMutex) {
                    m_executedList.Enqueue(buildNode);
                    Monitor.Pulse(m_executedListMutex);
                }
            }
        }

        private void DoAction(BuildAction action, BuildNode buildNode)
        {
            buildNode.Status = BuildStatus.InProgress;

            if (action == BuildAction.Build) {
                buildNode.Status = ExecuteOneBuildNode(buildNode);
            }
            else if (action == BuildAction.Clean) {
                foreach (var output in buildNode.Translation.ExplicitOutputs) {
                    File.Delete(output);
                }
                foreach (var output in buildNode.Translation.ImplicitOutputs) {
                    File.Delete(output);
                }
                HashSet<string> intermediateBuildFiles = buildNode.Translation.GetIntermediateBuildFiles();
                foreach (var output in intermediateBuildFiles) {
                    File.Delete(output);
                }
                File.Delete(buildNode.Translation.DepsCacheFilePath);
                buildNode.Status = BuildStatus.ExecuteSucceeded;
            }
            else {
                //  Unhandled action.
                buildNode.Status = BuildStatus.FailureUnknown;
            }

            NotifyBuildNodeCompletion(buildNode);
        }

        /// Executes the Translation associated with buildNode, if all dependencies
        /// are up-to-date.  This includes explicit and implicit IOs.
        /// This function does not access BuildNode.Dependencies or BuildNode.Consumers,
        /// so locking m_buildNodesMutex is not necessary here.
        private BuildStatus ExecuteOneBuildNode(BuildNode buildNode)
        {
            //  depsCache file opened for exclusive access here.  
            using (FileStream depsCacheFileStream = new FileStream(buildNode.Translation.DepsCacheFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                string prevDepsCacheFileContents = QRFileStream.ReadAllText(depsCacheFileStream);
                buildNode.Translation.ImplicitInputs.Clear();
                buildNode.Translation.ImplicitOutputs.Clear();
                if (buildNode.Translation.RequiresImplicitInputs ||
                    buildNode.Translation.GeneratesImplicitOutputs) {
                    //  Pretend that the implicit IOs from a previous build apply to the current
                    //  build state.  This allows comparing current and previous state.
                    DependencyCache.LoadImplicitIO(prevDepsCacheFileContents, buildNode.Translation.ImplicitInputs, buildNode.Translation.ImplicitOutputs);
                }

                if (!RequiresBuild(buildNode, prevDepsCacheFileContents)) {
                    //  success!  we can early-exit
                    return BuildStatus.TranslationUpToDate;
                }

                //  Ensure inputs exist prior to execution.
                //  This must be done here since Translation.Execute() is not
                //  expected to do the checks.
                if (!FilesExist(buildNode.Translation.ExplicitInputs)) {
                    //  failed.  some input does not exist
                    WriteDepsCacheFile(depsCacheFileStream, buildNode);
                    return BuildStatus.InputsDoNotExist;
                }

                //  Since all explicit inputs exist, we can update the implicit IOs.
                buildNode.Translation.UpdateImplicitIO();
                bool implicitDependenciesReady = PropagateDependenciesFromImplicitIO(buildNode);
                if (!implicitDependenciesReady) {
                    //  Clear the deps cache, so this node must re-execute when inputs are ready.
                    depsCacheFileStream.SetLength(0);
                    return BuildStatus.ImplicitIONotReady;
                }

                bool executeSucceeded = false;
                try
                {
                    executeSucceeded = buildNode.Translation.Execute();
                }
                catch (System.Exception)
                {
                	//  TODO: log the error
                }
                if (!executeSucceeded) {
                    //  Clear the deps cache, so this failed node must execute on the next build.
                    depsCacheFileStream.SetLength(0);
                    return BuildStatus.ExecuteFailed;
                }

                //  success.
                WriteDepsCacheFile(depsCacheFileStream, buildNode);
                return BuildStatus.ExecuteSucceeded;
            }
        }

        private void WriteDepsCacheFile(FileStream depsCacheFileStream, BuildNode buildNode)
        {
            string depsCacheString = DependencyCache.CreateDepsCacheString(
                buildNode.Translation,
                m_buildOptions.FileDecider);
            QRFileStream.WriteAllText(depsCacheFileStream, depsCacheString);
        }

        private bool PropagateDependenciesFromImplicitIO(BuildNode buildNode)
        {
            bool implicitDependenciesReady = true;
            lock (m_buildNodesMutex)
            {
                //  Create BuildFiles for ImplicitInputs and .
                foreach (string path in buildNode.Translation.ImplicitInputs) {
                    BuildFile buildFile = m_buildGraph.CreateOrGetBuildFile(path);
                    if (buildFile.BuildNode != null) {
                        buildNode.Dependencies.Add(buildFile.BuildNode);
                        buildFile.Consumers.Add(buildNode);
                        if (!buildFile.BuildNode.Status.Executed()) {
                            implicitDependenciesReady = false;
                        }
                    }
                }

                //  Set BuildFile.BuildNode for each implicit output.
                //  Find all immediate consumers of the ImplicitOutputs, and update the
                //  dependency relationship between buildNode and the immediate consumers.
                HashSet<BuildNode> immediateConsumers = new HashSet<BuildNode>();
                foreach (string path in buildNode.Translation.ImplicitOutputs) {
                    BuildFile buildFile = m_buildGraph.CreateOrGetBuildFile(path);
                    immediateConsumers.AddRange(buildFile.Consumers);
                    if (buildFile.BuildNode == null) {
                        buildFile.BuildNode = buildNode;
                    }
                    else {
                        Trace.TraceError("Target {0} claims to be generated by more than one Translation:\n\t{1}\n\t{2}\n",
                            buildFile.Path,
                            buildFile.BuildNode.Translation.BuildFileBaseName,
                            buildNode.Translation.BuildFileBaseName);
                    }
                }
                
                //  Add consumers to the build process. 
                AddAllConsumersToBuildProcess(immediateConsumers);
            }
            return implicitDependenciesReady;
        }

        private void AddAllConsumersToBuildProcess(IEnumerable<BuildNode> buildNodes)
        {
            var stack = new Stack<BuildNode>();
            stack.AddRange(buildNodes);

            HashSet<BuildNode> runSet = new HashSet<BuildNode>();
            HashSet<BuildNode> requiredNodes = new HashSet<BuildNode>();

            while (stack.Count > 0) {
                BuildNode currentNode = stack.Pop();

                requiredNodes.Add(currentNode);

                if (AllDependenciesExecuted(currentNode)) {
                    runSet.Add(currentNode);
                }

                //  Add all consumers.
                foreach (BuildNode buildNode in currentNode.Consumers) {
                    if (!requiredNodes.Contains(buildNode)) {
                        stack.Push(buildNode);
                    }
                }
            }

            //  Copy requiredNodes into m_requiredNodes.
            foreach (BuildNode requiredNode in requiredNodes) {
                if (!m_requiredNodes.Contains(requiredNode)) {
                    m_requiredNodes.Add(requiredNode);
                    m_requiredNodeCount += 1;
                }
            }
            
            //  Copy runSet into m_runSet/m_runList.
            foreach (var buildNode in runSet) {
                if (!m_runSet.Contains(buildNode)) {
                    m_runSet.Add(buildNode);
                    m_runList.Enqueue(buildNode);
                }
            }
        }

        private static bool FilesExist(IEnumerable<string> files)
        {
            foreach (string file in files) {
                if (!File.Exists(file)) {
                    Trace.TraceError("Input Target {0} does not exist.", file);
                    return false;
                }
            }
            return true;
        }

        private bool RequiresBuild(BuildNode buildNode, string previousBuildNodeState)
        {
            //  No need to check if the file exists, because it has been opened-or-created
            //  prior to this function call.

            //  Load a string representation of the previous state of files on disk.            
            //  If the deps cache does not exist, then return true to indicate we must build.
            if (String.IsNullOrEmpty(previousBuildNodeState)) {
                return true;
            }

            //  Create a string representation of the current state of files on disk.
            string currentBuildNodeState = DependencyCache.CreateDepsCacheString(
                buildNode.Translation,
                m_buildOptions.FileDecider);
            //  Compare previous and current state.
            bool filesChanged = (previousBuildNodeState != currentBuildNodeState);
            return filesChanged;
        }


        ///-----------------------------------------------------------------
        /// Private members

        /// This mutex serializes access to the following:
        /// - BuildNode.Dependencies and
        /// - BuildNode.Consumers, 
        /// - m_requiredNodes
        /// - m_buildGraph
        private readonly object m_buildNodesMutex = new object();
        /// m_requiredNodes contains all nodes that must be executed.
        private readonly HashSet<BuildNode> m_requiredNodes = new HashSet<BuildNode>();

        private volatile int m_requiredNodeCount = 0;
        private volatile int m_issuedNodeCount = 0;
        private volatile int m_completedNodeCount = 0;
        private volatile int m_pendingNodeCount = 0;

        //  The run list contains currently executing nodes.
        private readonly object m_runListMutex = new object();
        private readonly HashSet<BuildNode> m_runSet = new HashSet<BuildNode>();
        private readonly Queue<BuildNode> m_runList = new Queue<BuildNode>();

        //  The executed list contains nodes that have executed,
        //  but have not yet been added to m_completedNodes.
        private readonly object m_executedListMutex = new object();
        private readonly Queue<BuildNode> m_executedList = new Queue<BuildNode>();

        private readonly BuildGraph m_buildGraph;
        private readonly BuildOptions m_buildOptions;
        private readonly BuildResults m_buildResults;
        
    }
}
