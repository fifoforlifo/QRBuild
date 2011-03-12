using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using QRBuild.Text;
using QRBuild.Translations;
using QRBuild.Translations.ToolChain.Msvc;
using QRBuild.Translations.ToolChain.MsCsc;
using QRBuild.Translations.IO;
using QRBuild.IO;

using QRBuild.ProjectSystem;

namespace QRBuild
{
    public class TestHelpers
    {
        public static void PrintBuildResults(BuildOptions options, BuildResults buildResults)
        {
            Console.WriteLine("===================================================");
            Console.WriteLine("BuildResults.Action                = {0}", buildResults.Action);
            Console.WriteLine("BuildResults.Success               = {0}", buildResults.Success);
            Console.WriteLine("BuildResults.TranslationCount      = {0}", buildResults.TranslationCount);
            Console.WriteLine("BuildResults.UpToDateCount         = {0}", buildResults.UpToDateCount);
            Console.WriteLine("BuildResults.UpdateImplicitInputsCount = {0}", buildResults.UpdateImplicitInputsCount);
            Console.WriteLine("BuildOptions.FileDecider.FStatCount= {0}", options.FileDecider.FStatCount);
            TimeSpan executionTime = buildResults.ExecuteEndTime - buildResults.ExecuteStartTime;
            Console.WriteLine("# ExecutionTime = {0}", executionTime);
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

        static void TestMsvcCompile(string vcBinDir)
        {
            var ccp = new MsvcCompileParams();
            ccp.VcBinDir = vcBinDir;
            ccp.ToolChain = MsvcToolChain.ToolsX86TargetX86;
            ccp.CompileDir = @"K:\work\code\cpp\0002_test";
            ccp.SourceFile = @"test02.cpp";
            ccp.Compile = true;
            ccp.DebugInfoFormat = MsvcDebugInfoFormat.Normal;

            var buildGraph = new BuildGraph();

            var cc = new MsvcCompile(buildGraph, ccp);
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

            var ccp = new MsvcCompileParams();
            ccp.VcBinDir = @"C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin";
            ccp.ToolChain = MsvcToolChain.ToolsX86TargetX86;
            ccp.CompileDir = @"K:\work\code\cpp\0002_test";
            ccp.SourceFile = @"test02.cpp";
            ccp.Compile = true;
            ccp.DebugInfoFormat = MsvcDebugInfoFormat.Normal;

            var cc = new MsvcCompile(buildGraph, ccp);

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
            static MsvcToolChain toolChain = MsvcToolChain.ToolsX86TargetX86;
            static string compileDir = @"K:\work\code\C#\QRBuild\Tests\B";
            static string buildFileDir = @"K:\work\code\C#\QRBuild\Tests\B\built";

            static MsvcCompile CompileOne(BuildGraph buildGraph, string sourceFile)
            {
                string sourceFileName = Path.GetFileName(sourceFile);
                string objName = Path.Combine(buildFileDir, QRPath.ChangeExtension(sourceFileName, ".obj"));

                var ccp = new MsvcCompileParams();
                ccp.VcBinDir = vcBinDir;
                ccp.ToolChain = toolChain;
                ccp.CompileDir = compileDir;
                ccp.BuildFileDir = buildFileDir;
                ccp.SourceFile = sourceFile;
                ccp.ObjectPath = objName;
                ccp.Compile = true;
                ccp.DebugInfoFormat = MsvcDebugInfoFormat.Normal;
                ccp.IncludeDirs.Add(@"K:\work\code\lib\boost_1_43_0");
                ccp.CppExceptions = MsvcCppExceptions.Enabled;
                var cc = new MsvcCompile(buildGraph, ccp);
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

                var linkerParams = new MsvcLinkerParams();
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
                var link = new MsvcLink(buildGraph, linkerParams);

                BuildOptions buildOptions = new BuildOptions();
                buildOptions.ContinueOnError = false;
                buildOptions.FileDecider = new FileSizeDateDecider();
                buildOptions.MaxConcurrency = 6;

                string[] targets = { link.Params.OutputFilePath };

                BuildResults cleanBuildResults = buildGraph.Execute(BuildAction.Build, buildOptions, targets, true);
                TestHelpers.PrintBuildResults(buildOptions, cleanBuildResults);
                BuildResults incrementalBuildResults = buildGraph.Execute(BuildAction.Build, buildOptions, targets, true);
                TestHelpers.PrintBuildResults(buildOptions, incrementalBuildResults);

                bool doClean = true;
                if (doClean) {
                    BuildResults cleanResults = buildGraph.Execute(BuildAction.Clean, buildOptions, targets, true);
                    TestHelpers.PrintBuildResults(buildOptions, cleanResults);
                }
            }
        }

        public enum Configuration
        {
            Debug,
            Develop,
            Release,
        }
        public enum Platform
        {
            Win32,
            x64,
        }

        public class Variant : BuildVariant
        {
            [VariantPart(100)]
            public Configuration Configuration;

            [VariantPart(200)]
            public Platform Platform;

            [VariantPart(300)]
            public string IsDebacle = "YesItIs";
        }

        static void TestBuildVariant()
        {
            Variant v = new Variant();

            string format = v.GetVariantStringFormat();
            Console.WriteLine("VariantStringFormat  = {0}", format);
            string value = v.ToString();
            Console.WriteLine("VariantString        = {0}", value);
            string options = v.GetVariantStringOptions("\t");
            Console.WriteLine("VariantStringOptions = \n{0}", options);

            Variant v2 = new Variant();
            v2.FromString("Develop.x64.NopeNotThisTime");
            Console.WriteLine("FromString,ToString  = {0}", v2.ToString());
        }

        static void TestStringInterpolator()
        {
            StringInterpolator.Options options = new StringInterpolator.Options();
            IList<StringInterpolator.Token> tokens = StringInterpolator.Tokenize(
                options,
                @"c:\foo\$(bar) $(opt)");
            Func<string, string> getValue =
                name =>
                {
                    if (name == "bar") return "alpha.exe";
                    if (name == "opt") return "--skip=true";
                    return null;
                };
            string result = StringInterpolator.Interpolate(options, tokens, getValue);
            Console.WriteLine("");
        }

        static void Main(string[] args)
        {
            string dir = @"C:\one\two\three\four\";
            string dir4 = Path.GetDirectoryName(dir);
            string dir3 = Path.GetDirectoryName(dir4);
            string dir2 = Path.GetDirectoryName(dir3);
            string dir1 = Path.GetDirectoryName(dir2);
            string dir0 = Path.GetDirectoryName(dir1);
            string dirX = Path.GetDirectoryName(dir0);


#if false
            TestCscCompile();

            TestLaunchBatchFile();

            TestQRProcess();

            TestMsvcCompile(@"C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin";);
#endif

#if false
            TestSingleNodeGraph();
#endif

#if false
            TestDependencyChain();
#endif

#if false
            for (int i = 0; i < 1; i++) {
                CompileLink1.TestCppCompileLink();
            }
#endif

            const int numIterations = 20;

#if false
            DateTime startTime = DateTime.Now;
            for (int i = 0; i < numIterations; i++) {
                GeneratedHeaderTest.DoTest();
            }
            DateTime endTime = DateTime.Now;
            TimeSpan timeSpan = endTime - startTime;
            Console.WriteLine("TotalTestTime = {0}, AvgIterationTime = {1}",
                timeSpan,
                TimeSpan.FromTicks(timeSpan.Ticks / numIterations));

            Console.WriteLine(">> Press a key");
            Console.ReadKey();
#endif

#if false
            startTime = DateTime.Now;
            for (int i = 0; i < numIterations; i++) {
                GeneratedHeaderTest.DoTest2();
            }
            endTime = DateTime.Now;
            timeSpan = endTime - startTime;
            Console.WriteLine("TotalTestTime = {0}, AvgIterationTime = {1}",
                timeSpan,
                TimeSpan.FromTicks(timeSpan.Ticks / numIterations));
            
            Console.WriteLine(">> Press a key");
            Console.ReadKey();
#endif

#if false
            TestBuildVariant();
#endif

#if true
            TestStringInterpolator();
#endif

        }
    }
}
