using System;

namespace QRBuild.ProjectSystem
{
    public abstract class Project
    {
        /// This is set by the ProjectManager when the Project is instantiated.
        public ProjectManager ProjectManager
        {
            get; internal set;
        }

        public virtual string ModuleName
        {
            get { return String.Empty; }
        }

        /// This is set by the ProjectManager when the Project is instantiated.
        public BuildVariant Variant
        {
            get
            {
                return m_variant;
            }
            internal set
            {
                m_variant = value;
                m_fullVariantString = m_variant.ToVariantString();
            }
        }

        public string FullVariantString
        {
            get { return m_fullVariantString; }
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

        private BuildVariant m_variant;
        private string m_fullVariantString;
        private bool m_addedToGraph;
    }
}