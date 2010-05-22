using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using System.CodeDom.Compiler;
using Microsoft.CSharp;

using QRBuild.Engine;
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
            m_params = p;
        }

        public override bool Execute()
        {
            string responseFile = m_params.ToArgumentString();
            string responseFilePath = GetResponseFilePath();
            File.WriteAllText(responseFilePath, responseFile);

            string outputPath = GetBuildLogFilePath();

            string batchFile = String.Format(@"
SETLOCAL

cd ""{0}""

""{1}"" {2} @""{3}""  > ""{4}""  2>&1

ENDLOCAL
",
                m_params.CompileDir,
                @"C:\Windows\Microsoft.NET\Framework\v3.5\csc.exe",
                m_params.NoConfig ? "/noconfig" : "", 
                responseFilePath, 
                outputPath);

            string batchFilePath = GetBatchFilePath();
            File.WriteAllText(batchFilePath, batchFile);

            QRProcess process = QRProcess.LaunchBatchFile(batchFilePath, m_params.CompileDir);
            process.WaitHandle.WaitOne();

            string output = File.ReadAllText(outputPath);
            Console.Write(output);

            return process.GetExitCode() == 0;
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
        }

        protected override bool ComputeImplicitInputs(HashSet<string> inputs)
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

        readonly CSharpCompileParams m_params;
    }
}
