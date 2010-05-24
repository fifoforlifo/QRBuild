﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QRBuild.Engine
{
    /// 
    public sealed class BuildGraph
    {
        //-- Public interface

        /// Execute the specified action on targets.
        /// This is how you kick off a Build.
        public BuildResults Execute(
            BuildAction action,
            BuildOptions buildOptions,
            IEnumerable<string> targets,
            bool processDependencies)
        {
            Trace.TraceInformation("Execute {0}", action);
            Trace.TraceInformation("-----------");
            // TODO: trace targets

            BuildResults buildResults = new BuildResults();
            buildResults.ExecuteStartTime = DateTime.Now;

            List<BuildFile> targetBuildFiles = new List<BuildFile>();
            foreach (string target in targets) 
            {
                BuildFile buildFile;
                if (m_buildFiles.TryGetValue(target, out buildFile)) {
                    targetBuildFiles.Add(buildFile);
                }
                else {
                    // TODO: trace warning 
                }
            }

            buildResults.Success = UntimedExecute(
                action,
                buildOptions,
                targetBuildFiles,
                processDependencies,
                buildResults);

            buildResults.ExecuteEndTime = DateTime.Now;
            return buildResults;
        }

        /// Force dependencies to be recomputed on the next call that
        /// requires it.  This is really only useful if you want to get
        /// timings for ComputeDependencies.
        /// Typical usage: do not call this!
        public void ResetDependencyGraph()
        {
            m_dependenciesComputed = false;
        }

        /// Returns all files that are not generated by this BuildGraph.
        public ICollection<string> GetNonGeneratedFilePaths()
        {
            ComputeDependencies();
            if (!m_dependenciesValid) {
                return null;
            }

            var inputs = new HashSet<string>();
            foreach (var kvp in m_buildFiles) {
                BuildFile buildFile = kvp.Value;
                if (!IsTargetGenerated(buildFile)) {
                    inputs.Add(buildFile.Path);
                }
            }
            return inputs;
        }

        /// Returns all files that are generated by this BuildGraph.
        /// This may be a very large collection.
        public ICollection<string> GetGeneratedFilePaths()
        {
            ComputeDependencies();
            if (!m_dependenciesValid) {
                return null;
            }

            var inputs = new HashSet<string>();
            foreach (var kvp in m_buildFiles) {
                BuildFile buildFile = kvp.Value;
                if (IsTargetGenerated(buildFile)) {
                    inputs.Add(buildFile.Path);
                }
            }
            return inputs;
        }


        //-- Internal interface

        /// Add a translation to the BuildGraph.
        /// BuildTranslation's constructor calls this, so that users don't have to.
        internal void Add(BuildTranslation translation)
        {
            m_translations.Add(translation);
            m_dependenciesComputed = false;
        }

        internal HashSet<BuildFile> GetBuildFilesForPaths(ICollection<string> filePaths)
        {
            HashSet<BuildFile> buildFiles = new HashSet<BuildFile>();
            foreach (string filePath in filePaths)
            {
                BuildFile buildFile;
                if (m_buildFiles.TryGetValue(filePath, out buildFile)) {
                    buildFiles.Add(buildFile);
                }
                else {
                    // TODO: trace warning 
                }
            }
            return buildFiles;
        }


        //-- Helpers

        private static IList<BuildNode> GetBuildNodesForFiles(IEnumerable<BuildFile> files)
        {
            var result = files.Select(buildFile => buildFile.BuildNode).ToList();
            return result;
        }

        private static bool IsTargetGenerated(BuildFile target)
        {
            bool result = (target.BuildNode != null);
            return result;
        }

        private bool AddInputDependencies(BuildTranslation translation)
        {
            foreach (string input in translation.ExplicitInputs) {
                BuildFile buildFile = CreateOrGetBuildFile(input);
                if (buildFile.BuildNode != null) {
                    //  Add the BuildNode that generates input to the current node's dependency list.
                    translation.BuildNode.Dependencies.Add(buildFile.BuildNode);
                }
            }
            return true;
        }

        private bool AddOutputDependencies(BuildTranslation translation)
        {
            bool result = true;
            foreach (string output in translation.ExplicitOutputs) 
            {
                BuildFile buildFile = CreateOrGetBuildFile(output);
                if (IsTargetGenerated(buildFile)) {
                    Trace.TraceError("Target {0} claims to be generated by more than one Translation:\n    {1}\n    {2}\n",
                        buildFile.Path,
                        buildFile.BuildNode.Translation.PrimaryOutputFilePath,
                        translation.PrimaryOutputFilePath);
                    result = false;
                }
                else {
                    buildFile.BuildNode = translation.BuildNode;
                }
            }
            return result;
        }

        private BuildFile CreateOrGetBuildFile(string filePath)
        {
            BuildFile buildFile;
            if (m_buildFiles.TryGetValue(filePath, out buildFile)) {
                return buildFile;
            }
            buildFile = new BuildFile(filePath);
            return buildFile;
        }

        private bool TimedComputeDependencies(BuildResults buildResults)
        {
            buildResults.ComputeDependenciesStartTime = DateTime.Now;
            buildResults.DependenciesComputed = !m_dependenciesComputed;
            ComputeDependencies();
            m_dependenciesComputed = true;
            buildResults.ComputeDependenciesEndTime = DateTime.Now;
            return m_dependenciesValid;
        }

        /// This function determines all BuildNodes' dependencies.
        /// Consumers are cleared here, to be decided later by the
        /// specific targets required of a BuildProcess.
        /// Call this after all Translations have been added.
        private void ComputeDependencies()
        {
            if (m_dependenciesComputed) {
                return;
            }

            m_dependenciesValid = false;
            bool result = true;

            //  Clear m_buildFiles, so that we only use the most recent
            //  inputs and outputs from each BuildTranslation.
            m_buildFiles.Clear();

            //  Clear stale dependency information, and set outputs' BuildNode references.
            foreach (BuildTranslation translation in m_translations) {
                translation.BuildNode.Dependencies.Clear();
                translation.BuildNode.Consumers.Clear();
                translation.BuildNode.BuildStatus = BuildStatus.NotStarted;

                if (translation.DepsCacheFilePath == null) {
                    Trace.TraceError("Translation {0} has null DepsCachePath.", translation.PrimaryOutputFilePath);
                    return;
                }

                result &= AddOutputDependencies(translation);
            }

            //  Establish dependencies based on currentNode's inputs.
            foreach (BuildTranslation translation in m_translations) {
                result &= AddInputDependencies(translation);
            }

            m_dependenciesValid = result;
            return;
        }

        /// Returns true if a cycle exists, false otherwise.
        private bool CycleExists()
        {
            //  TODO: implement
            return false;
        }

        private bool UntimedExecute(
            BuildAction action,
            BuildOptions buildOptions,
            IEnumerable<BuildFile> targets,
            bool processDependencies,
            BuildResults buildResults)
        {
            bool computeDependenciesSuccess = TimedComputeDependencies(buildResults);
            if (!computeDependenciesSuccess) {
                return false;
            }

            IList<BuildNode> buildNodes = GetBuildNodesForFiles(targets);
            if (buildNodes == null) {
                return false;
            }

            BuildProcess buildProcess = new BuildProcess(this, buildOptions, buildResults);
            bool result = buildProcess.Run(
                action,
                buildNodes,
                processDependencies);
            return result;
        }


        //-- Members

        private bool m_dependenciesComputed;
        private bool m_dependenciesValid;

        private readonly HashSet<BuildTranslation> m_translations = new HashSet<BuildTranslation>();
        private readonly Dictionary<string, BuildFile> m_buildFiles = new Dictionary<string, BuildFile>();
    }
}
