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
            get { return m_fstatCount; }
        }

        public long BytesRead
        {
            get { return 0; }
        }

        private void AtomicIncrementFStatCount()
        {
            Interlocked.Increment(ref m_fstatCount);
        }

        private long m_fstatCount;
    }
}
