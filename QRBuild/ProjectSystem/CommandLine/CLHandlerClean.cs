namespace QRBuild.ProjectSystem.CommandLine
{
    internal class CLHandlerClean : CLHandler
    {
        public override string Name
        {
            get { return "clean"; }
        }
        public override string ShortHelp
        {
            get { return "Incremental clean."; }
        }
        public override string LongHelp
        {
            get
            {
                return
"usage: qrbuild clean [options] [project-file]\n" +
"options:\n" +
"";
            }
        }

        public override void Execute(string[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}
