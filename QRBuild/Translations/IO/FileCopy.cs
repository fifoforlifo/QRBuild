using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QRBuild.IO;

namespace QRBuild.Translations.IO
{
    public sealed class FileCopy : BuildTranslation
    {
        public FileCopy(
            BuildGraph buildGraph, 
            string source, 
            string destination,
            string buildFileDir)
            : base(buildGraph)
        {
            if (String.IsNullOrEmpty(source)) {
                throw new InvalidOperationException("Invalid source argument.");
            }
            if (String.IsNullOrEmpty(destination)) {
                throw new InvalidOperationException("Invalid destination argument.");
            }
            if (String.IsNullOrEmpty(destination)) {
                throw new InvalidOperationException("Invalid destination argument.");
            }

            m_source = QRPath.GetCanonical(source);
            m_destination = QRPath.GetCanonical(destination); ;
            m_buildFileDir = QRPath.GetCanonical(buildFileDir);
        }

        public override bool Execute()
        {
            File.Copy(m_source, m_destination, /* overwrite */ true);
            //  TODO: Is it desirable to update the modified time, or is it a
            //  pointless perf hit?
            FileInfo destFileInfo = new FileInfo(m_destination);
            if (destFileInfo.Exists) {
                destFileInfo.LastWriteTimeUtc = DateTime.UtcNow;
            }
            return true;
        }

        public override HashSet<string> GetIntermediateBuildFiles()
        {
            HashSet<string> result = new HashSet<string>();
            return result;
        }

        public override string BuildFileBaseName
        {
            get 
            {
                string destFileName = Path.GetFileName(m_destination);
                string result = Path.Combine(m_buildFileDir, destFileName);
                return result; 
            }
        }

        public override string GetCacheableTranslationParameters()
        {
            StringBuilder b = new StringBuilder();
            b.Append(m_source);
            b.Append(" ");
            b.Append(m_destination);
            return b.ToString();
        }

        public override bool RequiresImplicitInputs
        {
            get { return false; }
        }
        public override bool GeneratesImplicitOutputs
        {
            get { return false; }
        }

        protected override void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            inputs.Add(m_source);
            outputs.Add(m_destination);
        }

        protected override bool ComputeImplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            return true;
        }

        private readonly string m_source;
        private readonly string m_destination;
        private readonly string m_buildFileDir;
    }
}
