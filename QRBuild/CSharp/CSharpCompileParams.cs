using System;
using System.Collections.Generic;
using QRBuild.Linq;

namespace QRBuild.CSharp
{
    /// Values that can be passed to /target.
    public static class CSharpTargetFormats
    {
        public static string ConsoleExe = "exe";
        public static string WindowsExe = "winexe";
        public static string Library = "library";
        public static string Module = "module";
    }

    public static class CSharpPlatforms
    {
        public static string X86 = "x86";
        public static string Itanium = "Itanium";
        public static string Amd64 = "x64";
        public static string AnyCpu = "anycpu";
    }

    public static class CSharpLanguageVersions
    {
        public static string Iso1 = "ISO-1";
        public static string Iso2 = "ISO-2";
    }

    public class CSharpCompileParams
    {
        public CSharpCompileParams()
        {
        }

        public CSharpCompileParams(CSharpCompileParams rhs)
        {
            DeepCopy(rhs);
        }

        public void DeepCopy(CSharpCompileParams rhs)
        {
            //-- Meta Options
            CscPath = rhs.CscPath;
            CompileDir = rhs.CompileDir;
            ExtraArgs = rhs.ExtraArgs;
            ExtraInputs.AddRange(rhs.ExtraInputs);
            ExtraOutputs.AddRange(rhs.ExtraOutputs);
            //-- Input Options
            Sources.AddRange(rhs.Sources);
            AssemblyReferences.AddRange(rhs.AssemblyReferences);
            InputModules.AddRange(rhs.InputModules);
            //-- Output Options
            OutputFilePath = rhs.OutputFilePath;
            TargetFormat = rhs.TargetFormat;
            Platform = rhs.Platform;
            //-- Code Generation
            Debug = rhs.Debug;
            Optimize = rhs.Optimize;
            //-- Errors and Warnings
            WarnAsError = rhs.WarnAsError;
            WarnLevel = rhs.WarnLevel;
            //-- Language
            Checked = rhs.Checked;
            Unsafe = rhs.Unsafe;
            Defines.AddRange(rhs.Defines);
            LanguageVersion = rhs.LanguageVersion;
            //-- Miscellaneous
            NoConfig = rhs.NoConfig;
            //-- Advanced
            MainType = rhs.MainType;
            FullPaths = rhs.FullPaths;
            PdbFilePath = rhs.PdbFilePath;
            ModuleAssemblyName = rhs.ModuleAssemblyName;
        }

        //-- Meta Options
        /// Path to the C# compiler executable (csc.exe on Windows).
        public string CscPath;
        public string CompileDir;
        
        public string ExtraArgs;
        public HashSet<string> ExtraInputs = new HashSet<string>();
        public HashSet<string> ExtraOutputs = new HashSet<string>();

        //-- Input Options
        public readonly HashSet<string> Sources = new HashSet<string>();
        public readonly HashSet<string> AssemblyReferences = new HashSet<string>();
        public readonly HashSet<string> InputModules = new HashSet<string>();

        //-- Output Options
        public string OutputFilePath;
        public string TargetFormat;
        public string Platform;

        //-- Code Generation
        public bool Debug;
        public bool Optimize;

        //-- Errors and Warnings
        public bool WarnAsError;
        public int WarnLevel;
        
        //-- Language
        public bool Checked;
        public bool Unsafe;
        public readonly List<string> Defines = new List<string>();
        public string LanguageVersion;

        //-- Miscellaneous
        public bool NoConfig = true;

        //-- Advanced
        public string MainType;
        public bool FullPaths;
        public string PdbFilePath;
        public string ModuleAssemblyName;
    }
}
