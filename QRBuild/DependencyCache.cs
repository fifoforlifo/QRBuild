using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QRBuild
{
    public static class DependencyCache
    {
        public static string CreateDepsCacheString(
            BuildTranslation translation,
            IFileDecider fileDecider)
        {
            //  SortedDictionary is used to canonicalize the output.

            SortedDictionary<string, string> eiVersions = new SortedDictionary<string, string>();
            SortedDictionary<string, string> eoVersions = new SortedDictionary<string, string>();
            SortedDictionary<string, string> iiVersions = new SortedDictionary<string, string>();

            foreach (var filePath in translation.ExplicitInputs) {
                string versionStamp = fileDecider.GetVersionStamp(filePath);
                eiVersions[filePath] = versionStamp;
            }
            foreach (var filePath in translation.ExplicitOutputs) {
                string versionStamp = fileDecider.GetVersionStamp(filePath);
                eoVersions[filePath] = versionStamp;
            }
            foreach (var filePath in translation.ImplicitInputs) {
                string versionStamp = fileDecider.GetVersionStamp(filePath);
                iiVersions[filePath] = versionStamp;
            }

            string translationParameters = translation.GetCacheableTranslationParameters();
            if (translationParameters == null) {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("__ExplicitInputs:\n");
            foreach (var kvp in eiVersions) {
                sb.AppendFormat("\t{0} >> {1}\n", kvp.Key, kvp.Value);
            }
            sb.Append("__ExplicitsOutputs:\n");
            foreach (var kvp in eoVersions) {
                sb.AppendFormat("\t{0} >> {1}\n", kvp.Key, kvp.Value);
            }
            sb.Append("__ImplicitInputs:\n");
            foreach (var kvp in iiVersions) {
                sb.AppendFormat("\t{0} >> {1}\n", kvp.Key, kvp.Value);
            }
            sb.Append("__Params:\n");
            sb.Append(translationParameters);

            string result = sb.ToString();
            return result;
        }

        public static void LoadImplicitInputs(
            string depsCacheFileContents,
            HashSet<string> implicitInputs)
        {
            HashSet<string> currentSet = null;

            using (StringReader sr = new StringReader(depsCacheFileContents)) {
                while (true) {
                    string line = sr.ReadLine();
                    if (line == null) {
                        break;
                    }

                    if (line == "__ImplicitInputs:") {
                        currentSet = implicitInputs;
                        continue;
                    }
                    else if (line.StartsWith("_")) {
                        currentSet = null;
                        continue;
                    }
                    else if (line[0] == '\t') {
                        if (currentSet == null) {
                            continue;
                        }
                        int pathEnd = line.IndexOf(">>");
                        if (pathEnd != -1) {
                            pathEnd -= 1;
                            int pathStart = 1;
                            string path = line.Substring(pathStart, pathEnd - pathStart);
                            currentSet.Add(path);
                        }
                    }
                }
            }
        }

        public static void LoadDepsCacheImplicitIO(
            string filePath,
            HashSet<string> implicitInputs)
        {
            string fileContents = File.ReadAllText(filePath);
            LoadImplicitInputs(fileContents, implicitInputs);
        }
    }
}
