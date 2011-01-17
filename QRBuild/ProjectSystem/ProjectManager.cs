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
        internal ProjectManager()
        {
            BuildGraph = new BuildGraph();
        }

        public BuildGraph BuildGraph
        {
            get; private set;
        }

        /// TODO: this should be an extension method
        public TProject GetOrCreateProject<TProject>(BuildVariant variant)
            where TProject : Project
        {
            Type type = typeof(TProject);
            Project project = GetOrCreateProject(type, variant);
            return (TProject)project;
        }

        public Project GetOrCreateProject(Type type, BuildVariant variant)
        {
            // Get the project if it already exists.
            ProjectKey newKey = new ProjectKey(type, variant);
            Project project;
            if (m_projects.TryGetValue(newKey, out project)) {
                return project;
            }

            // Create the project.
            object projectObject = Activator.CreateInstance(type);
            project = (Project)projectObject;
            project.ProjectManager = this;
            project.Variant = variant;
            project.AddToGraphOnce();
            m_projects[newKey] = project;
            return project;
        }

        internal HashSet<Project> AddAllProjectsInAssembly(Assembly assembly, string variantString)
        {
            HashSet<Project> projects = new HashSet<Project>();

            Type[] types = assembly.GetTypes();
            foreach (Type type in types) {
                object[] attributeObjects = type.GetCustomAttributes(typeof(PrimaryProjectAttribute), true);
                if (attributeObjects.Length == 1) {
                    PrimaryProjectAttribute attribute = (PrimaryProjectAttribute)attributeObjects[0];
                    BuildVariant variant = (BuildVariant)Activator.CreateInstance(attribute.VariantType);
                    variant.FromString(variantString);
                    Project project = GetOrCreateProject(type, variant);
                    projects.Add(project);
                }
            }

            return projects;
        }

        internal Assembly LoadProjectFile(string filePath, string variantString)
        {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("Project file does not exist on disk.", filePath);
            }

            ProjectAssemblyKey newKey = new ProjectAssemblyKey(filePath, variantString);
            Assembly assembly;
            if (m_projectAssemblies.TryGetValue(newKey, out assembly)) {
                if (assembly == null) {
                    // TODO: track enough info to print the reference chain
                    throw new InvalidOperationException("Circular reference detected between projects.");
                }
                return assembly;
            }

            // Place a marker that indicates we have started processing the assembly.
            // (this is the counter-part to the null-check above for detecting circular references)
            m_projectAssemblies[newKey] = null;

            ProjectLoader loader = new ProjectLoader(this, variantString, filePath);
            m_projectAssemblies[newKey] = loader.Assembly;
            return loader.Assembly;
        }

        /// Returns an existing Target instance, or creates a new
        /// one and returns that.
        public Target GetOrCreateTarget(string name)
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
                if (target == null) {
                    files.Add(targetName);
                    continue;
                }

                foreach (var childTarget in target.Targets) {
                    if (childTarget == target.Name) {
                        // This is a special escape to allow a target to be both
                        // a real file and an aggregate target.
                        files.Add(childTarget);
                    }
                    else {
                        pendingTargetNames.Push(childTarget);
                    }
                }
            }

            return files;
        }

        class ProjectAssemblyKey : IComparable<ProjectAssemblyKey>
        {
            public ProjectAssemblyKey(string path, string variantString)
            {
                Path = path;
                VariantString = variantString;
            }

            public int CompareTo(ProjectAssemblyKey rhs)
            {
                int pathDiff = Path.CompareTo(rhs.Path);
                if (pathDiff != 0) {
                    return pathDiff;
                }
                return VariantString.CompareTo(rhs.VariantString);
            }

            public readonly string Path;
            public readonly string VariantString;
        }

        class ProjectKey : IComparable<ProjectKey>
        {
            public ProjectKey(Type type, BuildVariant variant)
            {
                ProjectType = type;
                BuildVariant = variant;
            }

            public int CompareTo(ProjectKey rhs)
            {
                int typeDiff = ProjectType.FullName.CompareTo(rhs.ProjectType.FullName);
                if (typeDiff != 0) {
                    return typeDiff;
                }
                return BuildVariant.CompareTo(rhs.BuildVariant);
            }

            public readonly Type ProjectType;
            public readonly BuildVariant BuildVariant;
        }

        private readonly Dictionary<string, Target> m_targets =
            new Dictionary<string, Target>();

        private readonly Dictionary<ProjectAssemblyKey, Assembly> m_projectAssemblies =
            new Dictionary<ProjectAssemblyKey, Assembly>();

        private readonly Dictionary<ProjectKey, Project> m_projects =
            new Dictionary<ProjectKey, Project>();
    }
}