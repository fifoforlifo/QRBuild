using System;
using System.IO;
using System.Threading;

namespace QRBuild
{
    public sealed class FileSizeDateDecider : IFileDecider
    {
        const string NonExistent = "NE";

        public string GetVersionStamp(string filePath)
        {
            AtomicIncrementFStatCount();

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

        public long FStatCount
        {
            get
            {
                long result = Interlocked.Add(ref m_fstatCount, 0);
                return result;
            }
        }

        private void AtomicIncrementFStatCount()
        {
            Interlocked.Increment(ref m_fstatCount);
        }

        private long m_fstatCount;
    }
}
