using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// Path to the C# compiler executable (csc.exe on Windows).
        public string CscPath;
        public string SourceRoot;

        public string ExtraArgs;

        //-- Input Options
        public readonly IList<string> Sources = new List<string>();
        public readonly IList<string> References = new List<string>();
        public readonly IList<string> InputModules = new List<string>();

        //-- Output Options
        public string Output;
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
        public readonly IList<string> Defines = new List<string>();
        public string LanguageVersion;

        //-- Advanced
        public string MainType;
        public bool FullPaths;
        public string PdbFilePath;
        public string ModuleAssemblyName;
    }
}
