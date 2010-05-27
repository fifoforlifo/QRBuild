using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using QRBuild.IO;
using QRBuild.Linq;
using QRBuild.Translations;

namespace QRBuild.CSharp
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
REM If run with an argument, the batch file calls itself and redirects stdout,stderr to log file.
IF _%1 NEQ _ (
    cmd /C  {0} > {1}  2>&1
    EXIT /B %ERRORLEVEL%
)

@echo {2}
SETLOCAL

cd ""{3}""

""{4}"" {5} @""{6}""

EXIT /B %ERRORLEVEL%
",
                batchFilePath,
                logFilePath,
                "on" /* TODO: logging verbosity could control this */,
                m_params.CompileDir,
                cscPath,
                m_params.NoConfig ? "/noconfig" : "", 
                responseFilePath);

            File.WriteAllText(batchFilePath, batchFile);

            using (QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir, false, " redirect_output"))
            {
                process.WaitHandle.WaitOne();

                string output = File.ReadAllText(logFilePath);
                Console.Write(output);

                bool success = process.GetExitCode() == 0;
                return success;
            }
        }

        public override string PrimaryOutputFilePath
        {
            get { return m_params.OutputFilePath; }
        }

        public override string GetCacheableTranslationParameters()
        {
            return m_params.ToArgumentString();
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
#if false
            // TODO: need a way to make this info available for Clean without polluting Build
            outputs.Add(GetBatchFilePath());
            outputs.Add(GetResponseFilePath());
            outputs.Add(GetBuildLogFilePath());
#endif
        }

        protected override bool ComputeImplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            //  C# does not have implicit dependencies.
            return true;
        }

        private string GetBatchFilePath()
        {
            return PrimaryOutputFilePath + "__qr__.bat";
        }
        private string GetResponseFilePath()
        {
            return PrimaryOutputFilePath + "__qr__.rsp";
        }
        private string GetBuildLogFilePath()
        {
            return PrimaryOutputFilePath + "__qr__.log";
        }
        private string GetCscPath()
        {
            string runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            string s1 = Path.Combine(runtimeDir, "..");
            string s2 = Path.Combine(s1, m_params.FrameworkVersion);
            string cscPath = Path.Combine(s2, "csc.exe");
            return cscPath;
        }

        readonly CSharpCompileParams m_params;
    }
}
