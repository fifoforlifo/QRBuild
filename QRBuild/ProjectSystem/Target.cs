using System.Collections.Generic;

namespace QRBuild.ProjectSystem
{
    public sealed class Target
    {
        internal Target(string name)
        {
            Name = name;
            Targets = new HashSet<string>();
        }

        public string Name
        {
            get; private set;
        }

        public ICollection<string> Targets
        {
            get; private set;
        }
    }
}
