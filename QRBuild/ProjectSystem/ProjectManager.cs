using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using QRBuild.IO;
using QRBuild.Linq;

namespace QRBuild.ProjectSystem
{
    public sealed class ProjectManager
    {
        public ProjectManager()
        {
            BuildGraph = new BuildGraph();
        }

        public BuildGraph BuildGraph
        {
            get; private set;
        }

        public Assembly LoadProjectFile(string filePath, BuildVariant variant)
        {
            string path = QRPath.GetCanonical(filePath);

            if (!File.Exists(path)) {
                throw new FileNotFoundException("Project file does not exist on disk.", filePath);
            }

            ProjectKey newKey = new ProjectKey(path, variant);
            Assembly assembly;
            if (m_projects.TryGetValue(newKey, out assembly)) {
                if (assembly == null) {
                    // TODO: track enough info to print the reference chain
                    throw new InvalidOperationException("Circular reference detected between projects.");
                }
                return assembly;
            }

            // Place a marker that indicates we have started processing the assembly.
            // (this is the counter-part to the null-check above for detecting circular references)
            m_projects[newKey] = null;

            ProjectLoader loader = new ProjectLoader(this, variant, path);
            m_projects[newKey] = loader.Assembly;
            return loader.Assembly;
        }

        /// Returns an existing Target instance, or creates a new
        /// one and returns that.
        public Target CreateTarget(string name)
        {
            Target target = GetTarget(name);
            if (target == null) {
                target = new Target(name);
                m_targets[name] = target;
            }
            return target;
        }

        /// Returns an existing Target instance, or null if it doesn't exist.
        public Target GetTarget(string name)
        {
            Target target;
            if (m_targets.TryGetValue(name, out target)) {
                return target;
            }
            return null;
        }

        /// Returns a list of all defined Targets.
        public IList<Target> GetTargets()
        {
            var targets = m_targets.Values.ToList();
            return targets;
        }

        public HashSet<string> GetTargetFiles(IEnumerable<string> targetNames)
        {
            HashSet<string> visitedTargets = new HashSet<string>();
            HashSet<string> files = new HashSet<string>();

            Stack<string> pendingTargetNames = new Stack<string>();
            pendingTargetNames.AddRange(targetNames);

            while (pendingTargetNames.Count > 0) {
                string targetName = pendingTargetNames.Pop();

                if (visitedTargets.Contains(targetName)) {
                    continue;
                }
                visitedTargets.Add(targetName);

                Target target = GetTarget(targetName);
                if (target == null || target.Name == targetName) {
                    files.Add(targetName);
                    continue;
                }

                pendingTargetNames.AddRange(target.Targets);
            }

            return files;
        }

        class ProjectKey : IComparable<ProjectKey>
        {
            public ProjectKey(string path, BuildVariant variant)
            {
                Path = path;
                BuildVariant = variant;
            }

            public int CompareTo(ProjectKey rhs)
            {
                int pathDiff = Path.CompareTo(rhs.Path);
                if (pathDiff != 0) {
                    return pathDiff;
                }
                return BuildVariant.CompareTo(rhs.BuildVariant);
            }

            public readonly string Path;
            public readonly BuildVariant BuildVariant;
        }

        private readonly Dictionary<string, Target> m_targets =
            new Dictionary<string, Target>();

        private readonly Dictionary<ProjectKey, Assembly> m_projects =
            new Dictionary<ProjectKey, Assembly>();
    }
}