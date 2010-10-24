using System.Collections.Generic;
using System.Linq;
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

        /// Returns an existing Target instance, or creates a new
        /// one and returns that.
        public Target CreateTarget(string name)
        {
            Target target = GetTarget(name);
            if (target == null)
            {
                target = new Target(name);
                m_targets[name] = target;
            }
            return target;
        }

        /// Returns an existing Target instance, or null if it doesn't exist.
        public Target GetTarget(string name)
        {
            Target target;
            if (m_targets.TryGetValue(name, out target))
                return target;
            return null;
        }

        /// Returns a list of all defined Targets.
        public IList<Target> GetTargets()
        {
            var targets = m_targets.Values.ToList();
            return targets;
        }

        public ICollection<string> GetTargetFiles(IEnumerable<string> targetNames)
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

                pendingTargetNames.AddRange(target.Targets);
            }

            return files;
        }

        private IDictionary<string, Target> m_targets = new Dictionary<string, Target>();
    }
}