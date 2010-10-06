using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using QRBuild.IO;
using QRBuild.Linq;
using QRBuild.Translations;

namespace QRBuild.Translations.ToolChain.MsCsc
{
    public sealed class CSharpCompile : BuildTranslation
    {
        public CSharpCompile(BuildGraph buildGraph, CSharpCompileParams p)
            : base(buildGraph)
        {
            m_params = p.Canonicalize();
        }

        public override bool Execute()
        {
            string responseFile = m_params.ToArgumentString();
            string responseFilePath = GetResponseFilePath();
            File.WriteAllText(responseFilePath, responseFile);

            string logFilePath = GetBuildLogFilePath();
            string cscPath = GetCscPath();

            string batchFilePath = GetBatchFilePath();
            string batchFile = String.Format(@"
@echo off
REM If run without an argument, the batch file calls itself and redirects stdout,stderr to log file.
IF _%1 == _ (
    cmd /C {0} doit > {1}  2>&1
    GOTO :END
)

@echo {2}
SETLOCAL

cd ""{3}""

""{4}"" {5} @""{6}""

:END
EXIT %ERRORLEVEL%
",
                batchFilePath,
                logFilePath,
                "off" /* TODO: logging verbosity could control this */,
                m_params.CompileDir,
                cscPath,
                m_params.NoConfig ? "/noconfig" : "", 
                responseFilePath);

            File.WriteAllText(batchFilePath, batchFile);

            using (QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir, false, ""))
            {
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
            return m_params.ToArgumentString();
        }

        public override bool RequiresImplicitInputs
        {
            get { return false; }
        }

        protected override void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            inputs.AddRange(m_params.Sources);
            inputs.AddRange(m_params.InputModules);
            inputs.AddRange(m_params.AssemblyReferences);

            outputs.Add(m_params.OutputFilePath);
            if (!String.IsNullOrEmpty(m_params.PdbFilePath)) {
                outputs.Add(m_params.PdbFilePath);
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
        private string GetCscPath()
        {
            string runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            string s1 = Path.Combine(runtimeDir, "..");
            string s2 = Path.Combine(s1, m_params.FrameworkVersion);
            string cscPath = Path.Combine(s2, "csc.exe");
            return cscPath;
        }

        private readonly CSharpCompileParams m_params;
        private string m_buildFileBaseName;
    }
}
