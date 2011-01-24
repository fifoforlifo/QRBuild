using System;
using System.Collections.Generic;
using QRBuild.Linq;

namespace QRBuild
{
    public abstract class BuildTranslation
    {
        //-- Public concrete interface

        /// ModuleName can be used as a filter.
        public string ModuleName
        {
            get; set;
        }

        /// The BuildGraph to which this Translation belongs.
        public BuildGraph BuildGraph { get; private set; }

        /// The DepsCache file is used to store Translation parameters
        /// and inputs+outputs.  It is used to determine dirty status
        /// during a BuildProcess execution.
        public string DepsCacheFilePath
        { 
            get
            {
                if (m_depsCachePath == null) 
                {
                    m_depsCachePath = BuildFileBaseName + "__qr__.deps";
                }
                return m_depsCachePath;
            }
        }

        /// Additional user-defined inputs (file names).
        /// Used by BuildGraph to perform dependency analysis.
        public HashSet<string> ForcedInputs
        {
            get { return m_forcedInputs; }
        }
        /// Additional user-defined outputs (file names).
        /// Used by BuildGraph to perform dependency analysis.
        public HashSet<string> ForcedOutputs
        {
            get { return m_forcedOutputs; }
        }
        /// Inputs that are fully determined by BuildTranslation params.
        public HashSet<string> ExplicitInputs
        {
            get { return m_explicitInputs; }
        }
        /// Outputs that are fully determined by BuildTranslation params.
        public HashSet<string> ExplicitOutputs
        {
            get { return m_explicitOutputs; }
        }
        /// Inputs that are determined based on the file contents of ExplicitInputs.
        public HashSet<string> ImplicitInputs
        {
            get { return m_implicitInputs; }
        }

        /// Causes ExplicitInputs and ExplicitOutputs to be updated.
        public void UpdateExplicitIO()
        {
            ExplicitInputs.Clear();
            ExplicitOutputs.Clear();
            ComputeExplicitIO(ExplicitInputs, ExplicitOutputs);
            ExplicitInputs.AddRange(ForcedInputs);
            ExplicitOutputs.AddRange(ForcedOutputs);
        }
        /// Causes ImplicitInputs and ImplicitOutputs to be updated.
        public bool UpdateImplicitInputs()
        {
            ImplicitInputs.Clear();
            bool implicitInputsKnown = ComputeImplicitIO(ImplicitInputs);
            return implicitInputsKnown;
        }

        //-- Public abstract interface
        
        /// Execute causes outputs to be created.
        public abstract bool Execute();

        //  TODO: add events for Executing and Executed

        /// Returns a set of build files.  Primarily used for Clean.
        public abstract HashSet<string> GetIntermediateBuildFiles();

        /// Returns a base file name suitable for generating other build-related
        /// files, such as the DepsCache and any process launching scripts.
        /// Typical Translations should name this after 
        public abstract string BuildFileBaseName { get; }

        /// Get a string that contains all configuration parameters that
        /// control the translation.  These are stored in DepsCache files,
        /// and compared to determine whether Execution is required.
        /// The current and previous values are compared when computing
        /// dirty status during a BuildProcess execution.
        public abstract string GetCacheableTranslationParameters();

        /// Translation returns true if it's possible to have implicit inputs.
        public abstract bool RequiresImplicitInputs { get; }


        //-- Internal interface

        /// The BuildNode that corresponds to this Translation.
        internal BuildNode BuildNode;


        //-- Protected interface

        /// Constructor to be called by derived classes.
        protected BuildTranslation(BuildGraph buildGraph)
        {
            if (buildGraph == null) {
                throw new ArgumentNullException("buildGraph");
            }
            ModuleName = "";
            BuildNode = new BuildNode(this);
            BuildGraph = buildGraph;
            BuildGraph.Add(this);
        }

        /// Implementors compute explicit inputs and outputs.
        protected abstract void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs);

        /// Implementors compute implicit inputs and outputs.
        protected abstract bool ComputeImplicitIO(HashSet<string> inputs);


        ///-----------------------------------------------------------------
        /// Members

        private string m_depsCachePath;
        private readonly HashSet<string> m_forcedInputs = new HashSet<string>();
        private readonly HashSet<string> m_forcedOutputs = new HashSet<string>();
        private readonly HashSet<string> m_explicitInputs = new HashSet<string>();
        private readonly HashSet<string> m_explicitOutputs = new HashSet<string>();
        private readonly HashSet<string> m_implicitInputs = new HashSet<string>();
    }
}
