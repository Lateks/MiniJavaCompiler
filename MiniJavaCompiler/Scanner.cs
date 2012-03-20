using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MiniJavaCompiler.Support.TokenTypes;
using MiniJavaCompiler.Support.Errors.Compilation;

namespace MiniJavaCompiler
{
    namespace LexicalAnalysis
    {
        public class Scanner
        {
            private static char ENDLINE = ';';
            private static char LEFT_PAREN = '(';
            private static char RIGHT_PAREN = ')';
            private static HashSet<char> symbols =
                new HashSet<char>(new char[] { ENDLINE, LEFT_PAREN, RIGHT_PAREN });
            private static HashSet<char> singleCharOperators =
                new HashSet<char>(new char[] { '/', '+', '-', '*', '<', '>', '!', '%' });
            private static HashSet<char> multiCharOperatorSymbols =
                new HashSet<char>(new char[] { '&', '=', '|' });
            private static HashSet<string> keywords =
                new HashSet<string>(new string[] { "this", "true", "false", "new", "length", "System", "out", "println",
                    "if", "else", "while", "return", "assert", "public", "static", "main", "class", "extends" });
            private static HashSet<string> types =
                new HashSet<string>(new string[] { "int", "boolean", "void" });

            private ScannerInputStack input;
            private Queue<Token> tokens;
            private int startRow;
            private int startCol;

            public Scanner(string input)
            {
                this.input = new ScannerInputStack(input);
                tokens = new Queue<Token>();
                BuildTokenList();
            }

            // If called after EOF, will return null.
            public Token NextToken()
            {
                if (tokens.Count > 0)
                    return tokens.Dequeue();
                return null;
            }

            // Passes through the code once and builds a queue of tokens.
            private void BuildTokenList()
            {
                Token token;
                do
                {
                    token = MatchNextToken();
                    tokens.Enqueue(token);
                } while (!(token is EOF));
            }

            private Token MatchNextToken()
            {
                try
                {
                    input.SkipWhiteSpaceAndComments();
                }
                catch (EndlessCommentError e)
                {
                    return new ErrorToken("", e.Message, e.Row, e.Col);
                }
                if (!input.InputLeft())
                    return new EOF(input.Row, input.Col);

                startRow = input.Row;     // store starting row and column for the token object 
                startCol = input.Col + 1;

                if (singleCharOperators.Contains(input.Peek()))
                    return MakeSingleCharOperatorToken();
                else if (multiCharOperatorSymbols.Contains(input.Peek()))
                    return MakeAssignmentOrMultiCharOperatorToken();
                else if (symbols.Contains(input.Peek()))
                    return MakeSymbolToken();
                else if (Char.IsDigit(input.Peek()))
                    return MakeIntegerLiteralToken();
                else if (Char.IsLetter(input.Peek()))
                    return MakeIdentifierOrKeywordToken();
                else
                {
                    string token = input.Pop();
                    return new ErrorToken(token, "Invalid token \"" + token +
                        "\" on row " + startRow + ", col " + startCol + ".",
                        startRow, startCol);
                }
            }

            private Token MakeSingleCharOperatorToken()
            {
                if (input.Peek().Equals('!'))
                {
                    input.Pop();
                    return new UnaryNotToken(startRow, startCol);
                }
                return new BinaryOperator(input.Pop(), startRow, startCol);
            }

            private Token MakeAssignmentOrMultiCharOperatorToken()
            {
                string symbol = input.Pop();
                if (input.InputLeft() && input.Peek().ToString().Equals(symbol))
                {
                    symbol += input.Pop();
                    return new BinaryOperator(symbol, startRow, startCol);
                }
                else if (symbol.Equals("="))
                    return new AssignmentToken(startRow, startCol);
                else
                    return new ErrorToken(symbol, "Unexpected token " + symbol +
                        " on row " + startRow + " col " + startCol + ".", startRow,
                        startCol);
            }

            private Token MakeSymbolToken()
            {
                string token = input.Pop();
                if (token.Equals(ENDLINE.ToString()))
                    return new EndLine(startRow, startCol);
                else if (token.Equals(LEFT_PAREN.ToString()))
                    return new LeftParenthesis(startRow, startCol);
                else
                    return new RightParenthesis(startRow, startCol);
            }

            private IntegerLiteralToken MakeIntegerLiteralToken()
            {
                string token = "";
                while (input.InputLeft() && Char.IsDigit(input.Peek()))
                    token += input.Pop();
                return new IntegerLiteralToken(token, startRow, startCol);
            }

            private Token MakeIdentifierOrKeywordToken()
            {
                string token = "";
                while (input.InputLeft() && (Char.IsLetterOrDigit(input.Peek()) ||
                                             input.Peek().Equals('_')))
                    token += input.Pop();
                if (types.Contains(token))
                    return new MiniJavaType(token, startRow, startCol);
                if (keywords.Contains(token))
                    return new KeywordToken(token, startRow, startCol);
                return new Identifier(token, startRow, startCol);
            }
        }
    }
}