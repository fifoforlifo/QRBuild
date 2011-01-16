using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;
using QRBuild.IO;
using QRBuild.Translations.ToolChain.MsCsc;

namespace QRBuild.ProjectSystem
{
    internal class ProjectLoader
    {
        public Assembly Assembly
        {
            get; private set;
        }

        public HashSet<Assembly> UsingAssemblies
        {
            get { return m_usingAssemblies; }
        }

        public ProjectManager ProjectManager
        {
            get; private set;
        }

        public BuildVariant BuildVariant
        {
            get; private set;
        }

        public ProjectLoader(
            ProjectManager projectManager,
            BuildVariant variant,
            string filePath)
        {
            ProjectManager = projectManager;
            BuildVariant = variant;
            m_currentDir = Path.GetDirectoryName(filePath);

            string text = File.ReadAllText(filePath);
            ProcessText(text);
            string assemblyFilePath = Compile(filePath);

            Assembly = TryLoadAssembly(assemblyFilePath);
        }

        private void ProcessText(string text)
        {
            using (StringReader reader = new StringReader(text)) {
                int lineNumber = 0;
                for (string line = ""; line != null; line = reader.ReadLine(), lineNumber++) {
                    const string prefix = "//@";
                    if (!line.StartsWith(prefix)) {
                        continue;
                    }

                    ProcessLine(line, prefix.Length);
                }
            }
        }

        private string Compile(string filePath)
        {
            BuildGraph buildGraph = new BuildGraph();
            var cscp = new CSharpCompileParams();
            cscp.BuildFileDir = m_currentDir + "\\build";
            cscp.Sources.Add(filePath);
            cscp.OutputFilePath = filePath + ".dll";
            cscp.TargetFormat = CSharpTargetFormats.Library;
            cscp.FrameworkVersion = CSharpFrameworkVersion.V3_5;
            cscp.CompileDir = m_currentDir;
            cscp.Debug = true;
            cscp.Platform = CSharpPlatforms.AnyCpu;
            foreach (Assembly assembly in UsingAssemblies) {
                cscp.AssemblyReferences.Add(assembly.Location);
            }
            var csc = new CSharpCompile(buildGraph, cscp);

            BuildOptions options = new BuildOptions();
            options.FileDecider = new FileSizeDateDecider();
            var targets = new [] { cscp.OutputFilePath };
            BuildResults results = buildGraph.Execute(BuildAction.Build, options, targets, true);

            if (!results.Success) {
                throw new InvalidOperationException();
            }

            return cscp.OutputFilePath;
        }

        enum TokenID
        {
            Error = 0,
            End,
            WhiteSpace,
            Comment,
            Ident,
            String,
            SemiColon,
        }
        class Token
        {
            public Token(TokenID id, string text, int startIndex, int endIndex)
            {
                ID = id;
                Value = text.Substring(startIndex, endIndex - startIndex);
                StartIndex = startIndex;
                EndIndex = endIndex;
            }
            public readonly TokenID ID;
            public readonly string Value;
            public readonly int StartIndex;
            public readonly int EndIndex;
        }

        private static bool IsIdentChar(char c)
        {
            bool result =
                    ('A' <= c && c <= 'Z')
                ||  ('a' <= c && c <= 'z')
                ||  (c == '_')
                ;
            return result;
        }

        private static Token GetNextToken(string text, ref int index)
        {
            if (index >= text.Length) {
                return new Token(TokenID.End, "", 0, 0);
            }
            int startIndex = index;

            char c = text[index++];
            if (c == ' ' || c == '\t') {
                return new Token(TokenID.WhiteSpace, text, startIndex, index);
            }
            else if (IsIdentChar(c)) {
                while (index < text.Length) {
                    c = text[index];
                    if (!IsIdentChar(c)) {
                        break;
                    }
                    index++;
                }
                return new Token(TokenID.Ident, text, startIndex, index);
            }
            else if (c == '"') {
                if (index >= text.Length) {
                    return new Token(TokenID.Error, text, startIndex, index);
                }

                while (index < text.Length) {
                    c = text[index++];
                    if (c == '"') {
                        break;
                    }
                }

                if (c != '"') {
                    return new Token(TokenID.Error, text, startIndex, index);
                }

                return new Token(TokenID.String, text, startIndex, index);
            }
            else if (c == '/') {
                if (index >= text.Length) {
                    return new Token(TokenID.Error, text, startIndex, index);
                }
                c = text[index++];
                if (c == '/') {
                    return new Token(TokenID.Comment, text, startIndex, text.Length);
                }
                else {
                    return new Token(TokenID.Error, text, startIndex, index);
                }
            }
            else if (c == ';') {
                return new Token(TokenID.SemiColon, text, startIndex, index);
            }
            else {
                return new Token(TokenID.Error, text, startIndex, index);
            }
        }

        /// Returns a list of interesting tokens.  No whitespace or comments are returned.
        private static List<Token> Tokenize(string text, int startIndex)
        {
            List<Token> tokens = new List<Token>();

            int index = startIndex;
            Token token = GetNextToken(text, ref index);
            while (token.ID != TokenID.End) {
                if (token.ID == TokenID.Error) {
                    throw new InvalidDataException();
                }
                else if (token.ID != TokenID.WhiteSpace && token.ID != TokenID.Comment) {
                    tokens.Add(token);
                }
                token = GetNextToken(text, ref index);
            }
            return tokens;
        }

        private void ProcessLine(string text, int textIndex)
        {
            List<Token> tokens = Tokenize(text, textIndex);
            if (tokens.Count < 1) {
                return;
            }

            Token command = tokens[0];
            if (command.ID == TokenID.Ident) {
                switch (command.Value) {
                    case "define":
                        Define(tokens);
                        break;
                    case "include":
                        Include(tokens);
                        break;
                    case "using":
                        Using(tokens);
                        break;
                    default:
                        throw new InvalidDataException("Invalid command.");
                }
            }
            else {
                throw new InvalidDataException();
            }
        }

        private string ResolveTokenToString(Token token)
        {
            if (token.ID == TokenID.String) {
                string unquotedValue = token.Value.Substring(1, token.Value.Length - 2);
                return unquotedValue;
            }
            else if (token.ID == TokenID.Ident) {
                // Resolve the name; it's a 1-level lookup because we only store
                // strings in m_defines.
                string finalValue;
                if (m_defines.TryGetValue(token.Value, out finalValue)) {
                    return finalValue;
                }
                else {
                    throw new InvalidDataException();
                }
            }
            else {
                throw new InvalidDataException();
            }
        }

        private void Define(List<Token> tokens)
        {
            if (tokens.Count < 3) {
                throw new InvalidDataException();
            }
            Token name = tokens[1];
            Token value = tokens[2];

            if (name.ID != TokenID.Ident) {
                throw new InvalidDataException();
            }

            string finalValue = ResolveTokenToString(value);
            m_defines[name.Value] = finalValue;
        }

        private void Include(List<Token> tokens)
        {
            if (tokens.Count < 2) {
                throw new InvalidDataException();
            }
            Token includeFile = tokens[1];
            string rawIncludeName = ResolveTokenToString(includeFile);
            string filePath = QRPath.GetAbsolutePath(rawIncludeName, m_currentDir);

            string text = File.ReadAllText(filePath);
            ProcessText(text);
        }

        private void Using(List<Token> tokens)
        {
            if (tokens.Count < 3) {
                throw new InvalidDataException();
            }

            Token usingType = tokens[1];
            if (usingType.ID != TokenID.Ident) {
                throw new InvalidDataException();
            }

            if (usingType.Value == "assembly") {
                Token target = tokens[2];
                string assemblyNameString = ResolveTokenToString(target);
                Assembly assembly = TryLoadAssembly(assemblyNameString);
                m_usingAssemblies.Add(assembly);
            }
            else if (usingType.Value == "project") {
                Token target = tokens[2];
                string projectFileName = ResolveTokenToString(target);
                Assembly assembly = ProjectManager.LoadProjectFile(projectFileName, BuildVariant);
                m_usingAssemblies.Add(assembly);
            }
            else {
                throw new InvalidDataException();
            }
        }

        private static Assembly TryLoadAssembly(string name)
        {
            if (name == "QRBuild") {
                return typeof(ProjectLoader).Assembly;
            }

            Assembly assembly = null;
            if (File.Exists(name)) {
                assembly = Assembly.LoadFrom(name);
            }
            else {
                assembly = Assembly.LoadWithPartialName(name);
            }
            return assembly;
        }

        public static string FindDefaultProjectFile()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string[] files = Directory.GetFiles(currentDir, "*.qr");
            if (files.Length == 1) {
                return files[0];
            }
            return null;
        }

        /// 'define name value'
        private readonly Dictionary<string, string> m_defines =
            new Dictionary<string, string>();

        /// 'using assembly name' or 'using project name'
        private readonly HashSet<Assembly> m_usingAssemblies =
            new HashSet<Assembly>();

        /// Current directory to use when computing file paths.
        private readonly string m_currentDir;
    }
}
