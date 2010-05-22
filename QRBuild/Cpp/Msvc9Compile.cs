using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using System.CodeDom.Compiler;
using Microsoft.CSharp;

using QRBuild.Engine;
using QRBuild.IO;
using QRBuild.Linq;

namespace QRBuild.Cpp
{
    public sealed class Msvc9Compile : BuildTranslation
    {
        public Msvc9Compile(BuildGraph buildGraph, Msvc9CompileParams p)
            : base(buildGraph)
        {
            m_params = p;
        }

        public override bool Execute()
        {
            //  TODO: this passes arguments directly; use response file instead
            //  TODO: use separate process to launch the build, for handles isolation

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = null;
            if (!String.IsNullOrEmpty(m_params.CompileDir)) {
                processStartInfo.WorkingDirectory = m_params.CompileDir;
            }
            else {
                processStartInfo.WorkingDirectory = Path.GetDirectoryName(m_params.SourceFile);
            }
            processStartInfo.Arguments = m_params.ToArgumentString();
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;

            Console.WriteLine("{0} {1}", processStartInfo.FileName, processStartInfo.Arguments);

            try
            {
                using (Process process = Process.Start(processStartInfo))
                {
                    // TODO: need to wait on cancellation as well?
                    process.WaitForExit();
                    Console.Write(process.StandardOutput.ReadToEnd());
                    bool result = process.ExitCode == 0;
                    return result;
                }
            }
            catch (System.Exception)
            {
                Console.WriteLine(">> process failed to start");
                return false;
            }
        }

        public override string PrimaryOutputFilePath
        {
            get { return m_params.ObjectPath; }
        }

        public override string GetCacheableTranslationParameters()
        {
            return m_params.ToArgumentString();
        }

        protected override void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            inputs.Add(m_params.SourceFile);

            string objFilePath = QRPath.ComputeDefaultFilePath(m_params.ObjectPath, m_params.SourceFile, ".obj");
            outputs.Add(objFilePath);
            if (m_params.DebugInfoFormat != Msvc9DebugInfoFormat.None) {
                string pdbFilePath = QRPath.ComputeDefaultFilePath(m_params.PdbPath, m_params.SourceFile, ".pdb");
                outputs.Add(pdbFilePath);
            }
            if (m_params.AsmOutputFormat != Msvc9AsmOutputFormat.None) {
                string asmFilePath = QRPath.ComputeDefaultFilePath(m_params.PdbPath, m_params.SourceFile, ".asm");
                outputs.Add(asmFilePath);
            }
            if (!String.IsNullOrEmpty(m_params.CreatePchFilePath)) {
                string pchFilePath = QRPath.ComputeDefaultFilePath(m_params.CreatePchFilePath, m_params.SourceFile, ".pch");
                outputs.Add(pchFilePath);
            }
        }

        protected override bool ComputeImplicitInputs(HashSet<string> inputs)
        {
            //  TODO: preprocess to determine headers
            return true;
        }

        private readonly Msvc9CompileParams m_params;
    }
}
