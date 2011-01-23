using System;
using System.Collections.Generic;
using QRBuild.Linq;

namespace QRBuild.Translations.ToolChain.MsCsc
{
    /// Values that can be passed to /target.
    public static class CSharpTargetFormats
    {
        public static readonly string ConsoleExe = "exe";
        public static readonly string WindowsExe = "winexe";
        public static readonly string Library = "library";
        public static readonly string Module = "module";
    }

    public static class CSharpPlatforms
    {
        public static readonly string X86 = "x86";
        public static readonly string Itanium = "Itanium";
        public static readonly string Amd64 = "x64";
        public static readonly string AnyCpu = "anycpu";
    }

    public static class CSharpLanguageVersions
    {
        public static readonly string Iso1 = "ISO-1";
        public static readonly string Iso2 = "ISO-2";
    }

    public static class CSharpFrameworkVersion
    {
        public static readonly string V2_0_50727 = "v2.0.50727";
        public static readonly string V3_0       = "v3.0";
        public static readonly string V3_5       = "v3.5";
        public static readonly string V4_0_30319 = "v4.0.30319";
    }

    public class CSharpCompileParams
    {
        public CSharpCompileParams()
        {
        }

        //-- Meta Options
        public string CompileDir;
        public string BuildFileDir;
        public string FrameworkVersion;
        public string ExtraArgs;

        //-- Input Options
        public readonly HashSet<string> Sources = new HashSet<string>();
        /// /r:
        public readonly HashSet<string> AssemblyReferences = new HashSet<string>();
        /// /addmodule:
        public readonly HashSet<string> InputModules = new HashSet<string>();

        //-- Output Options
        /// /out:
        public string OutputFilePath;
        /// /target:
        public string TargetFormat;
        /// /platform:
        public string Platform;

        //-- Code Generation
        /// /debug
        public bool Debug;
        /// /optimize
        public bool Optimize;

        //-- Errors and Warnings
        /// /warnaserror
        public bool WarnAsError;
        /// /warn:
        public int WarnLevel;
        
        //-- Language
        /// /checked
        public bool Checked;
        /// /unsafe
        public bool Unsafe;
        /// /define
        public readonly List<string> Defines = new List<string>();
        /// /langversion:
        public string LanguageVersion;

        //-- Miscellaneous
        public bool NoConfig = false;

        //-- Advanced
        /// /main:
        public string MainType;
        /// /fullpaths
        public bool FullPaths;
        /// /pdb:
        public string PdbFilePath;
        /// /moduleassemblyname:
        public string ModuleAssemblyName;
    }
}
