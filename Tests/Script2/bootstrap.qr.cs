//@ outdir "built";
//@ using assembly "QRBuild";
//@ using project  "common.qr.cs";

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QRBuild.IO;
using QRBuild.ProjectSystem;
using QRBuild.Translations.IO;

namespace Build
{
    public class BootstrapVariant : BuildVariant
    {
        // no fields since bootstrap does not vary
    }

    [PrimaryProject(typeof(BootstrapVariant))]
    public class Bootstrap : Project
    {
        protected override void AddToGraph()
        {
            var locations = Locations.S.GetLocations();

            string dir = QRPath.GetAssemblyDirectory(typeof(Locations), 1);
            string fnam = "env.qh.cs";
            string envqhPath = Path.Combine(dir, fnam);
            // Generate into a "built" subdirectory off of each projects' locations.
            string destRelPath = Path.Combine("built", fnam);

            // Generate the source file here.
            string envqhContents = GenerateEnvQh(locations);
            QRFile.WriteIfContentsDifferUTF8(envqhContents, envqhPath);

            foreach (var kvp in locations) {
                // Only generate env.qh for project files (*.qr).
                if (!kvp.Value.EndsWith(".qr.cs")) {
                    continue;
                }

                string locationDir = Path.GetDirectoryName(kvp.Value);
                if (locationDir != dir) {
                    string destFilePath = Path.Combine(locationDir, destRelPath);

                    new FileCopy(
                        ProjectManager.BuildGraph,
                        envqhPath,
                        destFilePath,
                        locationDir + "\\built");

                    DefaultTarget.Targets.Add(destFilePath);
                }
            }
        }

        public override Target DefaultTarget
        {
            get
            {
                Target target = ProjectManager.GetOrCreateTarget(DefaultTargetName);
                return target;
            }
        }

        private string GenerateEnvQh(IDictionary<string, string> locations)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var kvp in locations) {
                builder.AppendFormat(
                    "//@ define  {0,-20}    \"{1}\"{2}",
                    kvp.Key,
                    kvp.Value,
                    Environment.NewLine);
            }
            string result = builder.ToString();
            return result;
        }

        private static readonly string DefaultTargetName = "bootstrap";
    }
}
