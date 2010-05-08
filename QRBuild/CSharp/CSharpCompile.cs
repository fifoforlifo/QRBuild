using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace QRBuild.CSharp
{
    public sealed class CSharpCompile
    {
        public CSharpCompile(CSharpCompileParams p)
        {
            m_params = p;
        }

        public bool Execute()
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

        readonly CSharpCompileParams m_params;
    }
}
