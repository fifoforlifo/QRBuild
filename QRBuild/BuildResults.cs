﻿using System;

namespace QRBuild
{
    public sealed class BuildResults
    {
        public BuildAction Action { get; internal set; }
        
        public bool Success { get; internal set; }

        /// Total number of translations involved in a Build execution.
        public int TranslationCount { get; internal set; }

        /// Number of Execute() calls that needed to be made.
        /// This indicates the number of "dirty" BuildNodes.
        public int ExecutedCount { get; internal set; }

        /// Number of Translations that were skipped specifically because
        /// all inputs and outputs were up-to-date.
        public int UpToDateCount { get; internal set; }

        /// Number of times a Translation's implicit inputs were updated.
        public int UpdateImplicitInputsCount { get; internal set; }

        /// BuildGraph.Execute() wallclock times.
        public DateTime ExecuteStartTime { get; internal set; }
        public DateTime ExecuteEndTime { get; internal set; }

        /// BuildGraph.ComputeDependencies() wallclock times.
        /// Currently this is called as part of every Execute() call, and could
        /// consume a non-trivial amount of time.
        public bool DependenciesValid { get; internal set; }
        public DateTime ComputeDependenciesStartTime { get; internal set; }
        public DateTime ComputeDependenciesEndTime { get; internal set; }
    }
}
