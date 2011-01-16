using System;
using System.IO;
using System.Text;

namespace QRBuild.IO
{
    public static class QRFile
    {
        public static bool Delete(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (System.Exception)
            {
            	return false;
            }
        }

        public static void Delete(params string[] paths)
        {
            foreach (string path in paths) {
                Delete(path);
            }
        }

        public static bool WriteIfContentsDifferUTF8(
            string source,
            string destFilePath)
        {
            byte[] data = Encoding.UTF8.GetBytes(source);
            return WriteIfContentsDiffer(data, destFilePath);
        }

        /// Returns true if the file needed to be written.
        public static bool WriteIfContentsDiffer(
            byte[] source,
            string destFilePath)
        {
            if (FileContentsDiffer(source, destFilePath)) {
                File.WriteAllBytes(destFilePath, source);
                return true;
            }
            return false;
        }

        public static bool FileContentsDiffer(
            byte[] source,
            string destFilePath)
        {
            if (!File.Exists(destFilePath)) {
                return true;
            }

            var fileInfo = new FileInfo(destFilePath);
            if (fileInfo.Length != source.Length) {
                return true;
            }

            // lengths are the same, so load the file's current contents and compare
            byte[] dest = File.ReadAllBytes(destFilePath);
            return !ByteArraysEqual(source, dest);
        }

        private static bool ByteArraysEqual(byte[] lhs, byte[] rhs)
        {
            if (lhs.Length != rhs.Length) {
                return false;
            }
            for (int i = 0; i < lhs.Length; i++) {
                if (lhs[i] != rhs[i]) {
                    return false;
                }
            }
            return true;
        }
    }
}
