namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerClobber : CLHandlerProject
    {
        public override string Name
        {
            get { return "clobber"; }
        }
        public override string ShortHelp
        {
            get { return "Delete all built files, remove empty directories."; }
        }

        protected override BuildAction BuildAction
        {
            get { return BuildAction.Clean; }
        }

        protected override void ModifyOptions(BuildOptions options)
        {
            options.RemoveEmptyDirectories = true;
        }
    }
}
