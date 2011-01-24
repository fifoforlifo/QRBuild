using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QRBuild.IO;
using QRBuild.Linq;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public sealed class MsvcLink : BuildTranslation
    {
        public MsvcLink(BuildGraph buildGraph, MsvcLinkerParams p)
            : base(buildGraph)
        {
            m_params = p.Canonicalize();
        }

        public MsvcLinkerParams Params
        {
            get { return m_params; }
        }

        public override bool Execute()
        {
            QRDirectory.EnsureDirectoryExists(m_params.BuildFileDir);

            string responseFile = m_params.ToArgumentString();
            string responseFilePath = GetResponseFilePath();
            File.WriteAllText(responseFilePath, responseFile);

            string logFilePath = GetBuildLogFilePath();

            string batchFilePath = GetBatchFilePath();
            string batchFile = String.Format(@"
@echo off
REM If run without an argument, the batch file calls itself and redirects stdout,stderr to log file.
IF _%1 == _ (
    cmd /C  {0} doit > ""{1}""  2>&1
    GOTO :END
)

@echo {2}
SETLOCAL

rem Adding quotes to the PATH variable causes DLL search to fail.
SET PATH={3};{3}\..\..\Common7\IDE;%PATH%
SET INCLUDE={3}\..\Include;%INCLUDE%
SET LIB={3}\..\Lib;%LIB%

cd ""{4}""

link @""{5}""

:END
EXIT %ERRORLEVEL%
",
                batchFilePath,
                logFilePath,
                "off" /* TODO: logging verbosity could control this */,
                m_params.VcBinDir,
                m_params.CompileDir,
                responseFilePath);

            File.WriteAllText(batchFilePath, batchFile);

            using (QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir, false, "")) {
                process.WaitHandle.WaitOne();

                string output = File.ReadAllText(logFilePath);
                Console.Write(output);

                bool success = process.GetExitCode() == 0;
                return success;
            }
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
                    string fileName = Path.GetFileName(m_params.OutputFilePath);
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
            b.Append(m_params.ToArgumentString());
            return b.ToString();
        }

        public override bool RequiresImplicitInputs
        {
            get { return true; }
        }

        protected override void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            inputs.AddRange(m_params.Inputs);
            inputs.AddRange(m_params.InputModules);
            // DefaultLib and NoDefaultLib are not tracked.

            // Add known toolchain binaries to the inputs.
            if (m_params.ToolChain == MsvcToolChain.ToolsX86TargetX86) {
                string clPath = QRPath.GetCanonical(Path.Combine(m_params.VcBinDir, "link.exe"));
                inputs.Add(clPath);
            }
            else if (m_params.ToolChain == MsvcToolChain.ToolsX86TargetAmd64) {
                string clPath = QRPath.GetCanonical(
                    Path.Combine(Path.Combine(m_params.VcBinDir, "x86_amd64"), "link.exe"));
                inputs.Add(clPath);
            }
            else if (m_params.ToolChain == MsvcToolChain.ToolsAmd64TargetAmd64) {
                string clPath = QRPath.GetCanonical(
                    Path.Combine(Path.Combine(m_params.VcBinDir, "amd64"), "link.exe"));
                inputs.Add(clPath);
            }

            // Outputs
            outputs.Add(m_params.OutputFilePath);
            if (!String.IsNullOrEmpty(m_params.PdbFilePath)) {
                outputs.Add(m_params.PdbFilePath);
            }
            if (!String.IsNullOrEmpty(m_params.ImpLibPath)) {
                outputs.Add(m_params.ImpLibPath);
            }
            if (!String.IsNullOrEmpty(m_params.MapFilePath)) {
                outputs.Add(m_params.MapFilePath);
            }
        }

        protected override bool ComputeImplicitIO(HashSet<string> inputs)
        {
            return true;
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

        private readonly MsvcLinkerParams m_params;
        private string m_buildFileBaseName;
    }
}
