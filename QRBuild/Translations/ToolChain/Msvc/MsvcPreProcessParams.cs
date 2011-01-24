using System.Collections.Generic;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public class MsvcPreProcessParams
    {
        public MsvcPreProcessParams()
        {
        }


        //-- Meta
        public string CompileDir;
        public string BuildFileDir;
        /// VcBinDir typically will be 
        /// 32-bit OS : %ProgramFiles%\Microsoft Visual Studio 9.0\VC\bin
        /// 64-bit OS : %ProgramFiles(x86)%\Microsoft Visual Studio 9.0\VC\bin
        public string VcBinDir;
        public MsvcToolChain ToolChain;

        //-- Input and Output Options
        public string SourceFile;
        public string OutputPath;
        public string ExtraArgs;


        //-- PreProcessor
        public readonly List<string> Defines = new List<string>();
        public readonly List<string> Undefines = new List<string>();
        public bool UndefineAllPredefinedMacros;
        public readonly List<string> IncludeDirs = new List<string>();
        public readonly List<string> AssemblySearchDirs = new List<string>();
        /// Also known as "prefix files", these files are implicitly
        /// included at the top of each translation unit.
        /// It's equivalent to having
        ///     #define "PrefixFile.h" 
        /// at the top of each source file.
        public readonly List<string> ForcedIncludes = new List<string>();
        /// Forced #using assemblies.
        public readonly List<string> ForcedUsings = new List<string>();
        /// Ignore PATH and INCLUDE environment variables.
        public bool IgnoreStandardPaths;

        //-- Warnings
        public bool DisableAllWarnings;
        public bool EnableAllWarnings;
        public int WarnLevel = 1;
        public bool TreatWarningsAsErrors;
    }
}
