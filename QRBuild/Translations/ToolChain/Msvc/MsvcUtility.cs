﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using QRBuild.IO;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public static class MsvcUtility
    {
        /// Returns a path to the specified toolChain's vcvars32.bat file.
        /// This exists as a general utility, but none of the Msvc* Translation classes
        /// rely on it anymore.
        public static string GetVcVarsBatchFilePath(MsvcToolChain toolChain, string vcBinDir)
        {
            if (toolChain == MsvcToolChain.ToolsX86TargetX86) {
                string batchFilePath = Path.Combine(vcBinDir, "vcvars32.bat");
                return batchFilePath;
            }
            else if (toolChain == MsvcToolChain.ToolsX86TargetAmd64) {
                string toolsDir = Path.Combine(vcBinDir, "x86_amd64");
                string batchFilePath = Path.Combine(toolsDir, "vcvarsx86_amd64.bat");
                return batchFilePath;
            }
            else if (toolChain == MsvcToolChain.ToolsAmd64TargetAmd64) {
                string toolsDir = Path.Combine(vcBinDir, "amd64");
                string batchFilePath = Path.Combine(toolsDir, "vcvarsamd64.bat");
                return batchFilePath;
            }
            return null;
        }

        /// Parse the output for includes and error messages.
        /// Returns true if no errors, false if any irrecoverable error.
        /// TODO: This algorithm is imperfect, because the "NOTE: including file:" messages do not distinguish
        /// between quoted-form versus angle-bracket-form includes.  Since the rules on header search are
        /// different between the two, there is always a chance that we will guess a wrong generated file is
        /// a dependency of this translation.
        public static bool ParseShowIncludesText(
            BuildGraph buildGraph,
            string compilerOutput,
            string compileDir,
            IList<string> includeDirs,
            HashSet<string> includes)
        {
            bool foundError = false;

            // includesList is ordered, since the search algorithm requires it
            IList<string> includesList = new List<string>();

            using (StringReader sr = new StringReader(compilerOutput)) {
                while (true) {
                    string line = sr.ReadLine();
                    if (line == null) {
                        break;
                    }

                    string existingIncludePrefix = "Note: including file:";
                    if (line.StartsWith(existingIncludePrefix)) {
                        int pathStart;
                        for (pathStart = existingIncludePrefix.Length; pathStart < line.Length; pathStart++) {
                            if (line[pathStart] != ' ') {
                                break;
                            }
                        }
                        string path = line.Substring(pathStart);
                        string absPath = QRPath.GetCanonical(path);
                        includesList.Add(absPath);
                    }
                    else if (line.Contains("fatal error C1083")) {
                        // C1083 is the "missing include file" error
                        // Extract the missing file-name and add it to inputs.
                        string missingIncludePrefix = "Cannot open include file: '";
                        int prefixIndex = line.IndexOf(missingIncludePrefix);
                        if (prefixIndex > 0) {
                            int fileNameStartIndex = prefixIndex + missingIncludePrefix.Length;
                            int cursor = fileNameStartIndex;
                            for (; cursor < line.Length; cursor++) {
                                if (line[cursor] == '\'') {
                                    break;
                                }
                            }
                            string path = line.Substring(fileNameStartIndex, cursor - fileNameStartIndex);
                            string absPath = SearchIncludePathsForGeneratedFile(
                                buildGraph, compileDir, includeDirs, path);
                            if (absPath != null) {
                                includesList.Add(absPath);
                            }
                            else {
                                // Trace an error that the missing include can never exist in this build.
                                Trace.TraceError("Missing header '{0}' not generated by any Translation.", path);
                                foundError = true;
                            }
                        }
                    }
                    else if (line.Contains("error")) {
                        foundError = true;
                    }
                }
            }

            foreach (string include in includesList) {
                includes.Add(include);
            }
            return !foundError;
        }

        /// Returns path to the specified include file. 
        /// TODO: still needs work.  The full algorithm to emulate is here:
        /// http://msdn.microsoft.com/en-us/library/36k2cdd4.aspx
        private static string SearchIncludePathsForGeneratedFile(
            BuildGraph buildGraph,
            string compileDir,
            IList<string> includeDirs,
            string path)
        {
            if (Path.IsPathRooted(path)) {
                return path;
            }

            foreach (string includeDir in includeDirs) {
                string absPath = QRPath.GetAbsolutePath(path, includeDir);
                BuildFile buildFile = buildGraph.GetBuildFile(absPath);
                if (buildFile != null && buildFile.BuildNode != null) {
                    return absPath;
                }
            }

            // Try the compileDir as a fallback.
            {
                string absPath = QRPath.GetAbsolutePath(path, compileDir);
                BuildFile buildFile = buildGraph.GetBuildFile(absPath);
                if (buildFile != null && buildFile.BuildNode != null) {
                    return absPath;
                }
            }

            // No path 
            return null;
        }
    }
}
