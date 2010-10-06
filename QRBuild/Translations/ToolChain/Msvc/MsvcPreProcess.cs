using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QRBuild.IO;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public sealed class MsvcPreProcess : BuildTranslation
    {
        public MsvcPreProcess(BuildGraph buildGraph, MsvcPreProcessParams p)
            : base(buildGraph)
        {
            m_params = p.Canonicalize();
        }

        public MsvcPreProcessParams Params
        {
            get { return m_params; }
        }

        public override bool Execute()
        {
            // This is a no-op.  The real work happens in ComputeImplicitIO().
            return m_preProcessSucceeded;
        }


        public override HashSet<string> GetIntermediateBuildFiles()
        {
            HashSet<string> result = new HashSet<string>();
            result.Add(GetBatchFilePath());
            result.Add(GetResponseFilePath());
            result.Add(GetBuildLogFilePath());
            return result;
        }

        public override string BuildFileBaseName
        {
            get
            {
                if (String.IsNullOrEmpty(m_buildFileBaseName)) {
                    string fileName = Path.GetFileName(m_params.OutputPath);
                    m_buildFileBaseName = Path.Combine(m_params.BuildFileDir, fileName);
                }
                return m_buildFileBaseName;
            }
        }

        public override string GetCacheableTranslationParameters()
        {
            StringBuilder b = new StringBuilder();
            b.AppendFormat("VcBinDir={0}\n", m_params.VcBinDir);
            b.AppendFormat("ToolChain={0}\n", m_params.ToolChain);
            b.Append(m_params.ToArgumentString(/* showIncludes */ true));
            return b.ToString();
        }

        public override bool RequiresImplicitInputs
        {
            get { return true; }
        }

        protected override void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            inputs.Add(m_params.SourceFile);
            // Add known toolchain binaries to the inputs.
            if (m_params.ToolChain == MsvcToolChain.ToolsX86TargetX86) {
                string clPath = QRPath.GetCanonical(Path.Combine(m_params.VcBinDir, "cl.exe"));
                inputs.Add(clPath);
            }
            else if (m_params.ToolChain == MsvcToolChain.ToolsX86TargetAmd64) {
                string clPath = QRPath.GetCanonical(
                    Path.Combine(Path.Combine(m_params.VcBinDir, "x86_amd64"), "cl.exe"));
                inputs.Add(clPath);
            }
            else if (m_params.ToolChain == MsvcToolChain.ToolsAmd64TargetAmd64) {
                string clPath = QRPath.GetCanonical(
                    Path.Combine(Path.Combine(m_params.VcBinDir, "amd64"), "cl.exe"));
                inputs.Add(clPath);
            }

            // Outputs
            string outputFilePath = QRPath.ComputeDefaultFilePath(m_params.OutputPath, m_params.SourceFile, ".obj", m_params.CompileDir);
            outputs.Add(outputFilePath);
        }


        protected override bool ComputeImplicitIO(HashSet<string> inputs)
        {
            string responseFile = m_params.ToArgumentString(true);
            string responseFilePath = GetResponseFilePath();
            File.WriteAllText(responseFilePath, responseFile);

            string logFilePath = GetBuildLogFilePath();
            string preProcessedFilePath = m_params.OutputPath;

            string batchFilePath = GetBatchFilePath();
            string batchFile = String.Format(@"
@echo off
SETLOCAL

rem Adding quotes to the PATH variable causes DLL search to fail.
SET PATH={0};{0}\..\..\Common7\IDE;%PATH%
SET INCLUDE={0}\..\Include;%INCLUDE%
SET LIB={0}\..\Lib;%LIB%

cd ""{1}""

cl @""{2}"" 1> ""{3}"" 2> ""{4}""

EXIT /B %ERRORLEVEL%
",
                m_params.VcBinDir,
                m_params.CompileDir,
                responseFilePath,
                preProcessedFilePath,
                logFilePath);

            File.WriteAllText(batchFilePath, batchFile);

            using (QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir, false, "stdout")) {
                process.WaitHandle.WaitOne();
                // The process' exit code is ignored, because "missing header" errors
                // are not real errors if it's possible for the file to be generated.
            }

            string showIncludesFileContents = File.ReadAllText(logFilePath);
            m_preProcessSucceeded = MsvcUtility.ParseShowIncludesText(
                this.BuildGraph,
                showIncludesFileContents,
                m_params.CompileDir,
                m_params.IncludeDirs,
                inputs);
            return m_preProcessSucceeded;
        }

        private string GetBatchFilePath()
        {
            return BuildFileBaseName + "__qr__.bat";
        }
        private string GetResponseFilePath()
        {
            return BuildFileBaseName + "__qr__.rsp";
        }
        private string GetBuildLogFilePath()
        {
            return BuildFileBaseName + "__qr__.log";
        }

        private readonly MsvcPreProcessParams m_params;
        private string m_buildFileBaseName;
        private bool m_preProcessSucceeded;
    }
}
