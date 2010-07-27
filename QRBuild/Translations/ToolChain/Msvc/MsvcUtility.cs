using System.IO;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public static class MsvcUtility
    {
        public static string GetVcVarsBatchFilePath(MsvcToolChain toolChain, string vcBinDir)
        {
            if (toolChain == MsvcToolChain.ToolsX86TargetX86) {
                string batchFilePath = Path.Combine(vcBinDir, "vcvars32.bat");
                return batchFilePath;
            }
            else if (toolChain == MsvcToolChain.ToolsX86TargetAmd64) {
                string toolsDir = Path.Combine(vcBinDir, "x86_amd64");
                string batchFilePath = Path.Combine(toolsDir, "vcvarsx86_amd64.bat");
                return batchFilePath;
            }
            else if (toolChain == MsvcToolChain.ToolsAmd64TargetAmd64) {
                string toolsDir = Path.Combine(vcBinDir, "amd64");
                string batchFilePath = Path.Combine(toolsDir, "vcvarsamd64.bat");
                return batchFilePath;
            }
            return null;
        }


    }
}
