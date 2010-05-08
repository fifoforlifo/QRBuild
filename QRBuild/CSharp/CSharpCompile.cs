using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using QRBuild.Engine;
using QRBuild.IO;
using QRBuild.Linq;

namespace QRBuild.CSharp
{
    public sealed class CSharpCompile : BuildTranslation
    {
        public CSharpCompile(BuildGraph buildGraph, CSharpCompileParams p)
            : base(buildGraph)
        {
            m_params = p;
        }

        public override bool Execute()
        {
            //  TODO: this passes arguments directly; use response file instead
            
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = m_params.CscPath;
            processStartInfo.WorkingDirectory = m_params.SourceRoot;
            processStartInfo.Arguments = m_params.ToArgumentString();
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;

            Console.WriteLine("{0} {1}", processStartInfo.FileName, processStartInfo.Arguments);

            try
            {
                using (Process process = Process.Start(processStartInfo))
                {
                    process.WaitForExit();
                    Console.Write(process.StandardOutput.ReadToEnd());
                }
            }
            catch (System.Exception)
            {
                Console.WriteLine(">> process failed to start");
            }
            return true;
        }

        public override string GetCacheableTranslationParameters()
        {
            return m_params.ToArgumentString();
        }

        protected override void ComputeInputs(HashSet<string> inputs)
        {
            inputs.AddRange(m_params.Sources);
            inputs.AddRange(m_params.InputModules);
            inputs.AddRange(m_params.AssemblyReferences);
        }

        protected override void ComputeOutputs(HashSet<string> outputs)
        {
            outputs.Add(m_params.OutputFilePath);
            if (!String.IsNullOrEmpty(m_params.PdbFilePath)) {
                outputs.Add(m_params.PdbFilePath);
            }
            else {
                // compiler's default behavior is to use Output filename with .pdb extension
                string pdbFilePath = QRPath.ChangeFileNameExtensionForPath(m_params.OutputFilePath, ".pdb");
                outputs.Add(pdbFilePath);
            }
        }

        protected override string GetDefaultDepsCacheFilePath()
        {
            if (String.IsNullOrEmpty(m_params.OutputFilePath)) {
                return null;
            }
            string depsCacheFilePath = m_params.OutputFilePath + ".deps";
            return depsCacheFilePath;
        }


        readonly CSharpCompileParams m_params;
    }
}
