using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace QRBuild
{
    /// Experimental decider.
    /// Could be very slow on large builds; needs to be benchmarked.
    public sealed class FileMD5HashDecider : IFileDecider
    {
        const string NonExistent = "NE";

        public string GetVersionStamp(string filePath)
        {
            AtomicIncrementFStatCount();
            if (!File.Exists(filePath)) {
                return NonExistent;
            }

            byte[] bytes = File.ReadAllBytes(filePath);

            using (MD5 md5 = MD5.Create()) {

                byte[] hash = md5.ComputeHash(bytes);

                Interlocked.Add(ref m_bytesRead, bytes.Length);

                string hashString = BitConverter.ToString(hash);
                string stamp = string.Format(
                    "{{size={0}, MD5={1}}}",
                    bytes.Length,
                    hashString);
                return stamp;
            }
        }

        public long FStatCount
        {
            get { return m_fstatCount; }
        }

        public long BytesRead
        {
            get { return m_bytesRead; }
        }

        private void AtomicIncrementFStatCount()
        {
            Interlocked.Increment(ref m_fstatCount);
        }

        private long m_fstatCount;
        private long m_bytesRead;
    }
}
