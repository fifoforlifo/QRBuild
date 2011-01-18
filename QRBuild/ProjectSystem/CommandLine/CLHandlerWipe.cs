using System;
using System.IO;
using System.Reflection;
using QRBuild.IO;

namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerWipe : CLHandler
    {
        public override string Name
        {
            get { return "wipe"; }
        }
        public override string ShortHelp
        {
            get { return "Remove project assemblies, etc."; }
        }
        public override string LongHelp
        {
            get
            {
                return
String.Format("usage: qr {0} [options]\n", Name) +
"options:\n" +
"  -p fname      Load specified project file.\n" +
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
            Assembly assembly = projectManager.LoadProjectFile(ProjectFile, true);

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
            }

            if (ProjectFile == null) {
                ProjectFile = CLHandlerProject.FindDefaultProjectFile();
                if (ProjectFile == null) {
                    return false;
                }
            }
            ProjectFile = QRPath.GetAbsolutePath(ProjectFile, Directory.GetCurrentDirectory());

            return true;
        }

        protected string ProjectFile;
    }
}
