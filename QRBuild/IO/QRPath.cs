using System;
using System.IO;
using System.Text;

namespace QRBuild.IO
{
    public static class QRPath
    {
        /// Changes the extension of the file name.
        /// Accepts a full FilePath (drive + directory + filename + ext). 
        /// Parameter newExtension must include the leading '.'.
        public static string ChangeFileNameExtensionForPath(string filePath, string newExtension)
        {
            string dir = Path.GetDirectoryName(filePath);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            string fileNameNewExt = fileNameNoExt + newExtension;
            string newFilePath = Path.Combine(dir, fileNameNewExt);
            return newFilePath;
        }
    }
}
