using System;

namespace QRBuild.ProjectSystem.CommandLine
{
    public abstract class CLHandler : IComparable<CLHandler>
    {
        public abstract string Name
        {
            get;
        }
        public abstract string ShortHelp
        {
            get;
        }
        public abstract string LongHelp
        {
            get;
        }
        public abstract void Execute(string[] args);

        public int CompareTo(CLHandler rhs)
        {
            return Name.CompareTo(rhs.Name);
        }
    }
}
