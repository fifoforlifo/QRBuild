namespace QRBuild
{
    public sealed class BuildOptions
    {
        public int MaxConcurrency = 1;
        public bool ContinueOnError;
        public IFileDecider FileDecider;
        public bool RemoveEmptyDirectories;
    }
}