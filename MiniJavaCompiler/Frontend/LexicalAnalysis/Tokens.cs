﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MiniJavaCompiler.Frontend.LexicalAnalysis
{
    public interface IToken
    {
        int Row { get; }
        int Col { get; }
    }

    public static class TokenDescriptions
    {
        private static readonly Dictionary<Type, string> Descriptions =
            new Dictionary<Type, string>()
                {
                    {typeof(TokenElement), "token"},
                    {typeof(StringToken), "token"},
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

        internal static string Describe(Type type)
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

    public abstract class TokenElement : IToken
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        protected TokenElement(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }

    public class ErrorToken : TokenElement
    {
        public string Message { get; private set; }

        public string Lexeme { get; private set; }

        public ErrorToken(string lexeme, string message, int row, int col)
            : base(row, col)
        {
            Message = message;
            Lexeme = lexeme;
        }
    }

    public abstract class StringToken : TokenElement
    {
        public string Value { get; private set; }

        protected StringToken(string name, int row, int col)
            : base(row, col)
        {
            Value = name;
        }
    }

    public class IntegerLiteralToken : StringToken
    {
        public IntegerLiteralToken(string value, int row, int col)
            : base(value, row, col) { }
    }

    public class IdentifierToken : StringToken, ITypeToken
    {
        public IdentifierToken(string name, int row, int col)
            : base(name, row, col) { }
    }

    public class KeywordToken : StringToken
    {
        public KeywordToken(string name, int row, int col)
            : base(name, row, col) { }
    }

    public class MiniJavaTypeToken : KeywordToken, ITypeToken
    {
        public MiniJavaTypeToken(string name, int row, int col)
            : base(name, row, col) { }
    }

    public class OperatorToken : StringToken
    {
        public OperatorToken(string symbol, int row, int col)
            : base(symbol, row, col) { }
    }

    public class PunctuationToken : StringToken
    {
        public PunctuationToken(string symbol, int row, int col)
            : base(symbol, row, col) { }
    }

    public class EndOfFile : TokenElement
    {
        public EndOfFile(int row, int col) : base(row, col) { }
    }
}