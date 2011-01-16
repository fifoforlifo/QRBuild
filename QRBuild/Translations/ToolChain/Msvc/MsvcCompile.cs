using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using QRBuild.IO;
using QRBuild.Linq;
using QRBuild.Translations;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public sealed class MsvcCompile : BuildTranslation
    {
        public MsvcCompile(BuildGraph buildGraph, MsvcCompileParams p)
            : base(buildGraph)
        {
            m_params = p.Canonicalize();
        }

        public MsvcCompileParams Params
        {
            get { return m_params; }
        }

        public override bool Execute()
        {
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

cl @""{5}""

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
            result.Add(GetPpBatchFilePath());
            result.Add(GetPpResponseFilePath());
            result.Add(GetPreProcessedFilePath());
            result.Add(GetShowIncludesFilePath());
            return result;
        }

        public override string BuildFileBaseName
        {
            get 
            {
                if (String.IsNullOrEmpty(m_buildFileBaseName)) {
                    if (m_params.Compile) {
                        string fileName = Path.GetFileName(m_params.ObjectPath);
                        m_buildFileBaseName = Path.Combine(m_params.BuildFileDir, fileName);
                    }
                    else if (m_params.CreatePch) {
                        string fileName = Path.GetFileName(m_params.CreatePchFilePath);
                        m_buildFileBaseName = Path.Combine(m_params.BuildFileDir, fileName);
                    }
                    else {
                        throw new InvalidOperationException("No valid output file specified for this Translation");
                    }
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
            get { return m_params.CheckForImplicitIO; }
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
            string objFilePath = QRPath.ComputeDefaultFilePath(m_params.ObjectPath, m_params.SourceFile, ".obj", m_params.CompileDir);
            outputs.Add(objFilePath);
            if (m_params.DebugInfoFormat != MsvcDebugInfoFormat.None) {
                string pdbFilePath = QRPath.ComputeDefaultFilePath(m_params.PdbPath, m_params.SourceFile, ".pdb", m_params.CompileDir);
                outputs.Add(pdbFilePath);
            }
            if (m_params.AsmOutputFormat != MsvcAsmOutputFormat.None) {
                string asmFilePath = QRPath.ComputeDefaultFilePath(m_params.PdbPath, m_params.SourceFile, ".asm", m_params.CompileDir);
                outputs.Add(asmFilePath);
            }
            if (!String.IsNullOrEmpty(m_params.CreatePchFilePath)) {
                string pchFilePath = QRPath.ComputeDefaultFilePath(m_params.CreatePchFilePath, m_params.SourceFile, ".pch", m_params.CompileDir);
                outputs.Add(pchFilePath);
            }
        }

        protected override bool ComputeImplicitIO(HashSet<string> inputs)
        {
            if (!m_params.CheckForImplicitIO) {
                return true;
            }

            string responseFile = m_params.ToPreProcessorArgumentString(true);
            string responseFilePath = GetPpResponseFilePath();
            File.WriteAllText(responseFilePath, responseFile);

            string showIncludesFilePath = GetShowIncludesFilePath();
            string preProcessedFilePath = GetPreProcessedFilePath();

            string batchFilePath = GetPpBatchFilePath();
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
                showIncludesFilePath);

            File.WriteAllText(batchFilePath, batchFile);

            using (QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir, false, "stdout")) {
                process.WaitHandle.WaitOne();
                // The process' exit code is ignored, because "missing header" errors
                // are not real errors if it's possible for the file to be generated.
            }

            string showIncludesFileContents = File.ReadAllText(showIncludesFilePath);
            bool ppSuccess = MsvcUtility.ParseShowIncludesText(
                this.BuildGraph,
                showIncludesFileContents,
                m_params.CompileDir,
                m_params.IncludeDirs,
                inputs);
            return ppSuccess;
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
        private string GetPpBatchFilePath()
        {
            return BuildFileBaseName + "__qr__._pp.bat";
        }
        private string GetPpResponseFilePath()
        {
            return BuildFileBaseName + "__qr__._pp.rsp";
        }
        private string GetPreProcessedFilePath()
        {
            return BuildFileBaseName + "__qr__._pp.i";
        }
        private string GetShowIncludesFilePath()
        {
            return BuildFileBaseName + "__qr__._pp.d";
        }

        private readonly MsvcCompileParams m_params;
        private string m_buildFileBaseName;
    }
}
