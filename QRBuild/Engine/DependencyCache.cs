using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QRBuild.Engine
{
    internal static class DependencyCache
    {
        public static string CreateDepsCacheString(
            BuildTranslation translation,
            IFileDecider fileDecider,
            IEnumerable<BuildFile> inputs,
            IEnumerable<BuildFile> outputs)
        {
            //  SortedDictionary is used to canonicalize the output.

            SortedDictionary<string, string> inputVersionStamps = new SortedDictionary<string, string>();
            SortedDictionary<string, string> outputVersionStamps = new SortedDictionary<string, string>();            

            foreach (var buildFile in inputs)
            {
                string versionStamp = fileDecider.GetVersionStamp(buildFile.Id);
                inputVersionStamps[buildFile.Id] = versionStamp;
            }
            foreach (var buildFile in outputs)
            {
                string versionStamp = fileDecider.GetVersionStamp(buildFile.Id);
                outputVersionStamps[buildFile.Id] = versionStamp;
            }

            string translationParameters = translation.GetCacheableTranslationParameters();
            if (translationParameters == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(translationParameters);
            sb.AppendLine("Inputs:");
            foreach (var kvp in inputVersionStamps)
            {
                sb.AppendFormat("{0} : {1}\n", kvp.Key, kvp.Value);
            }
            sb.AppendLine("Outputs:");
            foreach (var kvp in outputVersionStamps)
            {
                sb.AppendFormat("{0} : {1}\n", kvp.Key, kvp.Value);
            }

            string result = sb.ToString();
            return result;
        }

    }
}
