using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using QRBuild.Linq;

namespace QRBuild.Engine
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

        public bool Run(BuildAction action, IList<BuildNode> buildNodes, bool processDependencies)
        {
            bool initialized = InitializeBuildProcess(buildNodes, processDependencies);
            if (!initialized)
            {
                return false;
            }

            return Run(action);
        }


        ///-----------------------------------------------------------------
        /// Helpers

        /// Determines the initial runList.
        /// Also establishes consumer relationships for the specific nodes
        /// that are required for this build.
        private bool InitializeBuildProcess(IEnumerable<BuildNode> buildNodes, bool processDependencies)
        {
            //  If not processing dependencies, then simply add the requested
            //  buildNodes to the runList and return.
            //  Since no consumers will be written, no dependencies will be
            //  built or cleaned.
            if (!processDependencies) 
            {
                m_requiredNodes.AddRange(buildNodes);
                m_runList.AddRange(buildNodes);
                return true;
            }

            //  Processing dependencies requires recursing, so we use a stack.

            var stack = new Stack<BuildNode>();
            stack.AddRange(buildNodes);

            ICollection<BuildNode> runSet = new HashSet<BuildNode>();

            while (stack.Count > 0)
            {
                BuildNode currentNode = stack.Pop();

                if (m_requiredNodes.Contains(currentNode))
                {
                    //  More than one BuildNode depends on currentNode.
                    //  Since we already processed currentNode, continue.
                    continue;
                }
                m_requiredNodes.Add(currentNode);
                currentNode.InitializeWaitCount();

                //  Establish consumers.
                foreach (BuildNode dependency in currentNode.Dependencies)
                {
                    dependency.Consumers.Add(currentNode);
                }

                if (currentNode.Dependencies.Count == 0)
                {
                    runSet.Add(currentNode);
                }
                else
                {
                    stack.AddRange(currentNode.Dependencies);
                }
            }

            //  Copy runSet into runList.
            foreach (var buildNode in runSet)
            {
                m_runList.Enqueue(buildNode);
            }

            m_buildResults.TranslationCount = m_requiredNodes.Count;

            TraceBuildNodes();

            return true;
        }

        private void TraceBuildNodes()
        {
            foreach (var buildNode in m_requiredNodes) 
            {
                Trace.TraceInformation("BuildNode [{0}] : WaitCount = {1}", buildNode.Translation.DepsCacheFilePath, buildNode.WaitCount);
                foreach (var dependency in buildNode.Dependencies) 
                {
                    Trace.TraceInformation("    Dependency [{0}]", dependency.Translation.DepsCacheFilePath);
                }
                foreach (var consumer in buildNode.Consumers) 
                {
                    Trace.TraceInformation("    Consumer   [{0}]", consumer.Translation.DepsCacheFilePath);
                }
            }
        }

        private bool Run(BuildAction action)
        {
            Queue<BuildNode> localRunList = new Queue<BuildNode>();

            int issuedNodeCount = 0;
            int completedNodeCount = 0;
            int pendingNodeCount = 0;
            while (issuedNodeCount < m_requiredNodes.Count)
            {
                //  if things in runList, copy them to localRunList
                lock (m_runListMutex)
                {
                    //  If it's impossible to make progress, wait for more runList entries.
                    if (m_runList.Count == 0 && localRunList.Count == 0)
                    {
                        Monitor.Wait(m_runListMutex);
                    }

                    //  Greedily transfer all elements to localRunList.
                    while (m_runList.Count > 0)
                    {
                        BuildNode buildNode = m_runList.Dequeue();
                        localRunList.Enqueue(buildNode);
                    }
                }

                //  while (thingsToIssue  AND  allowedToLaunch)
                while ((localRunList.Count > 0) && (pendingNodeCount < m_buildOptions.MaxConcurrency))
                {
                    //  launch one thing
                    BuildNode buildNode = localRunList.Dequeue();
                    issuedNodeCount += 1;
                    pendingNodeCount += 1;
                    ThreadPool.QueueUserWorkItem(unused => DoAction(action, buildNode));
                }

                bool completionSuccess = WaitForOneOrMorePendingNodes(ref completedNodeCount, ref pendingNodeCount);
                if (!completionSuccess)
                {
                    return false;
                }
            }

            //  We have no more BuildNodes left to issue.
            //  Wait for remaining pending nodes to complete.
            while (pendingNodeCount > 0)
            {
                bool completionSuccess = WaitForOneOrMorePendingNodes(ref completedNodeCount, ref pendingNodeCount);
                if (!completionSuccess)
                {
                    return false;
                }
            }

            return true;
        }

        //  Wait for at least one BuildNode to complete, update state, and return.
        private bool WaitForOneOrMorePendingNodes(ref int completedNodeCount, ref int pendingNodeCount)
        {
            lock (m_completedListMutex)
            {
                //  Wait for m_completedList to gain one or more elements.
                if (m_completedList.Count == 0)
                {
                    Monitor.Wait(m_completedListMutex);
                }

                //  Quickly drain the completedList.
                //  All completed BuildNodes will be processed through here.
                while (m_completedList.Count > 0)
                {
                    BuildNode completedNode = m_completedList.Dequeue();
                    completedNodeCount += 1;
                    pendingNodeCount -= 1;

                    //  Update counters.
                    if (completedNode.BuildStatus == BuildStatus.ExecuteSucceeded ||
                        completedNode.BuildStatus == BuildStatus.ExecuteFailed)
                    {
                        m_buildResults.ExecutedCount += 1;
                    }
                    else if (completedNode.BuildStatus == BuildStatus.TranslationUpToDate)
                    {
                        m_buildResults.UpToDateCount += 1;
                    }

                    if (completedNode.BuildStatus.Succeeded())
                    {
                        //  Trace creation of each output file.
                        foreach (var output in completedNode.Translation.GetOutputs())
                        {
                            Trace.TraceInformation("Generated Output {0}", output);
                        }
                    }
                    else if (completedNode.BuildStatus.Failed())
                    {
                        Trace.TraceError("Error while running Translation {0}.", completedNode.Translation.Name);
                        if (!m_buildOptions.ContinueOnError)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void NotifyBuildNodeCompletion(BuildNode buildNode)
        {
            foreach (var consumer in buildNode.Consumers)
            {
                bool consumerReady = consumer.DecrementWaitCount();
                if (consumerReady)
                {
                    lock (m_runListMutex)
                    {
                        m_runList.Enqueue(consumer);
                        Monitor.Pulse(m_runListMutex);
                    }
                }
            }

            lock (m_completedListMutex)
            {
                m_completedList.Enqueue(buildNode);
                Monitor.Pulse(m_completedListMutex);
            }
        }

        private void DoAction(BuildAction action, BuildNode buildNode)
        {
            buildNode.BuildStatus = BuildStatus.InProgress;

            if (action == BuildAction.Build)
            {
                buildNode.BuildStatus = ExecuteOneBuildNode(buildNode);
            }
            else if (action == BuildAction.Clean)
            {
                var outputs = m_buildGraph.GetBuildFilesForPaths(buildNode.Translation.GetOutputs());
                foreach (var output in outputs)
                {
                    output.Clean();
                }
                File.Delete(buildNode.Translation.DepsCacheFilePath);
                buildNode.BuildStatus = BuildStatus.ExecuteSucceeded;
            }
            else
            {
                //  Unhandled action.
                buildNode.BuildStatus = BuildStatus.FailureUnknown;
            }

            NotifyBuildNodeCompletion(buildNode);
        }

        private BuildStatus ExecuteOneBuildNode(BuildNode buildNode)
        {
            var inputs = m_buildGraph.GetBuildFilesForPaths(buildNode.Translation.GetInputs());
            var outputs = m_buildGraph.GetBuildFilesForPaths(buildNode.Translation.GetOutputs());

            //  TODO: lock deps cache file here (exclusive open/create for read/write)

            if (!RequiresBuild(buildNode, inputs, outputs))
            {
                //  success!  we can early-exit
                return BuildStatus.TranslationUpToDate;
            }
            //  Ensure inputs exist prior to execution.
            //  This must be done here since Translation.Execute() is not
            //  expected to do the checks.
            if (!FilesExist(inputs, m_buildOptions.ContinueOnError))
            {
                //  failed.  some input does not exist
                return BuildStatus.InputsDoNotExist;
            }

            bool executeSucceeded = buildNode.Translation.Execute();
            if (!executeSucceeded)
            {
                //  failed.  Remove the deps cache file to implicitly dirty 
                //           this Translation for next time.
                File.Delete(buildNode.Translation.DepsCacheFilePath);
                return BuildStatus.ExecuteFailed;
            }

            //  success!  write out the new deps cache file and return
            string depsCacheString = DependencyCache.CreateDepsCacheString(
                buildNode.Translation,
                m_buildOptions.FileDecider,
                inputs,
                outputs);
            File.WriteAllText(buildNode.Translation.DepsCacheFilePath, depsCacheString, Encoding.UTF8);
            //  TODO: unlock deps cache file here

            return BuildStatus.ExecuteSucceeded;
        }

        private static bool FilesExist(IEnumerable<BuildFile> buildFiles, bool continueOnError)
        {
            foreach (var buildFile in buildFiles)
            {
                if (!buildFile.Exists())
                {
                    Trace.TraceError("Input Target {0} does not exist.", buildFile.Id);
                    if (!continueOnError)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool RequiresBuild(BuildNode buildNode, ICollection<BuildFile> inputs, ICollection<BuildFile> outputs)
        {
            if (!File.Exists(buildNode.Translation.DepsCacheFilePath))
            {
                return true;
            }

            //  Load a string representation of the previous state of files on disk.            
            //  If the deps cache does not exist, then return true to indicate we must build.
            string previousBuildNodeState = File.ReadAllText(buildNode.Translation.DepsCacheFilePath);
            if (previousBuildNodeState == null)
            {
                return true;
            }

            //  Create a string representation of the current state of files on disk.
            string currentBuildNodeState = DependencyCache.CreateDepsCacheString(
                buildNode.Translation,
                m_buildOptions.FileDecider,
                inputs,
                outputs);
            //  Compare previous and current state.
            bool filesChanged = (previousBuildNodeState != currentBuildNodeState);
            return filesChanged;
        }


        ///-----------------------------------------------------------------
        /// Private members

        private readonly ICollection<BuildNode> m_requiredNodes = new HashSet<BuildNode>();

        private readonly object m_runListMutex = new object();
        private readonly Queue<BuildNode> m_runList = new Queue<BuildNode>();

        private readonly object m_completedListMutex = new object();
        private readonly Queue<BuildNode> m_completedList = new Queue<BuildNode>();

        readonly BuildGraph m_buildGraph;
        readonly BuildOptions m_buildOptions;
        readonly BuildResults m_buildResults;
    }
}
