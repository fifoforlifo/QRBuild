﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using QRBuild.IO;

namespace QRBuild.ProjectSystem.CommandLine
{
    internal abstract class CLHandlerProject : CLHandler
    {
        public override string LongHelp
        {
            get
            {
                string standardOptions =
String.Format("usage: qr {0} [options] [targets]\n", Name) +
"  targets       Space-delimited list of targets to build.\n" +
"                By default, the DefaultTarget of all projects in the\n" +
"                project file are processed.\n" +
"standard options:\n" +
"  -p fname      Load specified project file.\n" +
"  -a variant    Specify variant string.\n" +
"  -j maxproc    Max concurrent processes.\n" +
"  -m name       ModuleName regex that determines what is built.\n" +
"  -c            Contine on error.\n" +
"  -v verbosity  Verbosity level from 0 - 2.  Default 0, higher prints more.\n" +
"";
                string longHelp = standardOptions + LongHelpExtra;
                return longHelp;
            }
        }

        protected bool ParseArgs(string[] args)
        {
            for (int i = 1; i < args.Length; i++) {
                // derived class gets first try
                ParseResult parseResult = TryParseSingleArgument(args, ref i);
                if (parseResult == ParseResult.Error) {
                    return false;
                }
                else if (parseResult == ParseResult.Handled) {
                    continue;
                }

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
                if (args[i] == "-c") {
                    ContinueOnError = true;
                    continue;
                }
                if (args[i].StartsWith("-v")) {
                    if (args[i].Length == 3) {
                        if ('0' <= args[i][2] && args[i][2] <= '9') {
                            Verbosity = args[i][2] - '0';
                            continue;
                        }
                    }
                    else {
                        i++;
                        if (i >= args.Length) {
                            Console.WriteLine("Missing argument to -v");
                            return false;
                        }
                        Int32.TryParse(args[i], out Verbosity);
                        continue;
                    }
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

            if (!File.Exists(ProjectFile)) {
                Console.WriteLine("Error: file not found:");
                Console.WriteLine("    {0}", ProjectFile);
                return -1;
            }

            ProjectManager projectManager = new ProjectManager();
            Assembly assembly = projectManager.LoadProjectFile(ProjectFile, false);

            HashSet<Project> projects = projectManager.AddAllProjectsInAssembly(assembly, VariantString);

            BuildOptions buildOptions = new BuildOptions();
            buildOptions.MaxConcurrency = MaxConcurrency;
            buildOptions.ContinueOnError = ContinueOnError;
            buildOptions.FileDecider = new FileSizeDateDecider();
            buildOptions.ModuleNameRegex = ComputeModuleNameRegex(ModuleNameRegex, projects);
            ModifyOptions(buildOptions);

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
                buildOptions,
                targetFiles,
                true);

            PrintBuildResults(buildOptions, results);
            return results.Success ? 0 : -1;
        }

        internal static string FindDefaultProjectFile()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string[] files = Directory.GetFiles(currentDir, "*.qr.cs");
            if (files.Length == 1) {
                return files[0];
            }

            // search for a file named "build.qr"
            foreach (string file in files) {
                if (Path.GetFileName(file) == "build.qr.cs") {
                    return file;
                }
            }

            if (files.Length == 0) {
                Console.WriteLine("Error: no project files (*.qr.cs) found in current directory.");
                return null;
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
            for (int i = 0; i < Console.WindowWidth - 1; i++) {
                Console.Write("=");
            }
            Console.WriteLine("");
            Console.WriteLine("BuildResults.Action                     = {0}", buildResults.Action);
            Console.WriteLine("BuildResults.Success                    = {0}", buildResults.Success);
            Console.WriteLine("BuildResults.TranslationCount           = {0}", buildResults.TranslationCount);
            Console.WriteLine("BuildResults.UpToDateCount              = {0}", buildResults.UpToDateCount);
            Console.WriteLine("BuildResults.UpdateImplicitInputsCount  = {0}", buildResults.UpdateImplicitInputsCount);
            Console.WriteLine("BuildOptions.FileDecider.FStatCount     = {0}", options.FileDecider.FStatCount);
            Console.WriteLine("BuildOptions.FileDecider.BytesRead      = {0}", options.FileDecider.BytesRead);
            TimeSpan executionTime = buildResults.ExecuteEndTime - buildResults.ExecuteStartTime;
            Console.WriteLine("# ExecutionTime                         = {0}", executionTime);
        }

        protected virtual BuildAction BuildAction
        {
            get { return BuildAction.Build; }
        }

        protected virtual void ModifyOptions(BuildOptions options)
        {
        }

        protected virtual string LongHelpExtra
        {
            get { return String.Empty; }
        }

        protected enum ParseResult
        {
            Error,
            NotHandled,
            Handled,
        }

        protected virtual ParseResult TryParseSingleArgument(string[] args, ref int i)
        {
            return ParseResult.NotHandled;
        }

        protected static string ComputeModuleNameRegex(
            string moduleNameRegex,
            HashSet<Project> projects)
        {
            if (moduleNameRegex == ".init") {
                bool first = true;
                StringBuilder builder = new StringBuilder();
                foreach (Project project in projects) {
                    if (first) {
                        builder.Append(project.ModuleName);
                        first = false;
                    }
                    else {
                        builder.Append("|");
                        builder.Append(project.ModuleName);
                    }
                }
                return builder.ToString();
            }

            if (String.IsNullOrEmpty(moduleNameRegex)) {
                // match everything
                return ".*";
            }

            return moduleNameRegex;
        }

        protected string VariantString = "";
        // options
        protected string ProjectFile;
        protected int MaxConcurrency = 1;
        protected List<string> Targets = new List<string>();
        protected string ModuleNameRegex;
        protected bool ContinueOnError;
        protected int Verbosity;
    }
}
