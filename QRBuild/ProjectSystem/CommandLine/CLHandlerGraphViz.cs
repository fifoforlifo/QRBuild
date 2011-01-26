using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerGraphViz : CLHandlerProject
    {
        public CLHandlerGraphViz()
        {
            // by default, all paths are included
            m_filterList.Add(new FilterItem(FilterOp.Include, ".*"));
        }

        public override string Name
        {
            get { return "viz"; }
        }
        public override string ShortHelp
        {
            get { return "Generates dot files compatible with graphviz."; }
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

            BuildProcess buildProcess =
                projectManager.BuildGraph.CreateBuildProcess(buildOptions, targetPaths);
            WriteSimpleDotFile(buildProcess);

            return 0;
        }

        private void WriteSimpleDotFile(BuildProcess process)
        {
            int nextID = 0;

            Console.WriteLine("digraph \"{0}\" {{", ProjectFile);
            Console.WriteLine("width=\"10\";");
            Console.WriteLine("height=\"9\";");
            foreach (BuildNode buildNode in process.RequiredNodes) {
                string nodeID = String.Format("n{0}", nextID);
                nextID++;
                Console.WriteLine("node [shape=box];");
                Console.WriteLine("{0} [label=\"{1}\"];",
                    nodeID,
                    buildNode.Translation.GetType().Name);

                Console.WriteLine("node [shape=ellipse]");
                foreach (string filePath in buildNode.GetAllInputs()) {
                    bool allowFile = AllowFile(filePath);
                    if (!allowFile) {
                        continue;
                    }

                    Console.WriteLine("\"{0}\" [label=\"{1}\"];",
                        filePath,
                        Path.GetFileName(filePath));
                    Console.WriteLine("\"{0}\" -> {1};", filePath, nodeID);
                }
                foreach (string filePath in buildNode.GetAllOutputs()) {
                    bool allowFile = AllowFile(filePath);
                    if (!allowFile) {
                        continue;
                    }

                    Console.WriteLine("\"{0}\" [label=\"{1}\"];",
                        filePath,
                        Path.GetFileName(filePath));
                    Console.WriteLine("{0} -> \"{1}\";", nodeID, filePath);
                }
            }
            Console.WriteLine("}");
        }

        protected override string LongHelpExtra
        {
            get
            {
                return
"additional options:\n" +
"  -i pattern    Include files matching regex 'pattern' in graph.\n" +
"  -e pattern    Exclude files matching regex 'pattern' from graph.\n" +
"                Include and exclude may be specified more than once; each file\n" +
"                is tested against the entire list of include/exclude (the\n" +
"                'filter list').  Initially the filter list is '.*'\n" +
"";
            }
        }

        protected override ParseResult TryParseSingleArgument(string[] args, ref int i)
        {
            if (args[i] == "-i") {
                i++;
                if (i >= args.Length) {
                    Console.WriteLine("Missing argument to -i");
                    return ParseResult.Error;
                }
                m_filterList.Add(new FilterItem(FilterOp.Include, args[i]));
                return ParseResult.Handled;
            }
            if (args[i] == "-e") {
                i++;
                if (i >= args.Length) {
                    Console.WriteLine("Missing argument to -e");
                    return ParseResult.Error;
                }
                m_filterList.Add(new FilterItem(FilterOp.Exclude, args[i]));
                return ParseResult.Handled;
            }

            return ParseResult.NotHandled;
        }

        private bool AllowFile(string path)
        {
            bool allow = true;
            for (int i = 0; i < m_filterList.Count; i++) {
                FilterItem item = m_filterList[i];
                bool isMatch = item.Regex.IsMatch(path);
                if (isMatch) {
                    if (item.Op == FilterOp.Include) {
                        allow = true;
                    }
                    else if (item.Op == FilterOp.Exclude) {
                        allow = false;
                    }
                }
            }
            return allow;
        }

        private static int GetID(Dictionary<object, int> ids, object key, ref int nextID)
        {
            int result;
            if (ids.TryGetValue(key, out result)) {
                return result;
            }
            result = nextID++;
            ids[key] = result;
            return result;
        }

        enum FilterOp
        {
            Include,
            Exclude,
        }
        class FilterItem
        {
            public FilterItem(FilterOp op, string pattern)
            {
                Op = op;
                Regex = new Regex(pattern);
            }
            public readonly FilterOp Op;
            public readonly Regex Regex;
        }
        private List<FilterItem> m_filterList = new List<FilterItem>();
    }
}
