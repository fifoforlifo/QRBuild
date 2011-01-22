using System;
using System.Collections.Generic;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public enum MsvcClrImageType
    {
        Default,
        IJW,
        Pure,
        Safe,
    }

    [Flags]
    public enum MsvcForce
    {
        Default = Multiple | Unresolved,
        Multiple = 1,
        Unresolved = 2,
    }

    public enum MsvcSubSystem
    {
        None,
        Console,
        Windows,
        Native,
        Posix,
    }
    
    public enum MsvcOptRef
    {
        Default,
        /// /OPT:REF causes unreferenced symbols to be deadstripped
        OptRef,
        /// /OPT:NOREF does not deadstrip symbols
        OptNoRef,
    }

    public class MsvcLinkerParams
    {
        public MsvcLinkerParams()
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
        public string AdditionalOptions;

        //-- Input
        /// Inputs contains file paths that will be resolved to full paths
        /// based on the CompileDir.
        /// Inputs are *not* searched for through the LIB env-var nor through library paths.
        public readonly List<string> Inputs = new List<string>();
        /// /ASSEMBLYMODULE:
        public readonly List<string> InputModules = new List<string>();
        /// /DEF:
        public string DefFilePath;
        /// /DEFAULTLIB:
        public readonly List<string> DefaultLib = new List<string>();
        /// /NODEFAULTLIB:
        public readonly List<string> NoDefaultLib = new List<string>();
        
        //-- Output
        /// /DEBUG
        public bool Debug;
        /// /DLL  (if false, an executable is generated)
        public bool Dll;
        /// /SUBSYSTEM:
        public MsvcSubSystem SubSystem;
        /// /OUT:
        public string OutputFilePath;
        /// /PDB:
        public string PdbFilePath;
        /// /IMPLIB:
        public string ImpLibPath;
        /// /MAP:
        public string MapFilePath;

        //-- Options        
        /// /DELAYLOAD:dll
        public readonly List<string> DelayLoad = new List<string>();
        /// /ENTRY:
        public string Entry;
        /// /EXPORT:
        public readonly List<string> Export = new List<string>();
        /// /FORCE:
        public MsvcForce Force;
        /// /INCLUDE:
        public readonly List<string> Include = new List<string>();
        /// /INCREMENTAL:NO
        public bool Incremental = true;
        /// /NOASSEMBLY
        public bool NoAssembly;
        /// /NOLOGO
        public bool NoLogo = true;
        /// /NXCOMPAT:
        public bool NxCompat;
        /// /OPT:REF
        public MsvcOptRef OptRef;
        /// /STACK:
        public string Stack;
        /// /VERBOSE
        public bool Verbose;
        /// /VERSION:
        public string Version;
        /// /WX
        public bool WarningsAsErrors;
    }
}
