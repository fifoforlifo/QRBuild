using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QRBuild.IO
{
    public static class QRDirectory
    {
        public static void EnsureDirectoryExists(string dir)
        {
            // TODO: CreateDirectory() is apparently buggy w.r.t. security
            Directory.CreateDirectory(dir);
        }

        public static void EnsureDirectoryExistsForFile(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            EnsureDirectoryExists(dir);
        }

        public static void RemoveEmptyDirectories(string path)
        {
            string current = QRPath.GetCanonical(path);
            for (; current != null; current = Path.GetDirectoryName(current)) {
                if (Directory.Exists(current)) {
                    // NOTE: there is no function in the .NET Framework 3.5 for
                    // querying whether a directory is empty without incurring the
                    // overhead of enumerating file names.
                    // http://stackoverflow.com/questions/755574/how-to-quickly-check-if-folder-is-empty-net

                    bool removed = false;
                    try {
                        Directory.Delete(current);
                        removed = true;
                    }
                    catch (Exception) {
                        // Most often IOException occurs,
                        // indicating a non-empty directory
                        removed = false;
                    }
                    if (!removed) {
                        // cannot remove directory
                        break;
                    }
                }
            }
        }
    }
}
