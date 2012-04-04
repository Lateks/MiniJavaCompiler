using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler
{
    namespace Support
    {
        namespace TokenTypes
        {
            public interface Token
            {
                int Row { get; }
                int Col { get; }
            }

            public interface TypeToken : Token { }

            public abstract class TokenElement : Token
            {
                public int Row
                {
                    get;
                    private set;
                }
                public int Col
                {
                    get;
                    private set;
                }

                public TokenElement(int row, int col)
                {
                    Row = row;
                    Col = col;
                }
            }

            public class ErrorToken : TokenElement
            {
                public string Message
                {
                    get;
                    private set;
                }

                public string Lexeme
                {
                    get;
                    private set;
                }

                public ErrorToken(string lexeme, string message, int row, int col)
                    : base(row, col)
                {
                    Message = message;
                    Lexeme = lexeme;
                }
            }

            public class StringToken : TokenElement
            {
                public string Value
                {
                    get;
                    private set;
                }

                public StringToken(string name, int row, int col)
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

            public class Identifier : StringToken, TypeToken
            {
                public Identifier(string name, int row, int col)
                    : base(name, row, col) { }
            }

            public class KeywordToken : StringToken
            {
                public KeywordToken(string name, int row, int col)
                    : base(name, row, col) { }
            }

            public class MiniJavaType : KeywordToken, TypeToken
            {
                public MiniJavaType(string name, int row, int col)
                    : base(name, row, col) { }
            }

            public class BinaryOperatorToken : StringToken
            {
                public BinaryOperatorToken(string symbol, int row, int col)
                    : base(symbol, row, col) { }
            }

            public class ArithmeticOperatorToken : BinaryOperatorToken
            {
                public ArithmeticOperatorToken(string symbol, int row, int col)
                    : base(symbol, row, col) { }
            }

            public class LogicalOperatorToken : BinaryOperatorToken
            {
                public LogicalOperatorToken(string symbol, int row, int col)
                    : base(symbol, row, col) { }
            }

            public class UnaryNotToken : TokenElement
            {
                public UnaryNotToken(int row, int col)
                    : base(row, col) { }
            }

            public class LeftParenthesis : TokenElement
            {
                public LeftParenthesis(int row, int col)
                    : base(row, col) { }
            }

            public class RightParenthesis : TokenElement
            {
                public RightParenthesis(int row, int col)
                    : base(row, col) { }
            }

            public class EndLine : TokenElement
            {
                public EndLine(int row, int col)
                    : base(row, col) { }
            }

            public class AssignmentToken : TokenElement
            {
                public AssignmentToken(int row, int col)
                    : base(row, col) { }
            }

            public class TypeDeclaration : TokenElement
            {
                public TypeDeclaration(int row, int col)
                    : base(row, col) { }
            }

            public class LeftBracket : TokenElement
            {
                public LeftBracket(int row, int col)
                    : base(row, col) { }
            }

            public class RightBracket : TokenElement
            {
                public RightBracket(int row, int col)
                    : base(row, col) { }
            }

            public class LeftCurlyBrace : TokenElement
            {
                public LeftCurlyBrace(int row, int col)
                    : base(row, col) { }
            }

            public class RightCurlyBrace : TokenElement
            {
                public RightCurlyBrace(int row, int col)
                    : base(row, col) { }
            }

            public class MethodInvocationToken : TokenElement
            {
                public MethodInvocationToken(int row, int col)
                    : base(row, col) { }
            }

            public class ParameterSeparator : TokenElement
            {
                public ParameterSeparator(int row, int col)
                    : base(row, col) { }
            }

            public class EOF : TokenElement
            {
                public EOF(int row, int col) : base(row, col) { }
            }
        }
    }
}