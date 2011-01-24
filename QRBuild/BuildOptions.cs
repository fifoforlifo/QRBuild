namespace QRBuild
{
    public sealed class BuildOptions
    {
        public int MaxConcurrency = 1;
        public bool ContinueOnError;
        public IFileDecider FileDecider;
        public bool RemoveEmptyDirectories;
        /// If non-null, only Translations where the ModuleName matches
        /// this regex will be considered for processing.
        public string ModuleNameRegex;
    }
}