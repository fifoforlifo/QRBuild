using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QRBuild.Engine
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
            //SortedDictionary<string, string> ioVersions = new SortedDictionary<string, string>();

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
            sb.AppendLine(translationParameters);
            sb.AppendLine("__ExplicitInputs:");
            foreach (var kvp in eiVersions) {
                sb.AppendFormat("{0} >> {1}\n", kvp.Key, kvp.Value);
            }
            sb.AppendLine("__ExplicitsOutputs:");
            foreach (var kvp in eoVersions) {
                sb.AppendFormat("{0} >> {1}\n", kvp.Key, kvp.Value);
            }
            sb.AppendLine("__ImplicitInputs:");
            foreach (var kvp in iiVersions) {
                sb.AppendFormat("{0} >> {1}\n", kvp.Key, kvp.Value);
            }

            string result = sb.ToString();
            return result;
        }

    }
}
