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
            var psi = new ProcessStartInfo();
            psi.FileName = m_params.CscPath;
            psi.WorkingDirectory = m_params.SourceRoot;
            psi.Arguments = m_params.ToArgumentString();
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;

            Console.WriteLine("{0} {1}", psi.FileName, psi.Arguments);

            try
            {
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    Console.Write(process.StandardOutput.ReadToEnd());
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine(">> process failed to start");
            }
            return true;
        }

        readonly CSharpCompileParams m_params;
    }
}
