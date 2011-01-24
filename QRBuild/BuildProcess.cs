using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using QRBuild.IO;
using QRBuild.Linq;

namespace QRBuild
{
    internal class BuildWorkItem
    {
        public BuildWorkItem(BuildNode buildNode)
        {
            BuildNode = buildNode;
        }
        public readonly BuildNode BuildNode;
        public BuildStatus ReturnedStatus;
    }
    
    internal class BuildProcess
    {
        ///-----------------------------------------------------------------
        /// Public interface

        public BuildProcess(BuildGraph buildGraph, BuildAction buildAction, BuildOptions buildOptions, BuildResults buildResults)
        {
            m_buildGraph = buildGraph;
            m_buildAction = buildAction;
            m_buildOptions = buildOptions;
            m_buildResults = buildResults;
        }

        public bool Run(HashSet<BuildNode> buildNodes, bool processDependencies)
        {
            bool initialized = InitializeBuildProcess(buildNodes, processDependencies);
            if (!initialized) {
                return false;
            }

            bool result = Run();
            m_buildResults.TranslationCount = m_requiredNodes.Count;
            return result;
        }


        ///-----------------------------------------------------------------
        /// Helpers

        /// Determines the initial requiredNodes and runList.
        /// This is done by recursing through all dependencies of the input buildNodes to
        /// ensure that all required files are up to date.
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
                m_runSet.AddRange(buildNodes);
                return true;
            }

            var stack = new Stack<BuildNode>();
            stack.AddRange(buildNodes);

            HashSet<BuildNode> requiredNodes = new HashSet<BuildNode>();

            while (stack.Count > 0) {
                BuildNode currentNode = stack.Pop();
                requiredNodes.Add(currentNode);

                if (currentNode.Dependencies.Count == 0) {
                    m_runSet.Add(currentNode);
                }

                //  Recurse through all dependencies of currentNode.
                foreach (BuildNode dependency in currentNode.Dependencies) {
                    if (!requiredNodes.Contains(dependency)) {
                        stack.Push(dependency);
                    }
                }
            }

            //  Copy requiredNodes into m_requiredNodes.
            m_requiredNodes.AddRange(requiredNodes);
            //  Copy m_runSet into m_runList.
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

        private bool Run()
        {
            m_completedNodeCount = 0;
            m_pendingNodeCount = 0;

            bool completionSuccess = true;
            if (m_runList.Count == 0) {
                // nothing to build
                Trace.TraceInformation("nothing to build.");
                return true;
            }

            while (m_completedNodeCount < m_requiredNodes.Count) {
                //  while (thingsToIssue  AND  allowedToLaunch)
                while ((m_runList.Count > 0) && (m_pendingNodeCount < m_buildOptions.MaxConcurrency)) {
                    //  launch one thing
                    BuildNode buildNode = m_runList.Dequeue();
                    m_runSet.Remove(buildNode);

                    BuildWorkItem workItem = new BuildWorkItem(buildNode);
                    ThreadPool.QueueUserWorkItem(unused => DoAction(workItem));
                    m_pendingNodeCount += 1;
                }

                completionSuccess &= WaitForOneOrMorePendingNodes();
                if (!completionSuccess && !m_buildOptions.ContinueOnError) {
                    //  Break out of the dispatch loop.
                    break;
                }
            }

            //  We have no more BuildNodes left to issue.
            //  Wait for remaining pending nodes to complete.  We do this even on failure,
            //  so that we guarantee all work is drained prior to returning.
            while (m_pendingNodeCount > 0) {
                completionSuccess &= WaitForOneOrMorePendingNodes();
            }

            return completionSuccess;
        }

        //  Wait for at least one BuildNode to complete, update state, and return.
        private bool WaitForOneOrMorePendingNodes()
        {
            Queue<BuildWorkItem> localReturnList = new Queue<BuildWorkItem>();
            lock (m_returnListMutex) {
                //  Wait for m_returnList to gain one or more elements.
                if (m_returnList.Count == 0) {
                    Monitor.Wait(m_returnListMutex);
                }

                //  Quickly drain the returnList.
                //  All returned BuildNodes will be processed through here.
                while (m_returnList.Count > 0) {
                    BuildWorkItem workItem = m_returnList.Dequeue();
                    localReturnList.Enqueue(workItem);
                }
            }

            while (localReturnList.Count > 0) {
                BuildWorkItem workItem = localReturnList.Dequeue();
                workItem.BuildNode.Status = workItem.ReturnedStatus;
                m_pendingNodeCount -= 1;

                //  Update dependent nodes and performance counters.
                if (workItem.BuildNode.Status == BuildStatus.ExecuteSucceeded ||
                    workItem.BuildNode.Status == BuildStatus.ExecuteFailed) {
                    m_completedNodeCount += 1;
                    m_buildResults.ExecutedCount += 1;
                    Trace.TraceInformation("BuildNode Completed : {0}", workItem.BuildNode.Translation.DepsCacheFilePath);
                    NotifyBuildNodeCompletion(workItem.BuildNode);
                }
                else if (workItem.BuildNode.Status == BuildStatus.TranslationUpToDate) {
                    m_completedNodeCount += 1;
                    m_buildResults.UpToDateCount += 1;
                    Trace.TraceInformation("BuildNode UpToDate  : {0}", workItem.BuildNode.Translation.DepsCacheFilePath);
                    NotifyBuildNodeCompletion(workItem.BuildNode);
                }
                else if (workItem.BuildNode.Status == BuildStatus.ImplicitInputsComputed) {
                    bool implicitDependenciesReady = PropagateDependenciesFromImplicitInputs(workItem.BuildNode);
                    if (implicitDependenciesReady) {
                        //  Continue execution of this BuildNode immediately.
                        workItem.BuildNode.ImplicitInputsUpToDate = true;
                        ThreadPool.QueueUserWorkItem(unused => DoAction(workItem));
                        m_pendingNodeCount += 1;
                    }

                    m_buildResults.UpdateImplicitInputsCount += 1;
                }

                if (workItem.BuildNode.Status.Succeeded()) {
                    //  Trace creation of each output file.
                    foreach (var output in workItem.BuildNode.Translation.ExplicitOutputs) {
                        Trace.TraceInformation("Generated Output {0}", output);
                    }
                }
                else if (workItem.BuildNode.Status.Failed()) {
                    Trace.TraceError("Error while running Translation for {0}.", workItem.BuildNode.Translation.BuildFileBaseName);
                    if (!m_buildOptions.ContinueOnError) {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool AllDependenciesExecuted(BuildNode buildNode)
        {
            foreach (BuildNode dependency in buildNode.Dependencies) {
                if (!m_requiredNodes.Contains(dependency)) {
                    continue;
                }

                if (!dependency.Status.Executed()) {
                    return false; 
                }
            }
            return true;
        }

        private void NotifyBuildNodeCompletion(BuildNode buildNode)
        {
            foreach (BuildNode consumer in buildNode.Consumers) {
                //  Skip consumers that aren't part of this build.
                if (!m_requiredNodes.Contains(consumer)) {
                    continue;
                }

                bool consumerReady = AllDependenciesExecuted(consumer);
                if (consumerReady) {
                    m_runList.Enqueue(consumer);
                    m_runSet.Add(consumer);
                }
            }
        }

        private void DoAction(BuildWorkItem workItem)
        {
            BuildNode buildNode = workItem.BuildNode;            
            if (m_buildAction == BuildAction.Build) {
                workItem.ReturnedStatus = ExecuteOneBuildNode(buildNode);
            }
            else if (m_buildAction == BuildAction.Clean) {
                foreach (var output in buildNode.Translation.ExplicitOutputs) {
                    RemoveFile(output);
                }
                HashSet<string> intermediateBuildFiles = buildNode.Translation.GetIntermediateBuildFiles();
                foreach (var output in intermediateBuildFiles) {
                    RemoveFile(output);
                }
                RemoveFile(buildNode.Translation.DepsCacheFilePath);
                workItem.ReturnedStatus = BuildStatus.ExecuteSucceeded;
            }
            
            else {
                //  Unhandled action.
                workItem.ReturnedStatus = BuildStatus.FailureUnknown;
            }

            lock (m_returnListMutex) {
                m_returnList.Enqueue(workItem);
                Monitor.Pulse(m_returnListMutex);
            }
        }

        private void RemoveFile(string path)
        {
            QRFile.Delete(path);
            if (m_buildOptions.RemoveEmptyDirectories) {
                QRDirectory.RemoveEmptyDirectories(path);
            }
        }

        /// Executes the Translation associated with buildNode, if all dependencies
        /// are up-to-date.  This includes explicit and implicit IOs.
        private BuildStatus ExecuteOneBuildNode(BuildNode buildNode)
        {
            //  depsCache file opened for exclusive access here.  
            using (FileStream depsCacheFileStream = new FileStream(buildNode.Translation.DepsCacheFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
                if (!buildNode.Translation.RequiresImplicitInputs) {
                    string prevDepsCacheFileContents = QRFileStream.ReadAllText(depsCacheFileStream);
                    bool requiresBuild = RequiresBuild(buildNode, prevDepsCacheFileContents);
                    if (!requiresBuild) {
                        //  success!  we can early-exit
                        return BuildStatus.TranslationUpToDate;
                    }

                    //  Ensure explicit inputs exist prior to execution.
                    //  This must be done here since Translation.Execute() is not expected to do the checks.
                    bool explicitInputsExist = FilesExist(buildNode.Translation.ExplicitInputs);
                    if (!explicitInputsExist) {
                        //  failed.  some input does not exist
                        return BuildStatus.InputsDoNotExist;
                    }

                    //  fall through to Execute()
                }
                else { // buildNode.Translation.RequiresImplicitInputs
                    if (!buildNode.ImplicitInputsUpToDate) {
                        string prevDepsCacheFileContents = QRFileStream.ReadAllText(depsCacheFileStream);
                        bool requiresBuild = RequiresBuild(buildNode, prevDepsCacheFileContents);
                        if (!requiresBuild) {
                            //  success!  we can early-exit
                            return BuildStatus.TranslationUpToDate;
                        }

                        //  Ensure explicit inputs exist prior to execution.
                        //  This must be done here since Translation.Execute() is not expected to do the checks.
                        bool explicitInputsExist = FilesExist(buildNode.Translation.ExplicitInputs);
                        if (!explicitInputsExist) {
                            //  failed.  some input does not exist
                            return BuildStatus.InputsDoNotExist;
                        }

                        //  Since all explicit inputs exist, we can update the implicit IOs.
                        bool updateImplicitInputsSucceeded = buildNode.Translation.UpdateImplicitInputs();
                        if (!updateImplicitInputsSucceeded) {
                            return BuildStatus.ExecuteFailed;
                        }
                        return BuildStatus.ImplicitInputsComputed;
                    }
                    else { // buildNode.ImplicitInputsUpToDate
                        //  Ensure explicit inputs exist prior to execution.
                        //  This must be done here since Translation.Execute() is not expected to do the checks.
                        bool explicitInputsExist = FilesExist(buildNode.Translation.ExplicitInputs);
                        if (!explicitInputsExist) {
                            //  failed.  some input does not exist
                            return BuildStatus.InputsDoNotExist;
                        }
                        
                        //  Ensure inputs exist prior to execution.
                        //  This must be done here since Translation.Execute() is not expected to do the checks.
                        bool implicitInputsExist = FilesExist(buildNode.Translation.ImplicitInputs);
                        if (!implicitInputsExist) {
                            //  failed.  some input does not exist
                            return BuildStatus.InputsDoNotExist;
                        }

                        //  fall through to Execute()
                    }
                }

                bool executeSucceeded = false;
                try {
                    executeSucceeded = buildNode.Translation.Execute();
                }
                catch (Exception) {
                	//  TODO: log the error
                }
                if (!executeSucceeded) {
                    //  Clear the deps cache, so this failed node must execute on the next build.
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

        private bool PropagateDependenciesFromImplicitInputs(BuildNode buildNode)
        {
            bool implicitDependenciesReady = true;

            //  Create BuildFiles for ImplicitInputs and update Dependencies/Consumers.
            foreach (string path in buildNode.Translation.ImplicitInputs) {
                BuildFile buildFile = m_buildGraph.CreateOrGetBuildFile(path);
                if (buildFile.BuildNode != null) {
                    buildNode.Dependencies.Add(buildFile.BuildNode);
                    buildFile.BuildNode.Consumers.Add(buildNode);
                    buildFile.Consumers.Add(buildNode);
                    if (!buildFile.BuildNode.Status.Executed()) {
                        implicitDependenciesReady = false;
                    }
                    if (!m_requiredNodes.Contains(buildFile.BuildNode)) {
                        m_requiredNodes.Add(buildFile.BuildNode);
                        bool generatorReady = AllDependenciesExecuted(buildFile.BuildNode);
                        if (generatorReady) {
                            m_runList.Enqueue(buildFile.BuildNode);
                            m_runSet.Add(buildFile.BuildNode);
                        }
                    }
                }
            }

            return implicitDependenciesReady;
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

        /// m_requiredNodes contains all nodes that must be executed.
        private readonly HashSet<BuildNode> m_requiredNodes = new HashSet<BuildNode>();

        private int m_completedNodeCount;
        private int m_pendingNodeCount;

        //  The run list contains currently executing nodes.
        private readonly HashSet<BuildNode> m_runSet = new HashSet<BuildNode>();
        private readonly Queue<BuildNode> m_runList = new Queue<BuildNode>();

        //  The executed list contains nodes that have executed,
        //  but have not yet been added to m_completedNodes.
        private readonly object m_returnListMutex = new object();
        private readonly Queue<BuildWorkItem> m_returnList = new Queue<BuildWorkItem>();

        private readonly BuildGraph m_buildGraph;
        private readonly BuildAction m_buildAction;
        private readonly BuildOptions m_buildOptions;
        private readonly BuildResults m_buildResults;
    }
}
