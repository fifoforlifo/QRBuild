using System;
using System.Collections.Generic;

namespace QRBuild.Translations.ToolChain.Msvc
{
    /// LIB.EXE has multiple modes of operation.
    /// 
    public enum MsvcLibOutputType
    {
        /// Ordinary archive (collection) of object files.
        StaticLibrary,
        /// Import library that would be created if you were to
        /// link the specified object files into a DLL.  See here:
        /// http://msdn.microsoft.com/en-us/library/0b9xe492(v=VS.90).aspx
        ImportLibrary,
    }

    public class MsvcLibParams
    {
        public MsvcLibParams()
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
        /// Controls whether to use /DEF to generate an import library.
        /// This option exists to make output-type an explicit choice, rather
        /// than being implicit on whether /DEF was specified (as the toolchain does).
        public MsvcLibOutputType OutputType;

        //-- Input
        /// Inputs contains file paths that will be resolved to full paths
        /// based on the CompileDir.
        /// Inputs are *not* searched for through the LIB env-var nor through library paths.
        public readonly List<string> Inputs = new List<string>();
        /// /NODEFAULTLIB:
        public readonly List<string> NoDefaultLib = new List<string>();
        /// /DEF:
        public string DefFilePath;

        //-- Output
        /// /SUBSYSTEM:
        public MsvcSubSystem SubSystem;
        /// /OUT:
        public string OutputFilePath;

        //-- Options
        /// /EXPORT:
        public readonly List<string> Export = new List<string>();
        /// /INCLUDE:
        public readonly List<string> Include = new List<string>();
        /// /LTCG
        public bool LinkTimeCodeGeneration;
        /// /NAME:
        /// Only applies if OutputType is set to ImportLibrary.
        public string DllNameForImportLibrary;
        /// /NOLOGO
        public bool NoLogo = true;
        /// /VERBOSE
        public bool Verbose;
        /// /WX
        public bool WarningsAsErrors;
    }
}