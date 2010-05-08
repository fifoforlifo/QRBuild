using System;
using System.Collections.Generic;
using System.Threading;

namespace QRBuild.Engine
{
    /// BuildNode is 1:1 with BuildTranslation instance.
    /// BuildNode tracks dynamic state during a BuildProcess execution.
    internal sealed class BuildNode
    {
        public BuildNode(BuildTranslation translation)
        {
            Translation = translation;
        }

        public readonly BuildTranslation Translation;

        /// TODO: add public field for log

        /// Immediate dependencies.
        public readonly HashSet<BuildNode> Dependencies = new HashSet<BuildNode>();
        /// Consumer BuildNodes require this node to complete before they can execute.
        public readonly HashSet<BuildNode> Consumers = new HashSet<BuildNode>();

        /// Status of the most recent execution of a BuildProcess.
        public BuildStatus BuildStatus;


        /// BuildProcess calls this on all non-leaf BuildNodes.
        internal void InitializeWaitCount()
        {
            m_waitCount = Dependencies.Count;
        }

        /// Each time a dependency completes, this is called.
        /// When the waitCount reaches zero, then this BuildNode can be
        /// added to the runList (since all its dependencies are satisfied).
        internal bool DecrementWaitCount()
        {
            int waitCount = Interlocked.Decrement(ref m_waitCount);
            return (waitCount == 0);
        }

        /// This property is available purely for diagnostics.  It's *not* used by core algorithms.
        internal int WaitCount
        {
            get { return m_waitCount; }
        }

        /// m_waitCount only needs to be a 32-bit int (not 64-bit long) for a few reasons:
        /// [1] ICollection.Count is an int, so we can't have more than 2B deps
        /// [2] It is in general not useful for a BuildGraph to have 2 billion 
        ///     Translations that all depend on a single BuildNode.
        /// [3] You'd need a 64-bit process to create 2 billion objects, and
        ///     even then it would not be fast.
        private int m_waitCount;
    }
}
