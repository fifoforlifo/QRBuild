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
        public static string ChangeExtension(string filePath, string newExtension)
        {
            string dir = Path.GetDirectoryName(filePath);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            string fileNameNewExt = fileNameNoExt + newExtension;
            string newFilePath = Path.Combine(dir, fileNameNewExt);
            return newFilePath;
        }

        public static string ComputeDefaultFilePath(string pathOrDir, string filePath, string newExtension)
        {            
            if (String.IsNullOrEmpty(pathOrDir)) {
                string path = ChangeExtension(filePath, newExtension);
                return path;
            }
            else {
                if (pathOrDir[pathOrDir.Length - 1] == Path.DirectorySeparatorChar) {
                    // pathOrDir represents a directory.
                    string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
                    string pathNoExt = Path.Combine(pathOrDir, fileNameNoExt);
                    string path = pathNoExt + newExtension;
                    return path;
                }
                else {
                    // pathOrDir represents a file.
                    return pathOrDir;
                }
            }
        }
    }
}
