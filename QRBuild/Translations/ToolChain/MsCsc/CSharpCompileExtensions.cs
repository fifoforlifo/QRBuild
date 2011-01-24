using System;
using System.Text;

using QRBuild.IO;

namespace QRBuild.Translations.ToolChain.MsCsc
{
    public static class CSharpCompileExtensions
    {
        public static CSharpCompileParams Canonicalize(this CSharpCompileParams p)
        {
            CSharpCompileParams o = new CSharpCompileParams();
            //-- Meta Options
            if (String.IsNullOrEmpty(p.CompileDir)) {
                throw new InvalidOperationException("C# CompileDir not specified");
            }
            o.CompileDir = QRPath.GetCanonical(p.CompileDir);
            o.BuildFileDir = String.IsNullOrEmpty(p.BuildFileDir)
                ? p.CompileDir
                : QRPath.GetCanonical(p.BuildFileDir);
            if (String.IsNullOrEmpty(p.FrameworkVersion)) {
                throw new InvalidOperationException("C# FrameworkVersion not specified");
            }
            o.FrameworkVersion = p.FrameworkVersion;
            o.ExtraArgs = p.ExtraArgs;
            //-- Input Options
            if (p.Sources.Count == 0) {
                throw new InvalidOperationException("C# Sources not specified");
            }
            foreach (string path in p.Sources) {
                string absPath = QRPath.GetAbsolutePath(path, p.CompileDir);
                o.Sources.Add(absPath);
            }
            foreach (string path in p.AssemblyReferences) {
                string absPath = QRPath.GetAbsolutePath(path, p.CompileDir);
                o.AssemblyReferences.Add(absPath);
            }
            foreach (string path in p.InputModules) {
                string absPath = QRPath.GetAbsolutePath(path, p.CompileDir);
                o.InputModules.Add(absPath);
            }
            //-- Output Options
            if (String.IsNullOrEmpty(p.OutputFilePath)) {
                throw new InvalidOperationException("C# compile OutputFilePath not specified");
            }
            o.OutputFilePath = QRPath.GetAbsolutePath(p.OutputFilePath, p.CompileDir);
            o.TargetFormat = p.TargetFormat;
            o.Platform = p.Platform;
            //-- Code Generation
            o.Debug = p.Debug;
            o.Optimize = p.Optimize;
            //-- Errors and Warnings
            o.WarnAsError = p.WarnAsError;
            o.WarnLevel = p.WarnLevel;
            //-- Language
            o.Checked = p.Checked;
            o.Unsafe = p.Unsafe;
            o.Defines.AddRange(p.Defines);
            o.LanguageVersion = p.LanguageVersion;
            //-- Miscellaneous
            o.NoConfig = p.NoConfig;
            //-- Advanced
            o.MainType = p.MainType;
            o.FullPaths = p.FullPaths;
            if (p.Debug) {
                o.PdbFilePath = QRPath.ComputeDefaultFilePath(p.PdbFilePath, o.OutputFilePath, ".pdb", o.CompileDir);
            }
            o.ModuleAssemblyName = p.ModuleAssemblyName;
            return o;
        }
        
        public static string ToArgumentString(this CSharpCompileParams p)
        {
            StringBuilder b = new StringBuilder();
            b.AppendFormat("/nologo ");
            //-- Input Options
            foreach (string reference in p.AssemblyReferences) {
                b.AppendFormat("/r:\"{0}\" ", reference);
            }
            foreach (string module in p.InputModules) {
                b.AppendFormat("/addmodule:\"{0}\" ", module);
            }
            //-- Output Options
            if (!String.IsNullOrEmpty(p.OutputFilePath)) {
                b.AppendFormat("/out:\"{0}\" ", p.OutputFilePath);
            }
            if (!String.IsNullOrEmpty(p.TargetFormat)) {
                b.AppendFormat("/target:{0} ", p.TargetFormat);
            }
            //-- Code Generation
            if (p.Debug) {
                b.AppendFormat("/debug ");
            }
            if (p.Optimize) {
                b.AppendFormat("/optimize ");
            }
            //-- Errors and Warnings
            if (p.WarnAsError) {
                b.AppendFormat("/warnaserror ");
            }
            b.AppendFormat("/warn:{0} ", p.WarnLevel);
            //-- Language
            if (p.Checked) {
                b.AppendFormat("/checked ");
            }
            if (p.Unsafe) {
                b.AppendFormat("/unsafe ");
            }
            foreach (string define in p.Defines) {
                b.AppendFormat("/define:{0} ", define);
            }
            if (!String.IsNullOrEmpty(p.LanguageVersion)) {
                b.AppendFormat("/langversion:{0} ", p.LanguageVersion);
            }
            //-- Miscellaneous
            // NOTE: p.NoConfig may not be supplied in a response file
            //-- Advanced
            if (!String.IsNullOrEmpty(p.MainType)) {
                b.AppendFormat("/main:{0} ", p.MainType);
            }
            if (p.FullPaths) {
                b.AppendFormat("/fullpaths ");
            }
            if (!String.IsNullOrEmpty(p.PdbFilePath)) {
                b.AppendFormat("/pdb:\"{0}\" ", p.PdbFilePath);
            }
            if (!String.IsNullOrEmpty(p.ModuleAssemblyName)) {
                b.AppendFormat("/moduleassemblyname:{0} ", p.ModuleAssemblyName);
            }

            //  ExtraArgs goes at the end.
            if (!String.IsNullOrEmpty(p.ExtraArgs)) {
                b.AppendFormat("{0} ", p.ExtraArgs);
            }

            //  Source files must go at the very end.
            foreach (string sourceFile in p.Sources) {
                b.AppendFormat("\"{0}\" ", sourceFile);
            }

            return b.ToString();
        }
    }
}
