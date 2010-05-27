using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using QRBuild.IO;
using QRBuild.Linq;
using QRBuild.Translations;

namespace QRBuild.Translations.Cpp.Msvc9
{
    public sealed class Msvc9Compile : BuildTranslation
    {
        public Msvc9Compile(BuildGraph buildGraph, Msvc9CompileParams p)
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

            string vcvarsBatchFilePath = GetVcVarsBatchFilePath();
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

cl @""{6}""

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

            using (QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir, false, "stdouterr"))
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
            get 
            {
                if (m_params.Compile) {
                    return m_params.ObjectPath; 
                }
                else if (m_params.CreatePch) {
                    return m_params.CreatePchFilePath;
                }
                throw new InvalidOperationException("No valid output file specified for this Translation");
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

        protected override void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            inputs.Add(m_params.SourceFile);
            // Add known toolchain binaries to the inputs.
            string vcvarsFilePath = GetVcVarsBatchFilePath();
            inputs.Add(vcvarsFilePath);
            if (m_params.ToolChain == Msvc9ToolChain.ToolsX86TargetX86) {
                string clPath = QRPath.GetCanonical(Path.Combine(m_params.VcBinDir, "cl.exe"));
                inputs.Add(clPath);
            }
            else if (m_params.ToolChain == Msvc9ToolChain.ToolsX86TargetAmd64) {
                string clPath = QRPath.GetCanonical(
                    Path.Combine(Path.Combine(m_params.VcBinDir, "x86_amd64"), "cl.exe"));
                inputs.Add(clPath);
            }
            else if (m_params.ToolChain == Msvc9ToolChain.ToolsAmd64TargetAmd64) {
                string clPath = QRPath.GetCanonical(
                    Path.Combine(Path.Combine(m_params.VcBinDir, "amd64"), "cl.exe"));
                inputs.Add(clPath);
            }

            string objFilePath = QRPath.ComputeDefaultFilePath(m_params.ObjectPath, m_params.SourceFile, ".obj", m_params.CompileDir);
            outputs.Add(objFilePath);
            if (m_params.DebugInfoFormat != Msvc9DebugInfoFormat.None) {
                string pdbFilePath = QRPath.ComputeDefaultFilePath(m_params.PdbPath, m_params.SourceFile, ".pdb", m_params.CompileDir);
                outputs.Add(pdbFilePath);
            }
            if (m_params.AsmOutputFormat != Msvc9AsmOutputFormat.None) {
                string asmFilePath = QRPath.ComputeDefaultFilePath(m_params.PdbPath, m_params.SourceFile, ".asm", m_params.CompileDir);
                outputs.Add(asmFilePath);
            }
            if (!String.IsNullOrEmpty(m_params.CreatePchFilePath)) {
                string pchFilePath = QRPath.ComputeDefaultFilePath(m_params.CreatePchFilePath, m_params.SourceFile, ".pch", m_params.CompileDir);
                outputs.Add(pchFilePath);
            }
        }

        protected override bool ComputeImplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            string responseFile = m_params.ToPreProcessorArgumentString(true);
            string responseFilePath = GetPpResponseFilePath();
            File.WriteAllText(responseFilePath, responseFile);

            string showIncludesFilePath = GetShowIncludesFilePath();

            string vcvarsBatchFilePath = GetVcVarsBatchFilePath();
            if (!File.Exists(vcvarsBatchFilePath)) {
                throw new InvalidOperationException(String.Format("vcvars batch file not found here : {0}", vcvarsBatchFilePath));
            }

            string preProcessedFilePath = GetPreProcessedFilePath();

            string batchFilePath = GetPpBatchFilePath();
            string batchFile = String.Format(@"
@echo off
SETLOCAL

call ""{0}"" {1}

cd ""{2}""

cl @""{3}"" 1> ""{4}"" 2> ""{5}""

EXIT /B %ERRORLEVEL%
",
                vcvarsBatchFilePath,
                "> NUL" /* TODO: control whether vcvars messages are logged with a property */,
                m_params.CompileDir,
                responseFilePath,
                preProcessedFilePath,
                showIncludesFilePath);

            File.WriteAllText(batchFilePath, batchFile);

            using (QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir, false, "stdout")) {
                process.WaitHandle.WaitOne();
                // The process' exit code is irrelevant, because the preprocessing may have
                // failed for any number of reasons unrelated to missing include file.
            }

            // Parse the output for includes and error messages.
            bool success = true;
            string showIncludesFileContents = File.ReadAllText(showIncludesFilePath);
            using (StringReader sr = new StringReader(showIncludesFileContents)) {
                string prefix = "Note: including file:";
                while (true) {
                    string line = sr.ReadLine();
                    if (line == null) {
                        break;
                    }
                    
                    if (line.StartsWith(prefix)) {
                        int pathStart;
                        for (pathStart = prefix.Length; pathStart < line.Length; pathStart++) {
                            if (line[pathStart] != ' ') {
                                break;
                            }
                        }
                        string path = line.Substring(pathStart);
                        string absPath = QRPath.GetCanonical(path);
                        inputs.Add(absPath);
                    }
                    else if (line.Contains("fatal error C1083")) {
                        // C1083 is the "missing include file" error
                        success = false;
                    }
                }
            }

            return success;
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
        private string GetPpBatchFilePath()
        {
            return PrimaryOutputFilePath + "__qr__._pp.bat";
        }
        private string GetPpResponseFilePath()
        {
            return PrimaryOutputFilePath + "__qr__._pp.rsp";
        }
        private string GetPreProcessedFilePath()
        {
            return PrimaryOutputFilePath + "__qr__._pp.i";
        }
        private string GetShowIncludesFilePath()
        {
            return PrimaryOutputFilePath + "__qr__._pp.d";
        }
        private string GetVcVarsBatchFilePath()
        {
            if (m_params.ToolChain == Msvc9ToolChain.ToolsX86TargetX86) {
                string batchFilePath = Path.Combine(m_params.VcBinDir, "vcvars32.bat");
                return batchFilePath;
            }
            else if (m_params.ToolChain == Msvc9ToolChain.ToolsX86TargetAmd64) {
                string toolsDir = Path.Combine(m_params.VcBinDir, "x86_amd64");
                string batchFilePath = Path.Combine(toolsDir, "vcvarsx86_amd64.bat");
                return batchFilePath;
            }
            else if (m_params.ToolChain == Msvc9ToolChain.ToolsAmd64TargetAmd64) {
                string toolsDir = Path.Combine(m_params.VcBinDir, "amd64");
                string batchFilePath = Path.Combine(toolsDir, "vcvarsamd64.bat");
                return batchFilePath;
            }
            return null;
        }

        private readonly Msvc9CompileParams m_params;
    }
}
