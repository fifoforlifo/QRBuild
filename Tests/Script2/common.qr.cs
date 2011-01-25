//@ outdir "built";
//@ using assembly "QRBuild";

using System;
using System.Collections.Generic;
using System.IO;
using QRBuild.IO;
using QRBuild.ProjectSystem;
using QRBuild.Translations.ToolChain.Msvc;

namespace Build
{
    public sealed class Locations : ProjectLocations
    {
        static Locations()
        {
            dir = QRPath.GetAssemblyDirectory(typeof(Locations), 1);
            S = new Locations();
        }

        public static readonly Locations S;
        private static readonly string dir;

        public readonly string Common   = dir + @"\common.qr.cs";
        public readonly string Bar      = dir + @"\Libs\Bar\bar.qr.cs";
        public readonly string Blah     = dir + @"\Libs\Blah\blah.qr.cs";
    }

    public static class Config
    {
        public static readonly string VcBinDir =
            @"C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\bin";

        public static readonly string PlatformSdkDir =
            @"C:\Program Files\Microsoft SDKs\Windows\v6.0A";
    }

    public enum Architecture
    {
        x86,
        amd64,
    }
    public enum Configuration
    {
        debug,
        release,
    }

    public class MainVariant : BuildVariant
    {
        [VariantPart(100)]
        public Configuration Configuration;

        [VariantPart(200)]
        public Architecture Architecture;
    }

    public abstract class CppProject : Project
    {
        protected MainVariant MainVariant
        {
            get { return Variant as MainVariant; }
        }

        public virtual string ProjectDir
        {
            get
            {
                if (m_projectDir == null) {
                    // Project DLL is always in a "built" sub-directory, so
                    // the ProjectDir is 1 up from the AssemblyDirectory.
                    m_projectDir = QRPath.GetAssemblyDirectory(GetType(), 1);
                }
                return m_projectDir;
            }
        }

        public string OutDir
        {
            get
            {
                if (m_outDir == null) {
                    m_outDir = Path.Combine(
                        ProjectDir,
                        Path.Combine("built", FullVariantString)
                    );
                }
                return m_outDir;
            }
        }

        public MsvcToolChain ToolChain
        {
            get
            {
                if (m_msvcToolChain == null) {
                    if (MainVariant.Architecture == Architecture.x86) {
                        m_msvcToolChain = MsvcToolChain.ToolsX86TargetX86;
                    }
                    else if (MainVariant.Architecture == Architecture.amd64) {
                        if (QRSystem.Is64BitOperatingSystem) {
                            m_msvcToolChain = MsvcToolChain.ToolsAmd64TargetAmd64;
                        }
                        else {
                            m_msvcToolChain = MsvcToolChain.ToolsX86TargetAmd64;
                        }
                    }
                    else {
                        throw new InvalidOperationException(
                            String.Format("Unknown Architecture={0}.", MainVariant.Architecture)
                        );
                    }
                }
                return m_msvcToolChain.Value;
            }
        }

        protected virtual void CompileOverride(MsvcCompileParams ccp)
        {
        }

        protected MsvcCompileParams CreateCompileParams(
            string sourceFile,
            Action<MsvcCompileParams> overrideParams)
        {
            string srcDir = Path.GetDirectoryName(sourceFile);
            string objDir = Path.Combine(OutDir, srcDir);

            var ccp = new MsvcCompileParams();
            ccp.VcBinDir = Config.VcBinDir;
            ccp.CompileDir = Path.Combine(ProjectDir, srcDir);
            ccp.BuildFileDir = objDir;
            ccp.SourceFile = Path.GetFileName(sourceFile);
            ccp.ObjectPath = Path.Combine(
                objDir,
                ccp.SourceFile + ".obj");
            ccp.Compile = true;
            ccp.ToolChain = ToolChain;

            ccp.CppExceptions = MsvcCppExceptions.Enabled;
            ccp.DebugInfoFormat = MsvcDebugInfoFormat.Normal;
            ccp.EnableMinimalRebuild = true;

            if (MainVariant.Configuration == Configuration.debug) {
                ccp.OptLevel = MsvcOptLevel.Disabled;
            }
            else {
                ccp.OptLevel = MsvcOptLevel.GlobalOptimizations;
            }

            // Allow derived class to override settings in bulk.
            CompileOverride(ccp);

            // Allow caller to override settings for just this file.
            if (overrideParams != null) {
                overrideParams(ccp);
            }

            return ccp;
        }

        /// Derived project classes can override this to set project-level
        /// defaults on compiler params.
        protected MsvcCompile Compile(string sourceFile)
        {
            return Compile(sourceFile, null);
        }

        protected MsvcCompile Compile(
            string sourceFile,
            Action<MsvcCompileParams> overrideParams)
        {
            var ccp = CreateCompileParams(sourceFile, overrideParams);
            var cc = new MsvcCompile(ProjectManager.BuildGraph, ccp);
            cc.ModuleName = ModuleName + ".Compile";
            m_compiles.Add(cc);
            return cc;
        }

        /// Derived project classes can override this to set project-level
        /// defaults on linker params.
        protected virtual void LinkerOverride(MsvcLinkerParams lp)
        {
        }

        protected MsvcLinkerParams CreateLinkerParams(
            string outputName,
            Action<MsvcLinkerParams> overrideParams)
        {
            var lp = new MsvcLinkerParams();
            lp.VcBinDir = Config.VcBinDir;
            lp.ToolChain = ToolChain;
            lp.CompileDir = ProjectDir;
            lp.BuildFileDir = OutDir;
            lp.Inputs.Add(Config.PlatformSdkDir + @"\lib\kernel32.lib");
            lp.Inputs.Add(Config.PlatformSdkDir + @"\lib\user32.lib");
            foreach (MsvcCompile cc in m_compiles) {
                lp.Inputs.Add(cc.Params.ObjectPath);
            }
            lp.OutputFilePath = Path.Combine(OutDir, outputName);
            lp.Incremental = true;

            LinkerOverride(lp);

            if (overrideParams != null) {
                overrideParams(lp);
            }

            return lp;
        }

        protected MsvcLink Link(string outputName)
        {
            return Link(outputName, null);
        }

        protected virtual MsvcLink Link(
            string outputName,
            Action<MsvcLinkerParams> overrideParams)
        {
            var lp = CreateLinkerParams(outputName, overrideParams);
            var link = new MsvcLink(ProjectManager.BuildGraph, lp);
            link.ModuleName = ModuleName + ".Link";
            return link;
        }

        /// Derived project classes can override this to set project-level
        /// defaults on lib params.
        protected virtual void LibOverride(MsvcLibParams lp)
        {
        }

        protected MsvcLibParams CreateLibParams(
            string outputName,
            Action<MsvcLibParams> overrideParams)
        {
            var lp = new MsvcLibParams();
            lp.VcBinDir = Config.VcBinDir;
            lp.ToolChain = ToolChain;
            lp.CompileDir = ProjectDir;
            lp.BuildFileDir = OutDir;
            foreach (MsvcCompile cc in m_compiles) {
                lp.Inputs.Add(cc.Params.ObjectPath);
            }
            lp.OutputFilePath = Path.Combine(OutDir, outputName);

            LibOverride(lp);

            if (overrideParams != null) {
                overrideParams(lp);
            }

            return lp;
        }

        protected MsvcLib Lib(string outputName)
        {
            return Lib(outputName, null);
        }

        protected virtual MsvcLib Lib(
            string outputName,
            Action<MsvcLibParams> overrideParams)
        {
            var lp = CreateLibParams(outputName, overrideParams);
            var lib = new MsvcLib(ProjectManager.BuildGraph, lp);
            lib.ModuleName = ModuleName + ".Lib";
            return lib;
        }

        private string m_projectDir;
        private string m_outDir;
        private MsvcToolChain? m_msvcToolChain;
        private List<MsvcCompile> m_compiles = new List<MsvcCompile>();
    }
}
