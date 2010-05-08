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

        /// The BuildGraph to which this Translation belongs.
        public BuildGraph BuildGraph { get; private set; }

        /// The DepsCache file is used to store Translation parameters
        /// and inputs+outputs.  It is used to determine dirty status
        /// during a BuildProcess execution.
        public string DepsCacheFilePath 
        { 
            get
            {
                if (m_depsCachePath == null) {
                    return GetDefaultDepsCacheFilePath();
                }
                return m_depsCachePath;
            }
            set { m_depsCachePath = value; } 
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

        public HashSet<string> GetInputs()
        {
            HashSet<string> inputs = new HashSet<string>();
            ComputeInputs(inputs); // request inputs from derived class
            inputs.AddRange(ForcedInputs);
            return inputs;
        }
        public HashSet<string> GetOutputs()
        {
            HashSet<string> outputs = new HashSet<string>();
            ComputeOutputs(outputs); // request outputs from derived class
            outputs.AddRange(ForcedOutputs);
            return outputs;
        }


        //-- Public abstract interface
        
        /// Execute
        public abstract bool Execute();

        //  TODO: add events for Executing and Executed

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
            if (buildGraph == null) {
                throw new ArgumentNullException("buildGraph");
            }
            BuildNode = new BuildNode(this);
            BuildGraph = buildGraph;
            BuildGraph.Add(this);
        }

        /// Implementors should add inputs in this function.
        /// Parameter 'inputs' is guaranteed to be empty on entry.
        protected abstract void ComputeInputs(HashSet<string> inputs);

        /// Implementors should add outputs in this function.
        /// Parameter 'outputs' is guaranteed to be empty on entry.
        protected abstract void ComputeOutputs(HashSet<string> outputs);

        /// Implementor can optionally implement this to make things
        /// more convenient for users.
        /// A typical implementation will name the file after the
        /// "primary" output of the Translation.
        protected virtual string GetDefaultDepsCacheFilePath()
        {
            return null;
        }


        ///-----------------------------------------------------------------
        /// Members

        private string m_depsCachePath;
        private readonly HashSet<string> m_forcedInputs = new HashSet<string>();
        private readonly HashSet<string> m_forcedOutputs = new HashSet<string>();
    }
}
