using System;
using System.IO;

namespace QRBuild
{
    public sealed class FileSizeDateDecider : IFileDecider
    {
        const string NonExistent = "NE";

        public string GetVersionStamp(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) {
                return NonExistent;
            }

            string stamp = string.Format(
                "{{size={0}, date={1}}}",
                fileInfo.Length,
                fileInfo.LastWriteTimeUtc.Ticks);
            return stamp;
        }
    }
}
