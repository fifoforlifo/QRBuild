using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using QRBuild.CSharp;
using QRBuild.Engine;
using QRBuild.IO;
using QRBuild.Translations;

namespace QRBuild
{
    class Program
    {
        static bool TestLaunchBatchFile()
        {
            string logPath = @"K:\work\code\cpp\0002_test\test02.o.log";

            string bat = String.Format(@"
SETLOCAL

call ""C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin\vcvars32.bat""

cl   test02.cpp /nologo /c /Fotest02.o  > ""{0}"" 2>&1 

ENDLOCAL
", logPath);;

            string batPath = @"K:\work\code\cpp\0002_test\bldgen.bat";
            File.WriteAllText(batPath, bat, Encoding.ASCII);

            var psi = new ProcessStartInfo();
            psi.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            psi.WorkingDirectory = Path.GetDirectoryName(batPath);
            psi.Arguments = String.Format("/C \"{0}\" ", batPath);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = false;
            psi.ErrorDialog = false;

            Console.WriteLine("{0} {1}", psi.FileName, psi.Arguments);

            try {
                File.Delete(logPath);
                using (Process process = Process.Start(psi)) {
                    // TODO: need to wait on cancellation as well?
                    process.WaitForExit();
                    string log = File.ReadAllText(logPath);
                    Console.Write(log);
                    bool result = process.ExitCode == 0;
                    return result;
                }
            }
            catch (System.Exception) {
                Console.WriteLine(">> process failed to start");
                return false;
            }
        }

        static void TestQRProcess()
        {
            string logPath = @"K:\work\code\cpp\0002_test\test02.o.log";

            string bat = String.Format(@"
@echo off
SETLOCAL

call ""C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin\vcvars32.bat""

cl   test02.cpp /nologo /c /Fotest02.o  > ""{0}"" 2>&1 

ENDLOCAL
", logPath); ;

            string batPath = @"K:\work\code\cpp\0002_test\bldgen2.bat";
            File.WriteAllText(batPath, bat, Encoding.ASCII);

            using (QRProcess process = QRProcess.LaunchBatchFile(batPath, Path.GetDirectoryName(batPath)))
            {
                Console.WriteLine("launched pid={0}", process.Id);
                process.WaitHandle.WaitOne();
            }
            string log = File.ReadAllText(logPath);
            Console.Write(log);
        }
        
        static void Main(string[] args)
        {
            var cscp = new CSharpCompileParams();
            cscp.CompileDir = @"K:\work\code\C#\foo";
            string sourceFile = @"K:\work\code\C#\foo\Blah.cs";
            cscp.Sources.Add(sourceFile);
            cscp.OutputFilePath = QRPath.ChangeExtension(sourceFile, ".exe");
            cscp.Platform = CSharpPlatforms.AnyCpu;
            cscp.Debug = true;

            var buildGraph = new BuildGraph();

            var csc = new CSharpCompile(buildGraph, cscp);
            csc.Execute();

#if false
            TestLaunchBatchFile();

            TestQRProcess();
#endif

            {
                string a = Path.GetDirectoryName("a/b.txt");
                string nodir = Path.GetDirectoryName("c.txt");
                string nodir2 = Path.GetDirectoryName("a/");
            }

            Console.WriteLine(">> Press a key");
            Console.ReadKey();
        }
    }
}
