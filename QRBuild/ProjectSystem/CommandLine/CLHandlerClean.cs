namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerClean : CLHandlerProject
    {
        public override string Name
        {
            get { return "clean"; }
        }
        public override string ShortHelp
        {
            get { return "Incremental clean."; }
        }

        protected override BuildAction BuildAction
        {
            get { return BuildAction.Clean; }
        }
    }
}
