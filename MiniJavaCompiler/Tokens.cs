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
            public abstract class Token
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

                public Token(int row, int col)
                {
                    Row = row;
                    Col = col;
                }
            }

            public class ErrorToken : Token
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

            public class StringToken : Token
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

            public class Identifier : StringToken
            {
                public Identifier(string name, int row, int col)
                    : base(name, row, col) { }
            }

            public class KeywordToken : StringToken
            {
                public KeywordToken(string name, int row, int col)
                    : base(name, row, col) { }
            }

            public class MiniJavaType : KeywordToken
            {
                public MiniJavaType(string name, int row, int col)
                    : base(name, row, col) { }
            }

            public class BinaryOperator : StringToken
            {
                public BinaryOperator(string symbol, int row, int col)
                    : base(symbol, row, col) { }
            }

            public class UnaryNotToken : Token
            {
                public UnaryNotToken(int row, int col)
                    : base(row, col) { }
            }

            public class RangeOperator : Token
            {
                public RangeOperator(int row, int col)
                    : base(row, col) { }
            }

            public class LeftParenthesis : Token
            {
                public LeftParenthesis(int row, int col)
                    : base(row, col) { }
            }

            public class RightParenthesis : Token
            {
                public RightParenthesis(int row, int col)
                    : base(row, col) { }
            }

            public class EndLine : Token
            {
                public EndLine(int row, int col)
                    : base(row, col) { }
            }

            public class AssignmentToken : Token
            {
                public AssignmentToken(int row, int col)
                    : base(row, col) { }
            }

            public class TypeDeclaration : Token
            {
                public TypeDeclaration(int row, int col)
                    : base(row, col) { }
            }

            public class LeftBracket : Token
            {
                public LeftBracket(int row, int col)
                    : base(row, col) { }
            }

            public class RightBracket : Token
            {
                public RightBracket(int row, int col)
                    : base(row, col) { }
            }

            public class MethodInvocationToken : Token
            {
                public MethodInvocationToken(int row, int col)
                    : base(row, col) { }
            }

            public class EOF : Token
            {
                public EOF(int row, int col) : base(row, col) { }
            }
        }
    }
}