using System.IO;

namespace QRBuild.Translations.ToolChain.Msvc9
{
    public static class Msvc9Utility
    {
        public static string GetVcVarsBatchFilePath(Msvc9ToolChain toolChain, string vcBinDir)
        {
            if (toolChain == Msvc9ToolChain.ToolsX86TargetX86) {
                string batchFilePath = Path.Combine(vcBinDir, "vcvars32.bat");
                return batchFilePath;
            }
            else if (toolChain == Msvc9ToolChain.ToolsX86TargetAmd64) {
                string toolsDir = Path.Combine(vcBinDir, "x86_amd64");
                string batchFilePath = Path.Combine(toolsDir, "vcvarsx86_amd64.bat");
                return batchFilePath;
            }
            else if (toolChain == Msvc9ToolChain.ToolsAmd64TargetAmd64) {
                string toolsDir = Path.Combine(vcBinDir, "amd64");
                string batchFilePath = Path.Combine(toolsDir, "vcvarsamd64.bat");
                return batchFilePath;
            }
            return null;
        }


    }
}
