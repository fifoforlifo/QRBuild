using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using QRBuild;
using QRBuild.IO;
using QRBuild.Translations;
using QRBuild.Translations.IO;
using QRBuild.Translations.ToolChain.Msvc;

namespace QRBuild
{
    public sealed class GenerateHeader : BuildTranslation
    {
        public GenerateHeader(
            BuildGraph buildGraph, 
            string fileName, 
            string compileDir,
            string buildFileDir)
            : base(buildGraph)
        {
            m_fileName = QRPath.GetAbsolutePath(fileName, compileDir);
            m_buildFileDir = QRPath.GetCanonical(buildFileDir);
        }

        public override bool Execute()
        {
            string fileContents = @"
#ifndef __header__
#define __header__

const char* a();
const char* b();
const char* c();

#endif
";
            File.WriteAllText(m_fileName, fileContents);
            return true;
        }

        public override HashSet<string> GetIntermediateBuildFiles()
        {
            HashSet<string> result = new HashSet<string>();
            return result;
        }

        public override string BuildFileBaseName
        {
            get 
            {
                if (String.IsNullOrEmpty(m_buildFileBaseName)) {
                    string fileName = Path.GetFileName(m_fileName);
                    m_buildFileBaseName = Path.Combine(m_buildFileDir, fileName);
                }
                return m_buildFileBaseName; 
            }
        }

        public override string GetCacheableTranslationParameters()
        {
            return "";
        }

        public override bool RequiresImplicitInputs
        {
            get { return false; }
        }

        protected override void ComputeExplicitIO(HashSet<string> inputs, HashSet<string> outputs)
        {
            outputs.Add(m_fileName);
        }

        protected override bool ComputeImplicitIO(HashSet<string> inputs)
        {
            return true;
        }

        private readonly string m_fileName;
        private readonly string m_buildFileDir;
        private string m_buildFileBaseName;
    }
    
    public static class GeneratedHeaderTest
    {
        static string vcBinDir = @"C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin";
        static MsvcToolChain toolChain = MsvcToolChain.ToolsX86TargetX86;
        static string compileDir = @"K:\work\code\C#\QRBuild\Tests\GenH";
        static string buildFileDir = @"K:\work\code\C#\QRBuild\Tests\GenH\built";

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

        static MsvcLink LinkExecutable(
            BuildGraph buildGraph,
            string outputName,
            params string[] objFiles)
        {
            var linkerParams = new MsvcLinkerParams();
            linkerParams.VcBinDir = vcBinDir;
            linkerParams.ToolChain = toolChain;
            linkerParams.CompileDir = compileDir;
            linkerParams.BuildFileDir = buildFileDir;
            foreach (string objFile in objFiles) {
                linkerParams.Inputs.Add(objFile);
            }
            linkerParams.OutputFilePath = outputName;
            var link = new MsvcLink(buildGraph, linkerParams);
            return link;
        }

        public static void DoTest()
        {
            var buildGraph = new BuildGraph();
            Directory.CreateDirectory(buildFileDir);

            string windowsSdkDir = @"C:\Program Files\Microsoft SDKs\Windows\v6.0A";

            var cc_a    = CompileOne(buildGraph, "a.cpp");
            var cc_b    = CompileOne(buildGraph, "b.cpp");
            var cc_c    = CompileOne(buildGraph, "c.cpp");
            var cc_main = CompileOne(buildGraph, "main.cpp");

            string kernel32Lib = Path.Combine(windowsSdkDir, @"Lib\kernel32.lib");
            // NOTE: linker order matters; the linker translation respects that
            var link = LinkExecutable(
                buildGraph, 
                "main.exe",
                cc_a.Params.ObjectPath,
                cc_b.Params.ObjectPath,
                cc_c.Params.ObjectPath,
                cc_main.Params.ObjectPath,
                kernel32Lib);

            var generateHeader = new GenerateHeader(
                buildGraph, 
                "generated.h", 
                compileDir,
                buildFileDir);

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

        static MsvcCompile PpAndCompileOne(BuildGraph buildGraph, string sourceFile)
        {
            string sourceFileName = Path.GetFileName(sourceFile);

            string iName = Path.Combine(buildFileDir, QRPath.ChangeExtension(sourceFileName, ".i"));
            var ppp = new MsvcPreProcessParams();
            ppp.VcBinDir = vcBinDir;
            ppp.ToolChain = toolChain;
            ppp.CompileDir = compileDir;
            ppp.BuildFileDir = buildFileDir;
            ppp.SourceFile = sourceFile;
            ppp.OutputPath = iName;
            ppp.IncludeDirs.Add(@"K:\work\code\lib\boost_1_43_0");
            var pp = new MsvcPreProcess(buildGraph, ppp);

            string objName = Path.Combine(buildFileDir, QRPath.ChangeExtension(sourceFileName, ".obj"));
            var ccp = new MsvcCompileParams();
            ccp.VcBinDir = vcBinDir;
            ccp.ToolChain = toolChain;
            ccp.CompileDir = compileDir;
            ccp.BuildFileDir = buildFileDir;
            ccp.CheckForImplicitIO = false;
            ccp.SourceFile = iName;
            ccp.ObjectPath = objName;
            ccp.Compile = true;
            ccp.DebugInfoFormat = MsvcDebugInfoFormat.Normal;
            ccp.CppExceptions = MsvcCppExceptions.Enabled;
            ccp.CompileAsCpp = true;
            var cc = new MsvcCompile(buildGraph, ccp);
            return cc;
        }

        public static void DoTest2()
        {
            var buildGraph = new BuildGraph();
            Directory.CreateDirectory(buildFileDir);

            string windowsSdkDir = @"C:\Program Files\Microsoft SDKs\Windows\v6.0A";

            var cc_a    = PpAndCompileOne(buildGraph, "a.cpp");
            var cc_b    = PpAndCompileOne(buildGraph, "b.cpp");
            var cc_c    = PpAndCompileOne(buildGraph, "c.cpp");
            var cc_main = PpAndCompileOne(buildGraph, "main.cpp");

            string kernel32Lib = Path.Combine(windowsSdkDir, @"Lib\kernel32.lib");
            // NOTE: linker order matters; the linker translation respects that
            var link = LinkExecutable(
                buildGraph,
                "main.exe",
                cc_a.Params.ObjectPath,
                cc_b.Params.ObjectPath,
                cc_c.Params.ObjectPath,
                cc_main.Params.ObjectPath,
                kernel32Lib);

            var generateHeader = new GenerateHeader(
                buildGraph,
                "generated.h",
                compileDir,
                buildFileDir);

            BuildOptions buildOptions = new BuildOptions();
            buildOptions.ContinueOnError = false;
            buildOptions.FileDecider = new FileSizeDateDecider();
            buildOptions.MaxConcurrency = 4;

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
}
