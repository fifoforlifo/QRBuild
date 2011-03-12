using System;
using System.Collections.Generic;
using System.Text;

namespace QRBuild.Text
{
    public static class StringInterpolator
    {
        public sealed class Options
        {
            public Options()
            {
                EvalChar = '$';
                LParen = '(';
                RParen = ')';
                ThrowOnUnknownKey = true;
            }

            /// EvalChar is the character that indicates
            /// the evaluation of a substitution.
            /// The default value is '$'.
            public char EvalChar { get; set; }

            public char LParen { get; set; }
            public char RParen { get; set; }

            /// If true, then an unknown key in a substitution will cause
            /// an exception to be thrown.
            /// If false, then an empty string will be substituted.
            public bool ThrowOnUnknownKey { get; set; }
        }

        public enum TokenType
        {
            Error,
            Literal,
            Substitution,
        }

        public sealed class Token
        {
            public Token(TokenType type, string value)
            {
                Type = type;
                Value = value;
            }

            public TokenType Type { get; private set; }
            public string Value { get; private set; }
        }

        public static string Interpolate(Options options, IList<Token> tokens, Func<string, string> getValue)
        {
            StringBuilder builder = new StringBuilder();

            foreach (Token token in tokens) {
                if (token.Type == TokenType.Literal) {
                    builder.Append(token.Value);
                }
                else if (token.Type == TokenType.Substitution) {
                    string value = getValue(token.Value);
                    if (value != null) {
                        builder.Append(value);
                    }
                    else {
                        if (options.ThrowOnUnknownKey) {
                            String errorString = String.Format("Missing key '{0}' in substitution.", token.Value);
                            throw new InvalidOperationException(errorString);
                        }
                        else {
                            builder.Append(String.Empty);
                        }
                    }
                }
            }

            return builder.ToString();
        }

        public static IList<Token> Tokenize(Options options, string text)
        {
            List<Token> result = new List<Token>();
            if (String.IsNullOrEmpty(text) || options == null) {
                // return a single empty-string token
                result.Add(new Token(TokenType.Literal, ""));
                return result;
            }

            int i = 0;
            while (i < text.Length) {
                Token literal = ScanLiteral(options, text, ref i);
                if (literal != null) {
                    result.Add(literal);
                }
                if (literal.Type == TokenType.Error) {
                    break;
                }

                Token subst = ScanSubstitution(options, text, ref i);
                if (subst != null) {
                    result.Add(subst);
                }
                if (subst.Type == TokenType.Error) {
                    break;
                }
            }

            return result;
        }

        private static Token ScanLiteral(Options options, string text, ref int i)
        {
            int start = i;
            for (; i < text.Length; i++) {
                char c = text[i];
                if (c == options.EvalChar) {
                    break;
                }
            }
            return MakeLiteral(text, start, i);
        }

        private static Token MakeLiteral(string text, int start, int i)
        {
            string substr = text.Substring(start, i - start);
            return new Token(TokenType.Literal, substr);
        }

        private static Token ScanSubstitution(Options options, string text, ref int i)
        {
            if (i == text.Length) {
                return null;
            }

            int start = i;
            char c0 = text[i];
            if (c0 != options.EvalChar) {
                return null;
            }
            i++;
            if (i == text.Length) {
                // single EvalChar at the end is emitted verbatim
                return MakeLiteral(text, start, i);
            }

            char c1 = text[i];
            if (c1 == options.EvalChar) {
                // two EvalChar in a row is the escape for a literal EvalChar
                i++;
                return MakeLiteral(text, start, 1);
            }
            else if (c1 == options.LParen) {
                i++;

                int identStart = i;
                string ident = ScanIdent(text, ref i);
                if (ident == null ||
                    i == text.Length ||
                    text[i] != options.RParen) {
                    string substr = text.Substring(identStart, i - identStart);
                    return new Token(TokenType.Error, substr);
                }

                // consume the RParen
                i++;
                return new Token(TokenType.Substitution, ident);
            }
            else {
                // EvalChar is followed by insignificant character.
                // Do not consume character at position i.  (there is no i++ here)
                // Emit the EvalChar verbatim.
                return MakeLiteral(text, start, i);
            }
        }

        private static string ScanIdent(string text, ref int i)
        {
            int start = i;
            if (i == text.Length) {
                return null;
            }
            char c0 = text[i++];
            if (!IsIdentChar(c0) || IsNum(c0)) {
                return null;
            }

            for (; i < text.Length; i++) {
                char c = text[i];
                if (!IsIdentChar(c)) {
                    break;
                }
            }

            string substr = text.Substring(start, i - start);
            return substr;
        }

        private static bool IsNum(char c)
        {
            return '0' <= c && c <= '9';
        }
        private static bool IsIdentChar(char c)
        {
            return 'A' <= c && c <= 'Z'
                || 'a' <= c && c <= 'z'
                || c == '_'
                || IsNum(c);
        }
    }
}
