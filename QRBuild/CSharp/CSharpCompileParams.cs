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

        //-- Meta Options
        public string CompileDir;
        public string FrameworkVersion;
        public string ExtraArgs;
        
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
