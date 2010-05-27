using System;
using System.Collections.Generic;

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
        public BuildStatus Status;
    }
}
