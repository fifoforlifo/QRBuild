using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QRBuild.Linq;

namespace QRBuild.Engine
{
    public abstract class BuildTranslation
    {
        //-- Public concrete interface

        /// metadata
        public Dictionary<string, object> MetaData = new Dictionary<string, object>();

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
                    m_depsCachePath = PrimaryOutputFilePath + ".deps";
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
        /// Inputs that are determined based on the contents of ExplicitInputs.
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
        public bool UpdateImplicitIO()
        {
            ImplicitInputs.Clear();
            bool implicitInputsKnown = ComputeImplicitInputs(ImplicitInputs);
            return implicitInputsKnown;
        }

        //-- Public abstract interface
        
        /// Execute causes outputs to be created.
        public abstract bool Execute();

        //  TODO: add events for Executing and Executed

        /// Returns the name of the primary output of this translation.
        /// This file name is used as a base for generating other build-related
        /// files, such as the DepsCache and any process launching scripts.
        public abstract string PrimaryOutputFilePath { get; }

        /// Get a string that contains all configuration parameters that
        /// control the translation.  These are stored in DepsCache files,
        /// and compared to determine whether Execution is required.
        /// The current and previous values are compared when computing
        /// dirty status during a BuildProcess execution.
        public abstract string GetCacheableTranslationParameters();


        //-- Internal interface

        /// The BuildNode that corresponds to this Translation.
        internal BuildNode BuildNode;


        //-- Protected interface

        /// Constructor to be called by derived classes.
        protected BuildTranslation(BuildGraph buildGraph)
        {
            if (buildGraph == null) 
            {
                throw new ArgumentNullException("buildGraph");
            }
            BuildNode = new BuildNode(this);
            BuildGraph = buildGraph;
            BuildGraph.Add(this);
        }

        /// Implementors compute explicit inputs and outputs.
        protected abstract void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs);

        /// Implementors compute implicit inputs.
        protected abstract bool ComputeImplicitInputs(HashSet<string> inputs);


        ///-----------------------------------------------------------------
        /// Members

        private string m_depsCachePath;
        private readonly HashSet<string> m_forcedInputs = new HashSet<string>();
        private readonly HashSet<string> m_forcedOutputs = new HashSet<string>();
        private readonly HashSet<string> m_explicitInputs = new HashSet<string>();
        private readonly HashSet<string> m_explicitOutputs = new HashSet<string>();
        private readonly HashSet<string> m_implicitInputs = new HashSet<string>();
        private readonly HashSet<string> m_implicitOutputs = new HashSet<string>();
    }
}
