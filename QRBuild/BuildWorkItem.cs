namespace QRBuild
{
    internal class BuildWorkItem
    {
        public BuildWorkItem(BuildNode buildNode)
        {
            BuildNode = buildNode;
        }
        public readonly BuildNode BuildNode;
        public BuildStatus ReturnedStatus;
    }
}
