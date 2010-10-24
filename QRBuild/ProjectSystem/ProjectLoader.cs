using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QRBuild.ProjectSystem
{
    public static class ProjectLoader
    {
        public static IList<Project> Load(string text)
        {
            var result = new List<Project>();
            return result;
        }

        public static IList<Project> LoadFromFile(string path)
        {
            string text = File.ReadAllText(path);
            return Load(text);
        }

        public static string FindDefaultProjectFile()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string[] files = Directory.GetFiles(currentDir, "*.qr");
            if (files.Length == 1) {
                return files[0];
            }
            return null;
        }
    }
}
