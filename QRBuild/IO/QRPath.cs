using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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

        public static string GetCanonical(string path)
        {
            string absPath = Path.GetFullPath(path);
            string lowerAbsPath = absPath.ToLower();
            return lowerAbsPath;
        }

        public static string GetAbsolutePath(string path, string currentDir)
        {
            if (Path.IsPathRooted(path)) {
                string absolutePath = QRPath.GetCanonical(path);
                return absolutePath;
            }
            else {
                string fp2 = Path.Combine(currentDir, path);
                string absolutePath = QRPath.GetCanonical(fp2);
                return absolutePath;
            }
        }

        public static string ComputeDefaultFilePath(string pathOrDir, string filePath, string newExtension, string currentDir)
        {            
            if (String.IsNullOrEmpty(pathOrDir)) {
                string path = ChangeExtension(filePath, newExtension);
                string absPath = GetAbsolutePath(path, currentDir);
                return absPath;
            }
            else {
                if (pathOrDir[pathOrDir.Length - 1] == Path.DirectorySeparatorChar) {
                    // pathOrDir represents a directory.
                    string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
                    string pathNoExt = Path.Combine(pathOrDir, fileNameNoExt);
                    string path = pathNoExt + newExtension;
                    string absPath = GetAbsolutePath(path, currentDir);
                    return absPath;
                }
                else {
                    // pathOrDir represents a file.
                    string absPath = GetAbsolutePath(pathOrDir, currentDir);
                    return absPath;
                }
            }
        }

        public static void AddRangeAsAbsolutePaths(this ICollection<string> dest, IEnumerable<string> src, string currentDir)
        {
            foreach (string path in src) {
                string absPath = GetAbsolutePath(path, currentDir);
                dest.Add(path);
            }
        }

        public static string FindFileWithExt(IEnumerable<string> paths, string extension)
        {
            foreach (string path in paths) {
                if (Path.GetExtension(path) == extension) {
                    return path;
                }
            }
            return null;
        }

        public static string GetAssemblyFilePath(Type type)
        {
            return Assembly.GetAssembly(type).Location;
        }
        public static string GetAssemblyDirectory(Type type)
        {
            string filePath = GetAssemblyFilePath(type);
            string dir = Path.GetDirectoryName(filePath);
            return dir;
        }
        public static string GetAssemblyDirectory(Type type, int parentCount)
        {
            string assemblyDir = GetAssemblyDirectory(type);
            if (parentCount <= 0) {
                return assemblyDir;
            }
            string parentPath = "..";
            for (int i = 1; i < parentCount; i++) {
                parentPath = Path.Combine(parentPath, "..");
            }

            string dir = GetCanonical(
                Path.Combine(assemblyDir, parentPath)
            );
            return dir;
        }
    }
}
