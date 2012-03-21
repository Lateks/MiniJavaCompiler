using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MiniJavaCompiler.Support.TokenTypes;
using MiniJavaCompiler.Support.Errors.Compilation;
using System.IO;

namespace MiniJavaCompiler
{
    namespace LexicalAnalysis
    {
        public class Scanner
        {
            private static HashSet<char>
                symbols = new HashSet<char>(new char[] { ';', '(', ')', '[', ']', '.' }),
                singleCharOperators = new HashSet<char>(new char[] { '/', '+', '-', '*', '<', '>', '!', '%' }),
                multiCharOperatorSymbols = new HashSet<char>(new char[] { '&', '=', '|' });
            private static HashSet<string>
                keywords = new HashSet<string>(new string[] { "this", "true", "false", "new",
                                                        "length", "System", "out", "println",
                                                        "if", "else", "while", "return", "assert",
                                                        "public", "static", "main", "class", "extends" }),
                types = new HashSet<string>(new string[] { "int", "boolean", "void" });

            private ScannerInputReader input;
            private Queue<Token> tokens;
            private int startRow;
            private int startCol;

            public Scanner(TextReader input)
            {
                this.input = new ScannerInputReader(input);
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

                char inputChar = input.Peek();
                if (singleCharOperators.Contains(inputChar))
                    return MakeSingleCharOperatorToken();
                else if (multiCharOperatorSymbols.Contains(inputChar))
                    return MakeAssignmentOrMultiCharOperatorToken();
                else if (symbols.Contains(inputChar))
                    return MakeSymbolToken();
                else if (Char.IsDigit(inputChar))
                    return MakeIntegerLiteralToken();
                else if (Char.IsLetter(inputChar))
                    return MakeIdentifierOrKeywordToken();
                else
                {
                    string token = input.Read();
                    return new ErrorToken(token, "Invalid token \"" + token +
                        "\" on row " + startRow + ", col " + startCol + ".",
                        startRow, startCol);
                }
            }

            private Token MakeSingleCharOperatorToken()
            {
                if (input.Peek().Equals('!'))
                {
                    input.Read();
                    return new UnaryNotToken(startRow, startCol);
                }
                return new BinaryOperator(input.Read(), startRow, startCol);
            }

            private Token MakeAssignmentOrMultiCharOperatorToken()
            {
                string symbol = input.Read();
                if (input.InputLeft() && input.Peek().ToString().Equals(symbol))
                {
                    symbol += input.Read();
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
                string token = input.Read();
                switch (token) {
                    case "(":
                        return new LeftParenthesis(startRow, startCol);
                    case ")":
                        return new RightParenthesis(startRow, startCol);
                    case "[":
                        return new LeftBracket(startRow, startCol);
                    case "]":
                        return new RightBracket(startRow, startCol);
                    case ".":
                        return new MethodInvocationToken(startRow, startCol);
                    default:
                        return new EndLine(startRow, startCol);
                }
            }

            private IntegerLiteralToken MakeIntegerLiteralToken()
            {
                string token = "";
                while (input.InputLeft() && Char.IsDigit(input.Peek()))
                    token += input.Read();
                return new IntegerLiteralToken(token, startRow, startCol);
            }

            private Token MakeIdentifierOrKeywordToken()
            {
                string token = "";
                while (input.InputLeft() && (Char.IsLetterOrDigit(input.Peek()) ||
                                             input.Peek().Equals('_')))
                    token += input.Read();
                if (types.Contains(token))
                    return new MiniJavaType(token, startRow, startCol);
                if (keywords.Contains(token))
                    return new KeywordToken(token, startRow, startCol);
                return new Identifier(token, startRow, startCol);
            }
        }
    }
}