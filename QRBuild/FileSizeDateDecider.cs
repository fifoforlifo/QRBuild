using System;
using System.IO;
using QRBuild.Engine;

namespace QRBuild
{
    public sealed class FileSizeDateDecider : IFileDecider
    {
        const string NonExistent = "NE";

        public string GetVersionStamp(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return NonExistent;
            }

            string stamp = string.Format(
                "{{{0:x}, {1:x}}}",
                fileInfo.Length,
                fileInfo.LastAccessTimeUtc.Ticks);
            return stamp;
        }
    }
}
