using System;
using System.Collections.Generic;
using QRBuild.ProjectSystem.CommandLine;

namespace QRBuild.ProjectSystem
{
    public abstract class Project
    {
        /// This is set by the ProjectManager when the Project is instantiated.
        public ProjectManager ProjectManager
        {
            get; internal set;
        }

        /// This is set by the ProjectManager when the Project is instantiated.
        public BuildVariant BuildVariant
        {
            get; internal set;
        }

        /// This is called by the ProjectManager when the Project is instantiated,
        /// after all public properties have been set.
        public virtual void Initialize()
        {
        }

        /// AddToGraph is called by the ordinary build/clean/clobber commands.
        /// The implementor should add Translations to ProjectManager.Graph as appropriate.
        public abstract void AddToGraph(BuildVariant variant);

        /// DefaultTarget represents the default set of target files that
        /// will be built if a user does not specify any targets explicitly.
        /// The DefaultTarget.Targets property may be initialized in the AddToGraph()
        /// call if desired; its values will not be used/reported prior to that time.
        public abstract Target DefaultTarget
        {
            get;
        }
    }
}