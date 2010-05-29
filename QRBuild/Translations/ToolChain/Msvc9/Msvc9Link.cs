using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using QRBuild.IO;
using QRBuild.Linq;
using QRBuild.Translations;

namespace QRBuild.Translations.ToolChain.Msvc9
{
    public sealed class Msvc9Link : BuildTranslation
    {
        public Msvc9Link(BuildGraph buildGraph, Msvc9LinkerParams p)
            : base(buildGraph)
        {
            m_params = p.Canonicalize();
        }

        public Msvc9LinkerParams Params
        {
            get { return m_params; }
        }

        public override bool Execute()
        {
            string responseFile = m_params.ToArgumentString();
            string responseFilePath = GetResponseFilePath();
            File.WriteAllText(responseFilePath, responseFile);

            string logFilePath = GetBuildLogFilePath();

            string vcvarsBatchFilePath = Msvc9Utility.GetVcVarsBatchFilePath(m_params.ToolChain, m_params.VcBinDir);
            if (!File.Exists(vcvarsBatchFilePath)) {
                throw new InvalidOperationException(String.Format("vcvars batch file not found here : {0}", vcvarsBatchFilePath));
            }

            string batchFilePath = GetBatchFilePath();
            string batchFile = String.Format(@"
@echo off
REM If run with an argument, the batch file calls itself and redirects stdout,stderr to log file.
IF _%1 == _stdouterr (
    cmd /C  {0} > ""{1}""  2>&1
    EXIT /B %ERRORLEVEL%
)

@echo {2}
SETLOCAL

call ""{3}"" {4}

cd ""{5}""

link @""{6}""

EXIT /B %ERRORLEVEL%
",
                batchFilePath,
                logFilePath,
                "off" /* TODO: logging verbosity could control this */,
                vcvarsBatchFilePath,
                "> NUL" /* TODO: control whether vcvars messages are logged with a property */,
                m_params.CompileDir,
                responseFilePath);

            File.WriteAllText(batchFilePath, batchFile);

            using (QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir, false, "stdouterr")) {
                process.WaitHandle.WaitOne();

                string output = File.ReadAllText(logFilePath);
                Console.Write(output);

                bool success = process.GetExitCode() == 0;
                return success;
            }
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
        public override bool GeneratesImplicitOutputs
        {
            get { return false; }
        }

        protected override void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            inputs.AddRange(m_params.Inputs);
            inputs.AddRange(m_params.InputModules);
            // DefaultLib and NoDefaultLib are not tracked.

            // Add known toolchain binaries to the inputs.
            string vcvarsFilePath = Msvc9Utility.GetVcVarsBatchFilePath(m_params.ToolChain, m_params.VcBinDir);
            inputs.Add(vcvarsFilePath);
            if (m_params.ToolChain == Msvc9ToolChain.ToolsX86TargetX86) {
                string clPath = QRPath.GetCanonical(Path.Combine(m_params.VcBinDir, "link.exe"));
                inputs.Add(clPath);
            }
            else if (m_params.ToolChain == Msvc9ToolChain.ToolsX86TargetAmd64) {
                string clPath = QRPath.GetCanonical(
                    Path.Combine(Path.Combine(m_params.VcBinDir, "x86_amd64"), "link.exe"));
                inputs.Add(clPath);
            }
            else if (m_params.ToolChain == Msvc9ToolChain.ToolsAmd64TargetAmd64) {
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

        protected override bool ComputeImplicitIO(HashSet<string> inputs, HashSet<string> outputs)
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

        private readonly Msvc9LinkerParams m_params;
        private string m_buildFileBaseName;
    }
}
