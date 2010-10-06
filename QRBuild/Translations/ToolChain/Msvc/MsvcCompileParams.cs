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
        Clr,
        ClrPure,
        ClrSafe,
        ClrOldSyntax,
    }

    public enum MsvcAsmOutputFormat
    {
        None,
        AsmOnly,
        WithMachineCode,
        WithSourceCode,
        WithMachineAndSourceCode,
    }
    
    public enum MsvcOptLevel
    {
        Disabled,
        MinimizeSpace,
        MaximizeSpeed,
        MaximumOptimizations,
        GlobalOptimizations,
    }

    public enum MsvcSizeOrSpeed
    {
        Neither,
        Size,
        Speed,
    }
    
    public enum MsvcInlineExpansion
    {
        Default,
        Disabled,
        OnlyExplicit,
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
        MultiThreaded,
        MultiThreadedDll,
        MultiThreadedDebug,
        MultiThreadedDebugDll,
    }

    public enum MsvcEnhancedIsa
    {
        Default,
        SSE,
        SSE2,
    }

    public enum MsvcFloatingPointModel
    {
        Precise,
        Strict,
        Fast,
    }

    public enum MsvcDebugInfoFormat
    {
        None,
        OldStyleC7,
        Normal,
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
        public bool Compile;
        public string ObjectPath;
        public string PdbPath;
        public MsvcAsmOutputFormat AsmOutputFormat;
        public string AsmOutputPath;
        public MsvcClrSupport ClrSupport;
        public string ExtraArgs;

        //-- Optimization
        public MsvcOptLevel OptLevel;
        public MsvcInlineExpansion InlineExpansion;
        public bool EnableIntrinsicFunctions;
        public MsvcSizeOrSpeed FavorSizeOrSpeed;
        public bool OmitFramePointers;

        //-- Code Generation
        public bool EnableStringPooling;
        public MsvcCppExceptions CppExceptions;
        public bool ExternCNoThrow;
        public MsvcRuntimeChecks BasicRuntimeChecks;
        public MsvcRuntimeLibrary RuntimeLibrary;
        public int StructMemberAlignment = 8;
        public bool BufferSecurityCheck;
        public bool EnableFunctionLevelLinking;
        public MsvcEnhancedIsa EnhancedIsa;
        public MsvcFloatingPointModel FloatingPointModel = MsvcFloatingPointModel.Precise;
        public bool EnableFloatingPointExceptions;
        public bool HotPatchable;

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

        //-- Language
        public MsvcDebugInfoFormat DebugInfoFormat;
        public bool EnableExtensions = true;
        public bool DefaultCharUnsigned;
        public bool Wchar_tBuiltIn = true;
        public bool ConformantForLoopScope = true;
        public bool EnableRtti = true;
        public bool EnableOpenMPSupport;
        public bool CompileAsC;
        public bool CompileAsCpp;

        //-- Warnings
        public bool DisableAllWarnings;
        public bool EnableAllWarnings;
        public int WarnLevel = 1;
        public bool TreatWarningsAsErrors;

        //-- PreCompiled Headers (PCH)
        public bool CreatePch;
        public string CreatePchFilePath;
        public string UsePchFilePath;
    }
}
