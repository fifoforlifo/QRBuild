using System;
using System.Collections.Generic;

namespace QRBuild.Translations.ToolChain.Msvc
{
    /// Selects the processor architecture of the toolchain.
    public enum MsvcToolChain
    {
        /// x86-tools that generate x86 output
        ToolsX86TargetX86,
        /// x86-tools that generate amd64 output
        /// Also known as cross-compiler or "cross-build" tools.
        ToolsX86TargetAmd64,
        /// amd64-tools that generate amd64 output
        ToolsAmd64TargetAmd64,
    }

    public enum MsvcClrSupport
    {
        None,
        /// /clr
        Clr,
        /// /clr:pure
        ClrPure,
        /// /clr:safe
        ClrSafe,
        /// /clr:oldsyntax
        ClrOldSyntax,
    }

    public enum MsvcAsmOutputFormat
    {
        None,
        /// /FA
        AsmOnly,
        /// /FAc
        WithMachineCode,
        /// /FAs
        WithSourceCode,
        /// /FAsc
        WithMachineAndSourceCode,
    }
    
    public enum MsvcOptLevel
    {
        /// /Od
        Disabled,
        /// /O1
        MinimizeSpace,
        /// /O2
        MaximizeSpeed,
        /// /Ox
        MaximumOptimizations,
        /// /Og
        GlobalOptimizations,
    }

    public enum MsvcSizeOrSpeed
    {
        Neither,
        /// /Os
        Size,
        /// /Ot
        Speed,
    }
    
    public enum MsvcInlineExpansion
    {
        Default,
        /// /Ob0
        Disabled,
        /// /Ob1
        OnlyExplicit,
        /// /Ob2
        AutoInlining,
    }

    public enum MsvcCppExceptions
    {
        Disabled,
        /// /EHsc
        Enabled,
        /// /EHa
        EnabledWithSeh,
    }

    public enum MsvcRuntimeChecks
    {
        Default,
        /// /RTCs
        StackFrames,
        /// /RTCu
        UninitializedVariables,
        /// /RTCsu
        StackFramesAndUninitializedVariables,
    }

    public enum MsvcRuntimeLibrary
    {
        /// /MT
        MultiThreaded,
        /// /MD
        MultiThreadedDll,
        /// /MTd
        MultiThreadedDebug,
        /// /MDd
        MultiThreadedDebugDll,
    }

    public enum MsvcEnhancedIsa
    {
        Default,
        /// /arch:SSE
        SSE,
        /// /arch:SSE2
        SSE2,
    }

    public enum MsvcFloatingPointModel
    {
        /// /fp:precise
        Precise,
        /// /fp:strict
        Strict,
        /// /fp:fast
        Fast,
    }

    public enum MsvcDebugInfoFormat
    {
        None,
        /// /Z7
        OldStyleC7,
        /// /Zi
        Normal,
        /// /ZI
        EditAndContinue,
    }

    /// Parameters for compiling C/C++ source to object using MSVC.
    public class MsvcCompileParams
    {
        public MsvcCompileParams()
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
        /// If true, check for implicit inputs by preprocessing.
        public bool CheckForImplicitIO = true;

        //-- Input and Output Options
        public string SourceFile;
        /// /c
        public bool Compile;
        /// /Fo
        public string ObjectPath;
        /// /Fd
        public string PdbPath;
        /// /FA*
        public MsvcAsmOutputFormat AsmOutputFormat;
        /// /Fa
        public string AsmOutputPath;
        /// /clr*
        public MsvcClrSupport ClrSupport;
        /// /nologo
        public bool NoLogo = true;
        public string ExtraArgs;

        //-- Optimization
        /// /O*
        public MsvcOptLevel OptLevel;
        /// /Ob*
        public MsvcInlineExpansion InlineExpansion;
        /// /Oi
        public bool EnableIntrinsicFunctions;
        /// /O*
        public MsvcSizeOrSpeed FavorSizeOrSpeed;
        /// /Oy
        public bool OmitFramePointers;

        //-- Code Generation
        /// /Gm
        public bool EnableMinimalRebuild;
        /// /GF
        public bool EnableStringPooling;
        /// /EH*
        public MsvcCppExceptions CppExceptions;
        public bool ExternCNoThrow;
        /// /RTC*
        public MsvcRuntimeChecks BasicRuntimeChecks;
        /// /{MT|MTd|MD|MDd}
        public MsvcRuntimeLibrary RuntimeLibrary;
        /// /Zp
        public int StructMemberAlignment = 8;
        /// /GS
        public bool BufferSecurityCheck;
        /// /Gy
        public bool EnableFunctionLevelLinking;
        /// /arch:
        public MsvcEnhancedIsa EnhancedIsa;
        /// /fp
        public MsvcFloatingPointModel FloatingPointModel = MsvcFloatingPointModel.Precise;
        /// /fp:except
        public bool EnableFloatingPointExceptions;
        /// /hotpatch
        public bool HotPatchable;

        //-- PreProcessor
        /// /D
        public readonly List<string> Defines = new List<string>();
        /// /U
        public readonly List<string> Undefines = new List<string>();
        /// /u
        public bool UndefineAllPredefinedMacros;
        /// /I
        public readonly List<string> IncludeDirs = new List<string>();
        /// /AI
        public readonly List<string> AssemblySearchDirs = new List<string>();
        /// /FI
        /// Also known as "prefix files", these files are implicitly
        /// included at the top of each translation unit.
        /// It's equivalent to having
        ///     #include "PrefixFile.h" 
        /// at the top of each source file.
        public readonly List<string> ForcedIncludes = new List<string>();
        /// /FU
        /// Forced #using assemblies.
        public readonly List<string> ForcedUsings = new List<string>();
        /// /X
        /// Ignore PATH and INCLUDE environment variables.
        public bool IgnoreStandardPaths;

        //-- Language
        /// /Z*
        public MsvcDebugInfoFormat DebugInfoFormat;
        /// /Za
        public bool EnableExtensions = true;
        /// /J
        public bool DefaultCharUnsigned;
        /// /Zc:wchar_t
        public bool Wchar_tBuiltIn = true;
        /// /Zc:forScope-
        public bool ConformantForLoopScope = true;
        /// /GR
        public bool EnableRtti = true;
        /// /openmp
        public bool EnableOpenMPSupport;
        /// /TC
        public bool CompileAsC;
        /// /TP
        public bool CompileAsCpp;

        //-- Warnings
        /// /w
        public bool DisableAllWarnings;
        /// /Wall
        public bool EnableAllWarnings;
        /// /W
        public int WarnLevel = 1;
        /// /WX
        public bool TreatWarningsAsErrors;
        /// /WL
        public bool SingleLineDiagnostics = true;

        //-- PreCompiled Headers (PCH)
        /// /Yc
        public bool CreatePch;
        /// /Fp
        public string CreatePchFilePath;
        /// /Yu
        public string UsePchFilePath;
    }
}
