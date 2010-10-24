using System;
using System.Collections.Generic;
using QRBuild.ProjectSystem.CommandLine;

namespace QRBuild.ProjectSystem
{
    public abstract class Project
    {
        /// AddToGraph is called by the ordinary build/clean/clobber commands.
        /// The
        public abstract void AddToGraph(ProjectManager pmgr, object variant);

        public abstract Target DefaultTarget
        {
            get;
        }

        public virtual IEnumerable<CLHandler> ExtraCommands()
        {
            return new List<CLHandler>();
        }
    }
}