using System.Collections.Generic;

namespace QRBuild
{
    public static class BuildNodeExtensions
    {
        public static IEnumerable<string> GetAllInputs(this BuildTranslation buildTranslation)
        {
            foreach (string path in buildTranslation.ExplicitInputs) {
                yield return path;
            }
            foreach (string path in buildTranslation.ImplicitInputs) {
                yield return path;
            }
        }

        internal static IEnumerable<string> GetAllInputs(this BuildNode buildNode)
        {
            return buildNode.Translation.GetAllInputs();
        }

        public static IEnumerable<string> GetAllOutputs(this BuildTranslation buildTranslation)
        {
            foreach (string path in buildTranslation.ExplicitOutputs) {
                yield return path;
            }
        }

        internal static IEnumerable<string> GetAllOutputs(this BuildNode buildNode)
        {
            return buildNode.Translation.GetAllOutputs();
        }
    }
}