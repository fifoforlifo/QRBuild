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
            get { return "Delete all built files, do not remove directories."; }
        }

        protected override BuildAction BuildAction
        {
            get { return BuildAction.Clean; }
        }
    }
}
