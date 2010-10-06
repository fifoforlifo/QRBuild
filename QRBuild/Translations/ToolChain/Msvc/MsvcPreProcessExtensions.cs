using System;
using System.Collections.Generic;
using System.Text;
using QRBuild.IO;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public static class MsvcPreProcessExtensions
    {
        /// Returns a new canonicalized instance of compiler params.
        public static MsvcPreProcessParams Canonicalize(this MsvcPreProcessParams p)
        {
            MsvcPreProcessParams o = new MsvcPreProcessParams();
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

            //-- Input and Output Options
            if (String.IsNullOrEmpty(p.SourceFile)) {
                throw new InvalidOperationException("C/C++ SourceFile not specified");
            }
            o.SourceFile = QRPath.GetAbsolutePath(p.SourceFile, o.CompileDir);
            o.OutputPath = QRPath.ComputeDefaultFilePath(p.OutputPath, p.SourceFile, ".i", o.CompileDir);
            o.ExtraArgs = p.ExtraArgs;

            //-- PreProcessor
            o.Defines.AddRange(p.Defines);
            o.Undefines.AddRange(p.Undefines);
            o.UndefineAllPredefinedMacros = p.UndefineAllPredefinedMacros;
            o.IncludeDirs.AddRangeAsAbsolutePaths(p.IncludeDirs, o.CompileDir);
            o.AssemblySearchDirs.AddRangeAsAbsolutePaths(p.AssemblySearchDirs, o.CompileDir);
            o.ForcedIncludes.AddRangeAsAbsolutePaths(p.ForcedIncludes, o.CompileDir);
            o.ForcedUsings.AddRangeAsAbsolutePaths(p.ForcedUsings, o.CompileDir);
            o.IgnoreStandardPaths = p.IgnoreStandardPaths;

            //-- Warnings
            o.DisableAllWarnings = p.DisableAllWarnings;
            o.EnableAllWarnings = p.EnableAllWarnings;
            o.WarnLevel = p.WarnLevel;
            o.TreatWarningsAsErrors = p.TreatWarningsAsErrors;

            return o;
        }

        public static string ToArgumentString(this MsvcPreProcessParams p, bool showIncludes)
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
