using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QRBuild.IO;

namespace QRBuild.Cpp
{
    public static class Msvc9CompileExtensions
    {
        private static string GetAbsPath(
            string filePath, string workingDir, 
            string filePathName, string workingDirName)
        {
            string fileName = Path.GetFileName(filePath);
            if (String.IsNullOrEmpty(fileName)) {
                throw new InvalidOperationException(
                    String.Format("{0} is not a file, value={1}", filePathName, filePath));
            }
            
            string unresolvedPath;
            if (Path.IsPathRooted(filePath)) {
                unresolvedPath = filePath;
            }
            else {
                if (!Path.IsPathRooted(workingDir)) {
                    throw new InvalidOperationException(
                        String.Format("{0} is not rooted, cannot compute absolute path for {1}", workingDirName, filePathName));
                }
                unresolvedPath = Path.Combine(workingDir, filePath);
            }
            string resolvedPath = Path.GetFullPath(unresolvedPath);
            return resolvedPath;
        }
        
        /// Returns a new canonicalized instance of compiler params.
        public static Msvc9CompileParams Canonicalize(this Msvc9CompileParams p)
        {
            if (p.SourceFile == null) {
                throw new InvalidOperationException("C/C++ SourceFile not specified");
            }

            //-- Input and Output Options
            Msvc9CompileParams o = new Msvc9CompileParams();
            o.SourceFile = GetAbsPath(p.SourceFile, p.CompileDir, "SourceFile", "CompileDir");
            o.CompileDir = p.CompileDir;
            if (p.Compile) {
                o.ObjectPath = QRPath.ComputeDefaultFilePath(p.ObjectPath, p.SourceFile, ".obj");
                o.PdbPath = QRPath.ComputeDefaultFilePath(p.PdbPath, p.SourceFile, ".pdb");
            }
            o.AsmOutputFormat = p.AsmOutputFormat;
            if (p.AsmOutputFormat != Msvc9AsmOutputFormat.None) {
                o.AsmOutputPath = QRPath.ComputeDefaultFilePath(p.AsmOutputPath, p.SourceFile, ".asm");
            }
            o.ClrSupport = p.ClrSupport;

            //-- Optimization
            o.OptLevel = p.OptLevel;
            o.InlineExpansion = p.InlineExpansion;
            o.EnableIntrinsicFunctions = p.EnableIntrinsicFunctions;
            o.FavorSizeOrSpeed = p.FavorSizeOrSpeed;
            o.OmitFramePointers = p.OmitFramePointers;

            //-- Code Generation
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
            o.IncludeDirs.AddRange(p.IncludeDirs);
            o.AssemblySearchDirs.AddRange(p.AssemblySearchDirs);
            o.ForcedIncludes.AddRange(p.ForcedIncludes);
            o.ForcedUsings.AddRange(p.ForcedUsings);
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
                o.CreatePchFilePath = QRPath.ComputeDefaultFilePath(p.CreatePchFilePath, p.SourceFile, ".pch");
            }
            o.UsePchFilePath = p.UsePchFilePath;

            return o;
        }
        
        /// This function assumes p is canonicalized.
        public static string ToArgumentString(this Msvc9CompileParams p)
        {
            StringBuilder b = new StringBuilder();
            if (p.SourceFile == null) {
                throw new InvalidOperationException("C/C++ SourceFile not specified");
            }
            b.AppendFormat("{0} ", p.SourceFile);
            b.Append("/nologo ");

            //-- Input and Output Options
            if (!String.IsNullOrEmpty(p.ObjectPath)) {
                b.AppendFormat("/c /Fo{0} ", p.ObjectPath);
            }
            if (!String.IsNullOrEmpty(p.PdbPath)) {
                b.AppendFormat("/Fd{0} ", p.PdbPath);
            }
            switch (p.AsmOutputFormat)
            {
                case Msvc9AsmOutputFormat.AsmOnly: 
                    b.Append("/FA ");
                    break;
                case Msvc9AsmOutputFormat.WithMachineCode:
                    b.Append("/FAc ");
                    break;
                case Msvc9AsmOutputFormat.WithSourceCode:
                    b.AppendFormat("/FAs ");
                    break;
                case Msvc9AsmOutputFormat.WithMachineAndSourceCode:
                    b.AppendFormat("/FAsc ");
                    break;
            }                
            if (!String.IsNullOrEmpty(p.AsmOutputPath)) {
                b.AppendFormat("/Fa{0} ", p.AsmOutputPath);
            }
            switch (p.ClrSupport)
            {
                case Msvc9ClrSupport.Clr:
                    b.Append("/clr ");
                    break;
                case Msvc9ClrSupport.ClrPure:
                    b.Append("/clr:pure ");
                    break;
                case Msvc9ClrSupport.ClrSafe:
                    b.Append("/clr:safe ");
                    break;
                case Msvc9ClrSupport.ClrOldSyntax:
                    b.Append("/clr:oldsyntax ");
                    break;
            }

            //-- Optimization
            switch (p.OptLevel) {
                case Msvc9OptLevel.Disabled:
                    b.Append("/Od ");
                    break;
                case Msvc9OptLevel.MinimizeSpace:
                    b.Append("/O1 ");
                    break;
                case Msvc9OptLevel.MaximizeSpeed:
                    b.Append("/O2 ");
                    break;
                case Msvc9OptLevel.MaximumOptimizations:
                    b.Append("/Ox ");
                    break;
                case Msvc9OptLevel.GlobalOptimizations:
                    b.Append("/Og ");
                    break;
            }
            switch (p.InlineExpansion) {
                case Msvc9InlineExpansion.Disabled:
                    b.Append("/Ob0 ");
                    break;
                case Msvc9InlineExpansion.OnlyExplicit:
                    b.Append("/Ob1 ");
                    break;
                case Msvc9InlineExpansion.AutoInlining:
                    b.Append("/Ob2 ");
                    break;
            }
            if (p.EnableIntrinsicFunctions) {
                b.Append("/Oi ");
            }
            switch (p.FavorSizeOrSpeed) {
                case Msvc9SizeOrSpeed.Size:
                    b.Append("/Os ");
                    break;
                case Msvc9SizeOrSpeed.Speed:
                    b.Append("/Ot ");
                    break;
            }
            if (p.OmitFramePointers) {
                b.Append("/Oy ");
            }

            //-- Code Generation
            if (p.EnableStringPooling) {
                b.Append("/GF ");
            }
            if (p.CppExceptions == Msvc9CppExceptions.Enabled) {
                if (p.ExternCNoThrow) {
                    b.Append("/EHsc ");
                }
                else {
                    b.Append("/EHs ");
                }
            }
            else if (p.CppExceptions == Msvc9CppExceptions.EnabledWithSeh) {
                b.Append("/EHa ");
            }
            switch (p.BasicRuntimeChecks) {
                case Msvc9RuntimeChecks.StackFrames:
                    b.Append("/RTCs ");
                    break;
                case Msvc9RuntimeChecks.UninitializedVariables:
                    b.Append("/RTCu ");
                    break;
                case Msvc9RuntimeChecks.StackFramesAndUninitializedVariables:
                    b.Append("/RTCsu ");
                    break;
            }
            switch (p.RuntimeLibrary) {
                case Msvc9RuntimeLibrary.MultiThreaded:
                    b.Append("/MT ");
                    break;
                case Msvc9RuntimeLibrary.MultiThreadedDebug:
                    b.Append("/MTd ");
                    break;
                case Msvc9RuntimeLibrary.MultiThreadedDll:
                    b.Append("/MD ");
                    break;
                case Msvc9RuntimeLibrary.MultiThreadedDebugDll:
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
                case Msvc9EnhancedIsa.SSE:
                    b.Append("/arch:SSE ");
                    break;
                case Msvc9EnhancedIsa.SSE2:
                    b.Append("/arch:SSE2 ");
                    break;
            }
            switch (p.FloatingPointModel) {
                case Msvc9FloatingPointModel.Precise:
                    b.Append("/fp:precise ");
                    break;
                case Msvc9FloatingPointModel.Strict:
                    b.Append("/fp:strict ");
                    break;
                case Msvc9FloatingPointModel.Fast:
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
                case Msvc9DebugInfoFormat.OldStyleC7:
                    b.Append("/Z7 ");
                    break;
                case Msvc9DebugInfoFormat.Normal:
                    b.Append("/Zi ");
                    break;
                case Msvc9DebugInfoFormat.EditAndContinue:
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
                b.AppendFormat("/Yc{0} ", p.SourceFile);

                if (String.IsNullOrEmpty(p.CreatePchFilePath)) {
                    string createPchFilePath = QRPath.ComputeDefaultFilePath(p.CreatePchFilePath, p.SourceFile, ".pch");
                    b.AppendFormat("/Fp{0} ", createPchFilePath);
                }
                else {
                    string createPchFilePath = QRPath.ComputeDefaultFilePath(p.CreatePchFilePath, p.SourceFile, ".pch");
                    b.AppendFormat("/Fp{0} ", createPchFilePath);
                }
            }
            if (!String.IsNullOrEmpty(p.UsePchFilePath)) {
                string usePchFilePath = QRPath.ComputeDefaultFilePath(p.UsePchFilePath, p.SourceFile, ".pch");
                b.AppendFormat("/Yu{0} ", usePchFilePath);
            }

            return b.ToString();
        }
    }
}
