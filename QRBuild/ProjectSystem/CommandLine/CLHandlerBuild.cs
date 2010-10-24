namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerBuild : CLHandler
    {
        public override string Name
        {
            get { return "build"; }
        }
        public override string ShortHelp
        {
            get { return "Incremental build."; }
        }
        public override string LongHelp
        {
            get
            {
                return
"usage: qrbuild build [options] [targets]\n" +
"  targets       Space-delimited list of targets to build.\n" +
"options:\n" +
"  -p            Project file.\n" +
"";
            }
        }

        public override void Execute(string[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}
