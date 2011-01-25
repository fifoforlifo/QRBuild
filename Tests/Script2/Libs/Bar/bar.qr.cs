//@ outdir "built";
//@ include "built\env.qh.cs";
//@ using assembly "QRBuild";
//@ using project  Common;

using System.IO;
using QRBuild.ProjectSystem;
using QRBuild.Translations.ToolChain.Msvc;

namespace Build
{
    [PrimaryProject(typeof(MainVariant))]
    public class Bar : CppProject
    {
        public override string ModuleName
        {
            get { return "Lib.Bar"; }
        }

        protected override void AddToGraph()
        {
            Compile(@"src\bar0.cpp");
            Compile(@"src\bar1.cpp");
            Compile(@"src\bar2.cpp");
            Compile(@"src\bar3.cpp");
            Compile(@"src\bar4.cpp");
            Compile(@"src\bar5.cpp");
            Compile(@"src\bar6.cpp");
            Compile(@"src\bar7.cpp", ccp =>
                {
                    ccp.Defines.Add("ENABLE_HACKS=1");
                    ccp.WarnLevel = 4;
                });

            var lib = Lib(@"bar.lib");

            DefaultTarget.Targets.Add(lib.Params.OutputFilePath);
        }

        public override Target DefaultTarget
        {
            get
            {
                Target target = ProjectManager.GetOrCreateTarget(DefaultTargetName);
                return target;
            }
        }

        protected override void CompileOverride(MsvcCompileParams ccp)
        {
            ccp.IncludeDirs.Add(
                Path.Combine(Config.PlatformSdkDir, "Include")
            );
            ccp.IncludeDirs.Add(Path.Combine(ProjectDir, "inc"));
        }

        private static readonly string DefaultTargetName = "bar";
    }
}
