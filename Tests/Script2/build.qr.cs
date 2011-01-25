//@ include "env.qh"
//@ using assembly "QRBuild";
//@ using project  Common

using QRBuild;
using QRBuild.ProjectSystem;

namespace Build
{
    [PrimaryProject(typeof(MainVariant))]
    public class MainBuild : Project
    {
        protected override void AddToGraph()
        {
        }

        public override Target DefaultTarget
        {
            get
            {
                Target target = ProjectManager.GetOrCreateTarget(DefaultTargetName);
                return target;
            }
        }

        private static readonly string DefaultTargetName = "main";
    }
}
