using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using QRBuild.IO;

namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerShow : CLHandler
    {
        public override string Name
        {
            get { return "show"; }
        }
        public override string ShortHelp
        {
            get { return "Shows info about project and targets to be built."; }
        }

        public override string LongHelp
        {
            get
            {
                return
"usage: qr show [options] [targets]\n" +
"  targets       Space-delimited list of targets to build.\n" +
"                By default, the DefaultTarget of all projects in the\n" +
"                project file are processed.\n" +
"options:\n" +
"  -p fname      Load specified project file.\n" +
"  -a variant    Specify variant string.\n" +
"  -m name       ModuleName regex that determines what is built.\n" +
"  -v verbosity  Verbosity level from 0 - 2.  Default 0, higher prints more." +
"";
            }
        }

        public override int Execute(string[] args)
        {
            bool parseSuccess = ParseArgs(args);
            if (!parseSuccess) {
                return -1;
            }

            if (!File.Exists(ProjectFile)) {
                Console.WriteLine("Error: file not found:");
                Console.WriteLine("    {0}", ProjectFile);
                return -1;
            }

            ProjectManager projectManager = new ProjectManager();
            Assembly assembly = projectManager.LoadProjectFile(ProjectFile, false);

            HashSet<Project> projects = projectManager.AddAllProjectsInAssembly(assembly, VariantString);

            if (Verbosity >= 0) {
                Console.WriteLine("");

                string finalRegexString = CLHandlerProject.ComputeModuleNameRegex(
                    ModuleNameRegex, projects);
                Regex moduleNameRegex = new Regex(finalRegexString);

                foreach (Project project in projects) {
                    bool isModuleMatch = moduleNameRegex.IsMatch(project.ModuleName);
                    if (!isModuleMatch && Verbosity < 1) {
                        continue;
                    }

                    Console.WriteLine("class {0}:", project.GetType().Name);
                    Console.WriteLine("  ModuleName          = {0}", project.ModuleName);
                    Console.WriteLine("  VariantStringFormat = {0}", project.Variant.GetVariantStringFormat());
                    Console.Write(project.Variant.GetVariantStringOptions("                        "));
                    Console.WriteLine("  DefaultTarget.Name  = {0}", project.DefaultTarget.Name);
                    int index = 0;
                    foreach (string subTarget in project.DefaultTarget.Targets) {
                        Console.WriteLine("    [{0,4}] = {1}", index, subTarget);
                        index++;
                    }
                }
            }

            if (Verbosity >= 1) {
                Console.WriteLine("");
                Console.WriteLine("All Target Files:");

                HashSet<string> targetFiles;
                if (Targets.Count == 0) {
                    var targets = projects.Select(project => project.DefaultTarget.Name).ToList();
                    targetFiles = projectManager.GetTargetFiles(targets);
                }
                else {
                    targetFiles = projectManager.GetTargetFiles(Targets);
                }
                int index = 0;
                foreach (string targetFile in targetFiles) {
                    Console.WriteLine("    [{0,4}] = {1}", index, targetFile);
                    index++;
                }
            }

            BuildOptions buildOptions = new BuildOptions();
            buildOptions.FileDecider = new FileSizeDateDecider();
            buildOptions.ModuleNameRegex = CLHandlerProject.ComputeModuleNameRegex(
                ModuleNameRegex, projects);

            HashSet<string> targetPaths;
            if (Targets.Count == 0) {
                var targets = projects.Select(project => project.DefaultTarget.Name).ToList();
                targetPaths = projectManager.GetTargetFiles(targets);
            }
            else {
                targetPaths = projectManager.GetTargetFiles(Targets);
            }

            if (Verbosity >= 2) {
                HashSet<string> inputs, outputs;
                projectManager.BuildGraph.GetInputsAndOutputsForTargets(
                    buildOptions,
                    targetPaths,
                    out inputs,
                    out outputs);

                List<string> sortedInputs = new List<string>(inputs);
                sortedInputs.Sort();
                List<string> sortedOutputs = new List<string>(outputs);
                sortedOutputs.Sort();

                Console.WriteLine("");
                Console.WriteLine("Input Files:");
                int index = 0;
                foreach (string path in sortedInputs) {
                    Console.WriteLine("    [{0,4}] = {1}", index, path);
                    index++;
                }

                Console.WriteLine("");
                Console.WriteLine("Output Files:");
                index = 0;
                foreach (string path in sortedOutputs) {
                    Console.WriteLine("    [{0,4}] = {1}", index, path);
                    index++;
                }
            }

            return 0;
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
                if (args[i] == "-a") {
                    i++;
                    if (i >= args.Length) {
                        Console.WriteLine("Missing argument to -a");
                        return false;
                    }
                    VariantString = args[i];
                    continue;
                }
                if (args[i] == "-m") {
                    i++;
                    if (i >= args.Length) {
                        Console.WriteLine("Missing argument to -m");
                        return false;
                    }
                    ModuleNameRegex = args[i];
                    continue;
                }
                if (args[i] == "-v") {
                    i++;
                    if (i >= args.Length) {
                        Console.WriteLine("Missing argument to -v");
                        return false;
                    }
                    Int32.TryParse(args[i], out Verbosity);
                    continue;
                }
                if (args[i][0] == '-') {
                    Console.WriteLine("Unknown option '{0}'.", args[i]);
                    return false;
                }

                // default:
                Targets.Add(args[i]);
            }

            // Set defaults for any unset options.
            if (ProjectFile == null) {
                ProjectFile = CLHandlerProject.FindDefaultProjectFile();
                if (ProjectFile == null) {
                    return false;
                }
            }
            ProjectFile = QRPath.GetAbsolutePath(ProjectFile, Directory.GetCurrentDirectory());

            return true;
        }

        protected string VariantString = "";
        // options
        protected string ProjectFile;
        protected List<string> Targets = new List<string>();
        protected string ModuleNameRegex;
        protected int Verbosity;
    }
}
