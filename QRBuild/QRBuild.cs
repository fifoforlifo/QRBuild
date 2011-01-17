using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using QRBuild.ProjectSystem;
using QRBuild.ProjectSystem.CommandLine;

namespace QRBuild
{
    public static class QRBuild
    {
        class DummyVariant : BuildVariant
        {

        }

        public static int Main(string[] args)
        {
            var clHandlers = GetCLHandlers();

            //PrintUsage(clHandlers);
            ProjectManager projectManager = new ProjectManager();

            bool bootstrap = false;
            HashSet<Project> projects;
            if (bootstrap) {
                string variantString = "";
                Assembly assembly = projectManager.LoadProjectFile("bootstrap.qr", variantString);
                projects = projectManager.AddAllProjectsInAssembly(assembly, variantString);
            }
            else {
                string variantString = "Debug.x86";
                Assembly assembly = projectManager.LoadProjectFile("build.qr", variantString);
                projects = projectManager.AddAllProjectsInAssembly(assembly, variantString);
            }

            BuildOptions options = new BuildOptions();
            options.FileDecider = new FileSizeDateDecider();
            var targets = projects.Select(project => project.DefaultTarget.Name).ToList();
            var targetFiles = projectManager.GetTargetFiles(targets);
            BuildResults results = projectManager.BuildGraph.Execute(
                BuildAction.Build,
                options,
                targetFiles,
                true);

            if (!results.Success) {
                throw new InvalidOperationException();
            }

            return 0;
        }

        private static void PrintUsage(IList<CLHandler> clHandlers)
        {
#if false
            string usage = 
"usage: qrbuild COMMAND [ARGS]\n" +
"\n" +
"Built-in commands:\n" +
"  build        Incremental build.\n" +
"  clean        Delete all built files, do not remove directories.\n" +
"  clobber      Delete all built files, remove empty directories.\n" +
"  rebuild      Clean, then build.\n" +
"  version      Print version of QRBuild.\n" +
"Project's commands:\n" +
"  A1           Do thing #1.\n" +
"  A2           Do thing #2.\n" +
"\n" +
"See 'qrbuild help COMMAND' for more information on a specific command.\n" +
"";
            Console.Write(usage);
#else
            Console.Write(
"usage: qrbuild COMMAND [ARGS]\n" +
"\n" +
"The most commonly used commands are:\n");
            foreach (var clHandler in clHandlers) {
                Console.Write("  {0,-12}  {1}\n",
                    clHandler.Name,
                    clHandler.ShortHelp);
            }
            Console.Write(
"\n" +
"See 'qrbuild help COMMAND' for more information on a specific command.\n" +
"");
#endif
        }

        private static IList<CLHandler> GetCLHandlers()
        {
            var clHandlers = new CLHandler[] {
                new CLHandlerBuild(),
                new CLHandlerClean()
            };
            Array.Sort(clHandlers);
            return clHandlers.ToList();
        }
    }
}
