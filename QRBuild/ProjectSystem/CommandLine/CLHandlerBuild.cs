namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerBuild : CLHandlerProject
    {
        public override string Name
        {
            get { return "build"; }
        }
        public override string ShortHelp
        {
            get { return "Incremental build."; }
        }

        protected override BuildAction BuildAction
        {
            get { return BuildAction.Build; }
        }
    }
}
