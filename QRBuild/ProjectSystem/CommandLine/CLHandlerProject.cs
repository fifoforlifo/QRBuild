using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using QRBuild.IO;

namespace QRBuild.ProjectSystem.CommandLine
{
    internal abstract class CLHandlerProject : CLHandler
    {
        public override string LongHelp
        {
            get
            {
                return
String.Format("usage: qr {0} [options] [targets]\n", Name) +
"  targets       Space-delimited list of targets to build.\n" +
"                By default, the DefaultTarget of all projects in the\n" +
"                project file are processed.\n" +
"options:\n" +
"  -p fname      Load specified project file.\n" +
"  -j maxproc    Max concurrent processes.\n" +
"  -v variant    Specify variant string.\n" +
"";
            }
        }

        protected bool ParseArgs(string[] args)
        {
            for (int i = 1; i < args.Length; i++) {
                if (args[i] == "-p") {
                    i++;
                    if (i >= args.Length) {
                        Console.WriteLine("Missing argument to -p");
                        return false;
                    }
                    ProjectFile = args[i];
                    continue;
                }
                if (args[i] == "-j") {
                    i++;
                    if (i >= args.Length) {
                        Console.WriteLine("Missing argument to -j");
                        return false;
                    }
                    MaxConcurrency = Int32.Parse(args[i]);
                    continue;
                }
                if (args[i] == "-v") {
                    i++;
                    if (i >= args.Length) {
                        Console.WriteLine("Missing argument to -v");
                        return false;
                    }
                    VariantString = args[i];
                    continue;
                }

                // default:
                Targets.Add(args[i]);
            }

            // Set defaults for any unset options.
            if (ProjectFile == null) {
                ProjectFile = FindDefaultProjectFile();
                if (ProjectFile == null) {
                    return false;
                }
            }
            ProjectFile = QRPath.GetAbsolutePath(ProjectFile, Directory.GetCurrentDirectory());

            return true;
        }

        public override int Execute(string[] args)
        {
            bool parseSuccess = ParseArgs(args);
            if (!parseSuccess) {
                return -1;
            }

            ProjectManager projectManager = new ProjectManager();
            Assembly assembly = projectManager.LoadProjectFile(ProjectFile);
            HashSet<Project> projects = projectManager.AddAllProjectsInAssembly(assembly, VariantString);

            BuildOptions options = new BuildOptions();
            options.FileDecider = new FileSizeDateDecider();

            HashSet<string> targetFiles;
            if (Targets.Count == 0) {
                var targets = projects.Select(project => project.DefaultTarget.Name).ToList();
                targetFiles = projectManager.GetTargetFiles(targets);
            }
            else {
                targetFiles = projectManager.GetTargetFiles(Targets);
            }

            BuildResults results = projectManager.BuildGraph.Execute(
                BuildAction,
                options,
                targetFiles,
                true);

            PrintBuildResults(options, results);
            return results.Success ? 0 : -1;
        }

        private static string FindDefaultProjectFile()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string[] files = Directory.GetFiles(currentDir, "*.qr");
            if (files.Length == 1) {
                return files[0];
            }

            // search for a file named "build.qr"
            foreach (string file in files) {
                if (Path.GetFileName(file) == "build.qr") {
                    return file;
                }
            }

            // error - multiple project files to choose from
            Console.WriteLine("Error: multiple project files to choose from:");
            foreach (string file in files) {
                string fileName = Path.GetFileName(file);
                Console.WriteLine("    {0}", fileName);
            }

            return null;
        }

        private static void PrintBuildResults(BuildOptions options, BuildResults buildResults)
        {
            Console.WriteLine("===================================================");
            Console.WriteLine("BuildResults.Action                     = {0}", buildResults.Action);
            Console.WriteLine("BuildResults.Success                    = {0}", buildResults.Success);
            Console.WriteLine("BuildResults.TranslationCount           = {0}", buildResults.TranslationCount);
            Console.WriteLine("BuildResults.UpToDateCount              = {0}", buildResults.UpToDateCount);
            Console.WriteLine("BuildResults.UpdateImplicitInputsCount  = {0}", buildResults.UpdateImplicitInputsCount);
            Console.WriteLine("BuildOptions.FileDecider.FStatCount     = {0}", options.FileDecider.FStatCount);
            TimeSpan executionTime = buildResults.ExecuteEndTime - buildResults.ExecuteStartTime;
            Console.WriteLine("# ExecutionTime                         = {0}", executionTime);
        }

        protected abstract BuildAction BuildAction
        {
            get;
        }

        protected string VariantString = "";
        // options
        protected string ProjectFile;
        protected int MaxConcurrency = 1;
        protected List<string> Targets = new List<string>();
    }
}
