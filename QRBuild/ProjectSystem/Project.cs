using System;
using System.Collections.Generic;
using QRBuild.IO;
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
        public BuildVariant Variant
        {
            get; internal set;
        }

        public string ProjectDir
        {
            get
            {
                if (m_projectDir == null) {
                    m_projectDir = QRPath.GetAssemblyDirectory(GetType());
                }
                return m_projectDir;
            }
        }

        internal void AddToGraphOnce()
        {
            if (m_addedToGraph) {
                throw new InvalidOperationException("Programmer error.");
            }
            m_addedToGraph = true;

            AddToGraph();
        }

        /// AddToGraph is called by the ordinary build/clean/clobber commands.
        /// The implementor should add Translations to ProjectManager.Graph as appropriate.
        protected abstract void AddToGraph();

        /// DefaultTarget represents the default set of target files that
        /// will be built if a user does not specify any targets explicitly.
        /// The DefaultTarget.Targets property may be initialized in the AddToGraph()
        /// call if desired; its values will not be used/reported prior to that time.
        public abstract Target DefaultTarget
        {
            get;
        }

        private string m_projectDir;
        private bool m_addedToGraph;
    }
}