using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using QRBuild.Cpp;
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

            using (QRProcess process = QRProcess.LaunchBatchFile(batPath, Path.GetDirectoryName(batPath), false, ""))
            {
                Console.WriteLine("launched pid={0}", process.Id);
                process.WaitHandle.WaitOne();
            }
            string log = File.ReadAllText(logPath);
            Console.Write(log);
        }

        static void TestCscCompile()
        {
            var cscp = new CSharpCompileParams();
            cscp.CompileDir = @"K:\work\code\C#\foo";
            string sourceFile = @"K:\work\code\C#\foo\Blah.cs";
            cscp.Sources.Add(sourceFile);
            cscp.OutputFilePath = QRPath.ChangeExtension(sourceFile, ".exe");
            cscp.FrameworkVersion = "v3.5";
            cscp.Platform = CSharpPlatforms.AnyCpu;
            cscp.Debug = true;

            var buildGraph = new BuildGraph();

            var csc = new CSharpCompile(buildGraph, cscp);
            csc.Execute();

            csc.UpdateExplicitIO();
            csc.UpdateImplicitIO();

            string depsCache = DependencyCache.CreateDepsCacheString(csc, new FileSizeDateDecider());
            File.WriteAllText(csc.PrimaryOutputFilePath + "__qr__.deps", depsCache);
        }

        static void TestMsvc9Compile()
        {
            var ccp = new Msvc9CompileParams();
            ccp.VcBinDir = @"C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin";
            ccp.ToolChain = Msvc9ToolChain.ToolsX86TargetX86;
            ccp.CompileDir = @"K:\work\code\cpp\0002_test";
            ccp.SourceFile = @"test02.cpp";
            ccp.Compile = true;
            ccp.DebugInfoFormat = Msvc9DebugInfoFormat.Normal;

            var buildGraph = new BuildGraph();

            var cc = new Msvc9Compile(buildGraph, ccp);
            cc.Execute();

            cc.UpdateExplicitIO();
            cc.UpdateImplicitIO();

            string depsCache = DependencyCache.CreateDepsCacheString(cc, new FileSizeDateDecider());
            File.WriteAllText(cc.DepsCacheFilePath, depsCache);

            HashSet<string> implicitInputs = new HashSet<string>();
            HashSet<string> implicitOutputs = new HashSet<string>();
            DependencyCache.LoadDepsCacheImplicitIO(cc.DepsCacheFilePath, implicitInputs, implicitOutputs);
        }

        static void TestSingleNodeGraph()
        {
            var ccp = new Msvc9CompileParams();
            ccp.VcBinDir = @"C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin";
            ccp.ToolChain = Msvc9ToolChain.ToolsX86TargetX86;
            ccp.CompileDir = @"K:\work\code\cpp\0002_test";
            ccp.SourceFile = @"test02.cpp";
            ccp.Compile = true;
            ccp.DebugInfoFormat = Msvc9DebugInfoFormat.Normal;

            var buildGraph = new BuildGraph();

            var cc = new Msvc9Compile(buildGraph, ccp);

            BuildOptions buildOptions = new BuildOptions();
            buildOptions.ContinueOnError = false;
            buildOptions.FileDecider = new FileSizeDateDecider();
            buildOptions.MaxConcurrency = 1;

            string[] targets = { @"K:\work\code\cpp\0002_test\test02.obj" };
            BuildResults buildResults = buildGraph.Execute(BuildAction.Build, buildOptions, targets, true);
            Console.WriteLine("BuildResults.Success          = {0}", buildResults.Success);
            Console.WriteLine("BuildResults.TranslationCount = {0}", buildResults.TranslationCount);
            Console.WriteLine("BuildResults.UpToDateCount    = {0}", buildResults.UpToDateCount);
        }

        static void Main(string[] args)
        {

#if false
            TestCscCompile();

            TestLaunchBatchFile();

            TestQRProcess();

            TestMsvc9Compile();
#endif

            TestSingleNodeGraph();

            Console.WriteLine(">> Press a key");
            Console.ReadKey();
        }
    }
}
