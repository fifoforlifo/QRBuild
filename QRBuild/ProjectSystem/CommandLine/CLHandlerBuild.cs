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
"usage: qr build variant [options] [targets]\n" +
"  targets       Space-delimited list of targets to build.\n" +
"                By default, the DefaultTarget of all projects in the\n" +
"                project file are processed.\n" +
"options:\n" +
"  -p fname      Load specified project file.\n" +
"";
            }
        }

        public override void Execute(string[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}
