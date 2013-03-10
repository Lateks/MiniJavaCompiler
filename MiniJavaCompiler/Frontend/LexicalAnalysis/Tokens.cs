using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MiniJavaCompiler.FrontEnd.LexicalAnalysis
{
    public interface IToken
    {
        int Row { get; }
        int Col { get; }
        string Lexeme { get; }
    }

    /*  A static class for describing token types. These descriptions
     *  can be used in dynamically generated error messages.
     */
    public static class TokenDescriptions
    {
        private static readonly Dictionary<Type, string> Descriptions =
            new Dictionary<Type, string>()
                {
                    {typeof(TokenElement), "token"},
                    {typeof(ErrorToken), "lexical error"},
                    {typeof(IntegerLiteralToken), "integer literal"},
                    {typeof(IdentifierToken), "identifier"},
                    {typeof(KeywordToken), "keyword"},
                    {typeof(MiniJavaTypeToken), "built-in type"},
                    {typeof(OperatorToken), "operator"},
                    {typeof(PunctuationToken), "punctuation token"},
                    {typeof(EndOfFile), "end of file"},
                    {typeof(ITypeToken), "type name"}
                };

        public static string Describe(Type type)
        {
            Debug.Assert(typeof (IToken).IsAssignableFrom(type));
            if (Descriptions.ContainsKey(type))
            {
                return Descriptions[type];
            }
            return "token";
        }
    }

    // ITypeToken is a token that can appear representing a type
    // by itself.
    public interface ITypeToken : IToken { }

    // This is a base class for all token types.
    public abstract class TokenElement : IToken
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public string Lexeme { get; private set; }

        protected TokenElement(string lexeme, int row, int col)
        {
            Row = row;
            Col = col;
            Lexeme = lexeme;
        }
    }

    // This is used by the scanner: lexical errors end up as error tokens
    // in the token stream and can be handled by the parser.
    public class ErrorToken : TokenElement
    {
        public string Message { get; private set; }

        public ErrorToken(string lexeme, string message, int row, int col)
            : base(lexeme, row, col)
        {
            Message = message;
        }
    }

    public class IntegerLiteralToken : TokenElement
    {
        public IntegerLiteralToken(string value, int row, int col)
            : base(value, row, col) { }
    }

    public class IdentifierToken : TokenElement, ITypeToken
    {
        public IdentifierToken(string name, int row, int col)
            : base(name, row, col) { }
    }

    public class KeywordToken : TokenElement
    {
        public KeywordToken(string name, int row, int col)
            : base(name, row, col) { }
    }

    public class MiniJavaTypeToken : KeywordToken, ITypeToken
    {
        public MiniJavaTypeToken(string name, int row, int col)
            : base(name, row, col) { }
    }

    public class OperatorToken : TokenElement
    {
        public OperatorToken(string symbol, int row, int col)
            : base(symbol, row, col) { }
    }

    public class PunctuationToken : TokenElement
    {
        public PunctuationToken(string symbol, int row, int col)
            : base(symbol, row, col) { }
    }

    public class EndOfFile : TokenElement
    {
        public EndOfFile(int row, int col) : base(String.Empty, row, col) { }
    }
}