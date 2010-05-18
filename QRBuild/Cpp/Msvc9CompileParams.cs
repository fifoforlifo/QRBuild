using System;
using System.Collections.Generic;

namespace QRBuild.Cpp
{
    public enum Msvc9ClrSupport
    {
        None,
        Clr,
        ClrPure,
        ClrSafe,
        ClrOldSyntax,
    }

    public enum Msvc9AsmOutputFormat
    {
        None,
        AsmOnly,
        WithMachineCode,
        WithSourceCode,
        WithMachineAndSourceCode,
    }
    
    public enum Msvc9OptLevel
    {
        Disabled,
        MinimizeSpace,
        MaximizeSpeed,
        MaximumOptimizations,
        GlobalOptimizations,
    }

    public enum Msvc9SizeOrSpeed
    {
        Neither,
        Size,
        Speed,
    }
    
    public enum Msvc9InlineExpansion
    {
        Default,
        Disabled,
        OnlyExplicit,
        AutoInlining,
    }

    public enum Msvc9CppExceptions
    {
        Disabled,
        /// /EHsc
        Enabled,
        /// /EHa
        EnabledWithSeh,
    }

    public enum Msvc9RuntimeChecks
    {
        Default,
        /// /RTCs
        StackFrames,
        /// /RTCu
        UninitializedVariables,
        /// /RTCsu
        StackFramesAndUninitializedVariables,
    }

    public enum Msvc9RuntimeLibrary
    {
        MultiThreaded,
        MultiThreadedDll,
        MultiThreadedDebug,
        MultiThreadedDebugDll,
    }

    public enum Msvc9EnhancedIsa
    {
        Default,
        SSE,
        SSE2,
    }

    public enum Msvc9FloatingPointModel
    {
        Precise,
        Strict,
        Fast,
    }

    public enum Msvc9DebugInfoFormat
    {
        None,
        OldStyleC7,
        Normal,
        EditAndContinue,
    }

    /// Parameters for compiling C/C++ source to object using MSVC.
    public class Msvc9CompileParams
    {
        public Msvc9CompileParams()
        {
        }

        //-- Input and Output Options
        public string SourceFile;
        public string CompileDir;
        public bool Compile;
        public string ObjectPath;
        public string PdbPath;
        public Msvc9AsmOutputFormat AsmOutputFormat;
        public string AsmOutputPath;
        public Msvc9ClrSupport ClrSupport;

        //-- Optimization
        public Msvc9OptLevel OptLevel;
        public Msvc9InlineExpansion InlineExpansion;
        public bool EnableIntrinsicFunctions;
        public Msvc9SizeOrSpeed FavorSizeOrSpeed;
        public bool OmitFramePointers;

        //-- Code Generation
        public bool EnableStringPooling;
        public Msvc9CppExceptions CppExceptions;
        public bool ExternCNoThrow;
        public Msvc9RuntimeChecks BasicRuntimeChecks;
        public Msvc9RuntimeLibrary RuntimeLibrary;
        public int StructMemberAlignment = 8;
        public bool BufferSecurityCheck;
        public bool EnableFunctionLevelLinking;
        public Msvc9EnhancedIsa EnhancedIsa;
        public Msvc9FloatingPointModel FloatingPointModel = Msvc9FloatingPointModel.Precise;
        public bool EnableFloatingPointExceptions;
        public bool HotPatchable;

        //-- PreProcessor
        public List<string> Defines = new List<string>();
        public List<string> Undefines = new List<string>();
        public bool UndefineAllPredefinedMacros;
        public List<string> IncludeDirs = new List<string>();
        public List<string> AssemblySearchDirs = new List<string>();
        /// Also known as "prefix files", these files are implicitly
        /// included at the top of each translation unit.
        /// It's equivalent to having
        ///     #define "PrefixFile.h" 
        /// at the top of each source file.
        public List<string> ForcedIncludes = new List<string>();
        /// Forced #using assemblies.
        public List<string> ForcedUsings = new List<string>();
        /// Ignore PATH and INCLUDE environment variables.
        public bool IgnoreStandardPaths;

        //-- Language
        public Msvc9DebugInfoFormat DebugInfoFormat;
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
