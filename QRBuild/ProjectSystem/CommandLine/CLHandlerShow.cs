using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerShow : CLHandlerProject
    {
        public override string Name
        {
            get { return "show"; }
        }
        public override string ShortHelp
        {
            get { return "Shows info about project and targets to be built."; }
        }

        // Override CLHandlerProject.Execute() with totally different implementation.
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

                string finalRegexString = ComputeModuleNameRegex(
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
            buildOptions.ModuleNameRegex = ComputeModuleNameRegex(
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

    }
}
