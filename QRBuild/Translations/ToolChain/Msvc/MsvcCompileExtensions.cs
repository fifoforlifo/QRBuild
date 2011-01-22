using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QRBuild.IO;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public static class MsvcCompileExtensions
    {
        /// Returns a new canonicalized instance of compiler params.
        public static MsvcCompileParams Canonicalize(this MsvcCompileParams p)
        {
            MsvcCompileParams o = new MsvcCompileParams();
            //-- Meta
            if (String.IsNullOrEmpty(p.VcBinDir)) {
                throw new InvalidOperationException("VcBinDir not specified");
            }
            if (String.IsNullOrEmpty(p.CompileDir)) {
                throw new InvalidOperationException("CompileDir not specified");
            }
            o.CompileDir = QRPath.GetCanonical(p.CompileDir);
            o.BuildFileDir = String.IsNullOrEmpty(p.BuildFileDir)
                ? p.CompileDir
                : QRPath.GetCanonical(p.BuildFileDir);
            o.VcBinDir = QRPath.GetAbsolutePath(p.VcBinDir, o.CompileDir);
            o.ToolChain = p.ToolChain;
            o.CheckForImplicitIO = p.CheckForImplicitIO;

            //-- Input and Output Options
            if (String.IsNullOrEmpty(p.SourceFile)) {
                throw new InvalidOperationException("C/C++ SourceFile not specified");
            }
            o.SourceFile = QRPath.GetAbsolutePath(p.SourceFile, o.CompileDir);
            o.Compile = p.Compile;
            if (p.Compile) {
                o.ObjectPath = QRPath.ComputeDefaultFilePath(p.ObjectPath, p.SourceFile, ".obj", o.CompileDir);
                o.PdbPath = QRPath.ComputeDefaultFilePath(p.PdbPath, o.ObjectPath, ".pdb", o.CompileDir);
            }
            o.AsmOutputFormat = p.AsmOutputFormat;
            if (p.AsmOutputFormat != MsvcAsmOutputFormat.None) {
                o.AsmOutputPath = QRPath.ComputeDefaultFilePath(p.AsmOutputPath, p.SourceFile, ".asm", o.CompileDir);
            }
            o.ClrSupport = p.ClrSupport;
            o.ExtraArgs = p.ExtraArgs;

            //-- Optimization
            o.OptLevel = p.OptLevel;
            o.InlineExpansion = p.InlineExpansion;
            o.EnableIntrinsicFunctions = p.EnableIntrinsicFunctions;
            o.FavorSizeOrSpeed = p.FavorSizeOrSpeed;
            o.OmitFramePointers = p.OmitFramePointers;

            //-- Code Generation
            o.EnableMinimalRebuild = p.EnableMinimalRebuild;
            o.EnableStringPooling = p.EnableStringPooling;
            o.CppExceptions = p.CppExceptions;
            o.ExternCNoThrow = p.ExternCNoThrow;
            o.BasicRuntimeChecks = p.BasicRuntimeChecks;
            o.RuntimeLibrary = p.RuntimeLibrary;
            o.StructMemberAlignment = p.StructMemberAlignment;
            o.BufferSecurityCheck = p.BufferSecurityCheck;
            o.EnableFunctionLevelLinking = p.EnableFunctionLevelLinking;
            o.EnhancedIsa = p.EnhancedIsa;
            o.FloatingPointModel = p.FloatingPointModel;
            o.EnableFloatingPointExceptions = p.EnableFloatingPointExceptions;
            o.HotPatchable = p.HotPatchable;

            //-- PreProcessor
            o.Defines.AddRange(p.Defines);
            o.Undefines.AddRange(p.Undefines);
            o.UndefineAllPredefinedMacros = p.UndefineAllPredefinedMacros;
            o.IncludeDirs.AddRangeAsAbsolutePaths(p.IncludeDirs, o.CompileDir);
            o.AssemblySearchDirs.AddRangeAsAbsolutePaths(p.AssemblySearchDirs, o.CompileDir);
            o.ForcedIncludes.AddRangeAsAbsolutePaths(p.ForcedIncludes, o.CompileDir);
            o.ForcedUsings.AddRangeAsAbsolutePaths(p.ForcedUsings, o.CompileDir);
            o.IgnoreStandardPaths = p.IgnoreStandardPaths;

            //-- Language
            o.DebugInfoFormat = p.DebugInfoFormat;
            o.EnableExtensions = p.EnableExtensions;
            o.DefaultCharUnsigned = p.DefaultCharUnsigned;
            o.Wchar_tBuiltIn = p.Wchar_tBuiltIn;
            o.ConformantForLoopScope = p.ConformantForLoopScope;
            o.EnableRtti = p.EnableRtti;
            o.EnableOpenMPSupport = p.EnableOpenMPSupport;
            o.CompileAsC = p.CompileAsC;
            o.CompileAsCpp = p.CompileAsCpp;

            //-- Warnings
            o.DisableAllWarnings = p.DisableAllWarnings;
            o.EnableAllWarnings = p.EnableAllWarnings;
            o.WarnLevel = p.WarnLevel;
            o.TreatWarningsAsErrors = p.TreatWarningsAsErrors;
            
            if (p.CreatePch) {
                o.CreatePchFilePath = QRPath.ComputeDefaultFilePath(p.CreatePchFilePath, p.SourceFile, ".pch", o.CompileDir);
            }
            if (!String.IsNullOrEmpty(p.UsePchFilePath)) {
                o.UsePchFilePath = QRPath.GetAbsolutePath(p.UsePchFilePath, o.CompileDir);
            }

            return o;
        }
        
        /// This function assumes p is canonicalized.
        public static string ToArgumentString(this MsvcCompileParams p)
        {
            StringBuilder b = new StringBuilder();
            b.AppendFormat("\"{0}\" ", p.SourceFile);
            b.Append("/nologo ");   // do not print logo to stdout
            b.Append("/FC ");       // show full path in diagnostic messages

            //-- Input and Output Options
            if (!String.IsNullOrEmpty(p.ObjectPath)) {
                b.AppendFormat("/c /Fo\"{0}\" ", p.ObjectPath);
            }
            if (!String.IsNullOrEmpty(p.PdbPath)) {
                b.AppendFormat("/Fd\"{0}\" ", p.PdbPath);
            }
            switch (p.AsmOutputFormat)
            {
                case MsvcAsmOutputFormat.AsmOnly: 
                    b.Append("/FA ");
                    break;
                case MsvcAsmOutputFormat.WithMachineCode:
                    b.Append("/FAc ");
                    break;
                case MsvcAsmOutputFormat.WithSourceCode:
                    b.AppendFormat("/FAs ");
                    break;
                case MsvcAsmOutputFormat.WithMachineAndSourceCode:
                    b.AppendFormat("/FAsc ");
                    break;
            }                
            if (!String.IsNullOrEmpty(p.AsmOutputPath)) {
                b.AppendFormat("/Fa\"{0}\" ", p.AsmOutputPath);
            }
            switch (p.ClrSupport)
            {
                case MsvcClrSupport.Clr:
                    b.Append("/clr ");
                    break;
                case MsvcClrSupport.ClrPure:
                    b.Append("/clr:pure ");
                    break;
                case MsvcClrSupport.ClrSafe:
                    b.Append("/clr:safe ");
                    break;
                case MsvcClrSupport.ClrOldSyntax:
                    b.Append("/clr:oldsyntax ");
                    break;
            }

            //-- Optimization
            switch (p.OptLevel) {
                case MsvcOptLevel.Disabled:
                    b.Append("/Od ");
                    break;
                case MsvcOptLevel.MinimizeSpace:
                    b.Append("/O1 ");
                    break;
                case MsvcOptLevel.MaximizeSpeed:
                    b.Append("/O2 ");
                    break;
                case MsvcOptLevel.MaximumOptimizations:
                    b.Append("/Ox ");
                    break;
                case MsvcOptLevel.GlobalOptimizations:
                    b.Append("/Og ");
                    break;
            }
            switch (p.InlineExpansion) {
                case MsvcInlineExpansion.Disabled:
                    b.Append("/Ob0 ");
                    break;
                case MsvcInlineExpansion.OnlyExplicit:
                    b.Append("/Ob1 ");
                    break;
                case MsvcInlineExpansion.AutoInlining:
                    b.Append("/Ob2 ");
                    break;
            }
            if (p.EnableIntrinsicFunctions) {
                b.Append("/Oi ");
            }
            switch (p.FavorSizeOrSpeed) {
                case MsvcSizeOrSpeed.Size:
                    b.Append("/Os ");
                    break;
                case MsvcSizeOrSpeed.Speed:
                    b.Append("/Ot ");
                    break;
            }
            if (p.OmitFramePointers) {
                b.Append("/Oy ");
            }

            //-- Code Generation
            if (p.EnableMinimalRebuild) {
                b.Append("/Gm ");
            }
            if (p.EnableStringPooling) {
                b.Append("/GF ");
            }
            if (p.CppExceptions == MsvcCppExceptions.Enabled) {
                if (p.ExternCNoThrow) {
                    b.Append("/EHsc ");
                }
                else {
                    b.Append("/EHs ");
                }
            }
            else if (p.CppExceptions == MsvcCppExceptions.EnabledWithSeh) {
                b.Append("/EHa ");
            }
            switch (p.BasicRuntimeChecks) {
                case MsvcRuntimeChecks.StackFrames:
                    b.Append("/RTCs ");
                    break;
                case MsvcRuntimeChecks.UninitializedVariables:
                    b.Append("/RTCu ");
                    break;
                case MsvcRuntimeChecks.StackFramesAndUninitializedVariables:
                    b.Append("/RTCsu ");
                    break;
            }
            switch (p.RuntimeLibrary) {
                case MsvcRuntimeLibrary.MultiThreaded:
                    b.Append("/MT ");
                    break;
                case MsvcRuntimeLibrary.MultiThreadedDebug:
                    b.Append("/MTd ");
                    break;
                case MsvcRuntimeLibrary.MultiThreadedDll:
                    b.Append("/MD ");
                    break;
                case MsvcRuntimeLibrary.MultiThreadedDebugDll:
                    b.Append("/MDd ");
                    break;
            }
            {
                int[] validStructMemberAlignment = { 1, 2, 4, 8, 16 };
                int structMemberAlignment = 8;
                if (Array.IndexOf(validStructMemberAlignment, p.StructMemberAlignment) != -1) {
                    structMemberAlignment = p.StructMemberAlignment;
                }
                b.AppendFormat("/Zp{0} ", structMemberAlignment);
            }
            if (p.BufferSecurityCheck) {
                b.Append("/GS ");
            }
            else {
                b.Append("/GS- ");
            }
            if (p.EnableFunctionLevelLinking) {
                b.Append("/Gy ");
            }
            switch (p.EnhancedIsa) {
                case MsvcEnhancedIsa.SSE:
                    b.Append("/arch:SSE ");
                    break;
                case MsvcEnhancedIsa.SSE2:
                    b.Append("/arch:SSE2 ");
                    break;
            }
            switch (p.FloatingPointModel) {
                case MsvcFloatingPointModel.Precise:
                    b.Append("/fp:precise ");
                    break;
                case MsvcFloatingPointModel.Strict:
                    b.Append("/fp:strict ");
                    break;
                case MsvcFloatingPointModel.Fast:
                    b.Append("/fp:fast ");
                    break;
            }
            if (p.EnableFloatingPointExceptions) {
                b.Append("/fp:except ");
            }
            if (p.HotPatchable) {
                b.AppendFormat("/hotpatch ");
            }

            //-- PreProcessor
            foreach (string define in p.Defines) {
                b.AppendFormat("/D{0} ", define);
            }
            foreach (string undefine in p.Undefines) {
                b.AppendFormat("/U{0} ", undefine);
            }
            if (p.UndefineAllPredefinedMacros) {
                b.Append("/u ");
            }
            foreach (string includeDir in p.IncludeDirs) {
                b.AppendFormat("/I\"{0}\" ", includeDir);
            }
            foreach (string assemblySearchDir in p.AssemblySearchDirs) {
                b.AppendFormat("/AI\"{0}\" ", assemblySearchDir);
            }
            foreach (string forcedInclude in p.ForcedIncludes) {
                b.AppendFormat("/FI\"{0}\" ", forcedInclude);
            }
            foreach (string forcedUsing in p.ForcedUsings) {
                b.AppendFormat("/FU\"{0}\" ", forcedUsing);
            }
            if (p.IgnoreStandardPaths) {
                b.Append("/X ");
            }

            //-- Language
            switch (p.DebugInfoFormat) {
                case MsvcDebugInfoFormat.OldStyleC7:
                    b.Append("/Z7 ");
                    break;
                case MsvcDebugInfoFormat.Normal:
                    b.Append("/Zi ");
                    break;
                case MsvcDebugInfoFormat.EditAndContinue:
                    b.Append("/ZI ");
                    break;
            }
            if (!p.EnableExtensions) {
                b.Append("/Za ");
            }
            if (p.DefaultCharUnsigned) {
                b.Append("/J ");
            }
            if (p.Wchar_tBuiltIn) {
                b.Append("/Zc:wchar_t ");
            }
            if (!p.ConformantForLoopScope) {
                b.Append("/Zc:forScope- ");
            }
            if (p.EnableRtti) {
                b.Append("/GR ");
            }
            if (p.EnableOpenMPSupport) {
                b.Append("/openmp ");
            }
            if (p.CompileAsC) {
                b.Append("/TC ");
            }
            if (p.CompileAsCpp) {
                b.Append("/TP ");
            }

            //-- Warnings
            if (p.DisableAllWarnings) {
                b.Append("/w ");
            }
            if (p.EnableAllWarnings) {
                b.Append("/Wall ");
            }
            if (1 <= p.WarnLevel && p.WarnLevel <= 4) {
                b.AppendFormat("/W{0} ", p.WarnLevel);
            }
            if (p.TreatWarningsAsErrors) {
                b.Append("/WX ");
            }

            //-- PreCompiled Headers (PCH)
            if (p.CreatePch) {
                b.AppendFormat("/Yc\"{0}\" ", p.SourceFile);
                b.AppendFormat("/Fp\"{0}\" ", p.CreatePchFilePath);
            }
            if (!String.IsNullOrEmpty(p.UsePchFilePath)) {
                b.AppendFormat("/Yu\"{0}\" ", p.UsePchFilePath);
            }

            b.Append(p.ExtraArgs);

            return b.ToString();
        }

        public static string ToPreProcessorArgumentString(this MsvcCompileParams p, bool showIncludes)
        {
            StringBuilder b = new StringBuilder();
            b.AppendFormat("{0} ", p.SourceFile);
            b.Append("/nologo ");   // do not print logo to stdout
            b.Append("/FC ");       // show full path in diagnostic messages
            b.Append("/E ");        // preprocess to stdout
            if (showIncludes) {
                b.Append("/showIncludes ");
            }

            //-- PreProcessor
            foreach (string define in p.Defines) {
                b.AppendFormat("/D{0} ", define);
            }
            foreach (string undefine in p.Undefines) {
                b.AppendFormat("/U{0} ", undefine);
            }
            if (p.UndefineAllPredefinedMacros) {
                b.Append("/u ");
            }
            foreach (string includeDir in p.IncludeDirs) {
                b.AppendFormat("/I\"{0}\" ", includeDir);
            }
            foreach (string assemblySearchDir in p.AssemblySearchDirs) {
                b.AppendFormat("/AI\"{0}\" ", assemblySearchDir);
            }
            foreach (string forcedInclude in p.ForcedIncludes) {
                b.AppendFormat("/FI\"{0}\" ", forcedInclude);
            }
            foreach (string forcedUsing in p.ForcedUsings) {
                b.AppendFormat("/FU\"{0}\" ", forcedUsing);
            }
            if (p.IgnoreStandardPaths) {
                b.Append("/X ");
            }

            return b.ToString();
        }
    }
}
