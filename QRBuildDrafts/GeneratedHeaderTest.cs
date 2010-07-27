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
        public override bool GeneratesImplicitOutputs
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

        public static void DoTest()
        {
            var buildGraph = new BuildGraph();
            Directory.CreateDirectory(buildFileDir);

            var cc_a = CompileOne(buildGraph, "a.cpp");
            var cc_b = CompileOne(buildGraph, "b.cpp");
            var cc_c = CompileOne(buildGraph, "c.cpp");
            var cc_main = CompileOne(buildGraph, "main.cpp");

            var linkerParams = new MsvcLinkerParams();
            linkerParams.VcBinDir = vcBinDir;
            linkerParams.ToolChain = toolChain;
            linkerParams.CompileDir = compileDir;
            linkerParams.BuildFileDir = buildFileDir;
            linkerParams.Inputs.Add(cc_a.Params.ObjectPath);
            linkerParams.Inputs.Add(cc_b.Params.ObjectPath);
            linkerParams.Inputs.Add(cc_c.Params.ObjectPath);
            linkerParams.Inputs.Add(cc_main.Params.ObjectPath);
            linkerParams.OutputFilePath = "main.exe";
            var link = new MsvcLink(buildGraph, linkerParams);

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
}
