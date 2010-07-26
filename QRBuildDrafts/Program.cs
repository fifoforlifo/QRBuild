﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using QRBuild.Translations;
using QRBuild.Translations.ToolChain.Msvc9;
using QRBuild.Translations.ToolChain.MsCsc;
using QRBuild.Translations.IO;
using QRBuild.IO;

namespace QRBuild
{
    public class TestHelpers
    {
        public static void PrintBuildResults(BuildResults buildResults)
        {
            Console.WriteLine("===================================================");
            Console.WriteLine("BuildResults.Action                = {0}", buildResults.Action);
            Console.WriteLine("BuildResults.Success               = {0}", buildResults.Success);
            Console.WriteLine("BuildResults.TranslationCount      = {0}", buildResults.TranslationCount);
            Console.WriteLine("BuildResults.UpToDateCount         = {0}", buildResults.UpToDateCount);
            Console.WriteLine("BuildResults.UpdateImplicitInputsCount = {0}", buildResults.UpdateImplicitInputsCount);
        }
    }
    
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
                QRFile.Delete(logPath);
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
            csc.UpdateImplicitInputs();

            string depsCache = DependencyCache.CreateDepsCacheString(csc, new FileSizeDateDecider());
            File.WriteAllText(csc.BuildFileBaseName + "__qr__.deps", depsCache);
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
            cc.UpdateImplicitInputs();

            string depsCache = DependencyCache.CreateDepsCacheString(cc, new FileSizeDateDecider());
            File.WriteAllText(cc.DepsCacheFilePath, depsCache);

            HashSet<string> implicitInputs = new HashSet<string>();
            DependencyCache.LoadDepsCacheImplicitIO(cc.DepsCacheFilePath, implicitInputs);
        }

        static void TestSingleNodeGraph()
        {
            var buildGraph = new BuildGraph();

            var ccp = new Msvc9CompileParams();
            ccp.VcBinDir = @"C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin";
            ccp.ToolChain = Msvc9ToolChain.ToolsX86TargetX86;
            ccp.CompileDir = @"K:\work\code\cpp\0002_test";
            ccp.SourceFile = @"test02.cpp";
            ccp.Compile = true;
            ccp.DebugInfoFormat = Msvc9DebugInfoFormat.Normal;

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

        static void TestDependencyChain()
        {
            var buildGraph = new BuildGraph();
            
            string testDir = @"K:\work\code\C#\QRBuild\Tests\A";
            string intDir = Path.Combine(testDir, "int");
            string a = Path.Combine(testDir, "a");
            string b = Path.Combine(testDir, "b");
            string c = Path.Combine(testDir, "c");
            string d = Path.Combine(testDir, "d");
            if (!File.Exists(a)) {
                File.WriteAllText(a, "a");
            }

            var fcB = new FileCopy(buildGraph, a, b, intDir);
            var fcC = new FileCopy(buildGraph, b, c, intDir);
            var fcD = new FileCopy(buildGraph, c, d, intDir);

            BuildOptions buildOptions = new BuildOptions();
            buildOptions.ContinueOnError = false;
            buildOptions.FileDecider = new FileSizeDateDecider();
            buildOptions.MaxConcurrency = 1;

            string[] targets = { d };
            BuildResults buildResults = buildGraph.Execute(BuildAction.Build, buildOptions, targets, true);
            Console.WriteLine("BuildResults.Success          = {0}", buildResults.Success);
            Console.WriteLine("BuildResults.TranslationCount = {0}", buildResults.TranslationCount);
            Console.WriteLine("BuildResults.UpToDateCount    = {0}", buildResults.UpToDateCount);

            //QRFile.Delete(a, b, c, d);
        }

        class CompileLink1
        {
            static string vcBinDir = @"C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin";
            static Msvc9ToolChain toolChain = Msvc9ToolChain.ToolsX86TargetX86;
            static string compileDir = @"K:\work\code\C#\QRBuild\Tests\B";
            static string buildFileDir = @"K:\work\code\C#\QRBuild\Tests\B\built";

            static Msvc9Compile CompileOne(BuildGraph buildGraph, string sourceFile)
            {
                string sourceFileName = Path.GetFileName(sourceFile);
                string objName = Path.Combine(buildFileDir, QRPath.ChangeExtension(sourceFileName, ".obj"));

                var ccp = new Msvc9CompileParams();
                ccp.VcBinDir = vcBinDir;
                ccp.ToolChain = toolChain;
                ccp.CompileDir = compileDir;
                ccp.BuildFileDir = buildFileDir;
                ccp.SourceFile = sourceFile;
                ccp.ObjectPath = objName;
                ccp.Compile = true;
                ccp.DebugInfoFormat = Msvc9DebugInfoFormat.Normal;
                ccp.IncludeDirs.Add(@"K:\work\code\lib\boost_1_43_0");
                ccp.CppExceptions = Msvc9CppExceptions.Enabled;
                var cc = new Msvc9Compile(buildGraph, ccp);
                return cc;
            }

            public static void TestCppCompileLink()
            {
                var buildGraph = new BuildGraph();

                Directory.CreateDirectory(buildFileDir);

                var cc_test02 = CompileOne(buildGraph, "test02.cpp");
                var cc_foo = CompileOne(buildGraph, "foo.cpp");
                var cc_groo = CompileOne(buildGraph, "groo.cpp");
                var cc_qoo = CompileOne(buildGraph, "qoo.cpp");
                var cc_yoo = CompileOne(buildGraph, "yoo.cpp");
                var cc_aoo = CompileOne(buildGraph, "aoo.cpp");
                var cc_boo = CompileOne(buildGraph, "boo.cpp");
                var cc_coo = CompileOne(buildGraph, "coo.cpp");
                var cc_doo = CompileOne(buildGraph, "doo.cpp");

                var linkerParams = new Msvc9LinkerParams();
                linkerParams.VcBinDir = vcBinDir;
                linkerParams.ToolChain = toolChain;
                linkerParams.CompileDir = compileDir;
                linkerParams.BuildFileDir = buildFileDir;
                linkerParams.Inputs.Add(cc_aoo.Params.ObjectPath);
                linkerParams.Inputs.Add(cc_boo.Params.ObjectPath);
                linkerParams.Inputs.Add(cc_coo.Params.ObjectPath);
                linkerParams.Inputs.Add(cc_doo.Params.ObjectPath);
                linkerParams.Inputs.Add(cc_foo.Params.ObjectPath);
                linkerParams.Inputs.Add(cc_groo.Params.ObjectPath);
                linkerParams.Inputs.Add(cc_qoo.Params.ObjectPath);
                linkerParams.Inputs.Add(cc_test02.Params.ObjectPath);
                linkerParams.Inputs.Add(cc_yoo.Params.ObjectPath);
                linkerParams.OutputFilePath = "result.exe";
                var link = new Msvc9Link(buildGraph, linkerParams);

                BuildOptions buildOptions = new BuildOptions();
                buildOptions.ContinueOnError = false;
                buildOptions.FileDecider = new FileSizeDateDecider();
                buildOptions.MaxConcurrency = 6;

                string[] targets = { link.Params.OutputFilePath };

                BuildResults cleanBuildResults = buildGraph.Execute(BuildAction.Build, buildOptions, targets, true);
                TestHelpers.PrintBuildResults(cleanBuildResults);
                BuildResults incrementalBuildResults = buildGraph.Execute(BuildAction.Build, buildOptions, targets, true);
                TestHelpers.PrintBuildResults(incrementalBuildResults);

                bool doClean = true;
                if (doClean) {
                    BuildResults cleanResults = buildGraph.Execute(BuildAction.Clean, buildOptions, targets, true);
                    TestHelpers.PrintBuildResults(cleanResults);
                }
            }
        }

        static void Main(string[] args)
        {

#if false
            TestCscCompile();

            TestLaunchBatchFile();

            TestQRProcess();

            TestMsvc9Compile();
#endif

#if false
            TestSingleNodeGraph();
#endif

#if false
            TestDependencyChain();
#endif

#if false
            for (int i = 0; i < 100; i++) {
                CompileLink1.TestCppCompileLink();
            }
#endif

            for (int i = 0; i < 100; i++) {
                GeneratedHeaderTest.DoTest();
            }

            Console.WriteLine(">> Press a key");
            Console.ReadKey();
        }
    }
}
