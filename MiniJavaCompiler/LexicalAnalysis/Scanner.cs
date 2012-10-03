using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace MiniJavaCompiler.LexicalAnalysis
{
    public class OutOfInput : Exception
    {
        public OutOfInput() { }

        public OutOfInput(string message) : base(message) { }
    }

    public interface IScanner
    {
        IToken NextToken();
    }

    public class MiniJavaScanner : IScanner
    {
        private static readonly HashSet<char>
            Symbols = new HashSet<char>(new char[] { ';', '(', ')', '[', ']', '.', '{', '}', ',' }),
            SingleCharOperators = new HashSet<char>(new char[] { '/', '+', '-', '*', '<', '>', '!', '%' }),
            MultiCharOperatorSymbols = new HashSet<char>(new char[] { '&', '=', '|' });

        private static readonly HashSet<string>
            Keywords = new HashSet<string>(new string[] { "this", "true", "false", "new",
                                                        "length", "System", "out", "println",
                                                        "if", "else", "while", "return", "assert",
                                                        "public", "static", "main", "class", "extends" }),
            Types = new HashSet<string>(new string[] { "int", "boolean", "void" });

        private readonly ScannerInputReader input;
        private readonly Queue<IToken> tokens;
        private int _startRow;
        private int _startCol;
        private bool _endOfFileReached;

        public MiniJavaScanner(TextReader input)
        {
            this.input = new ScannerInputReader(input);
            tokens = new Queue<IToken>();
            BuildTokenList();
            _endOfFileReached = false;
        }

        public IToken NextToken()
        {
            if (_endOfFileReached)
            {
                throw new OutOfInput("Reached end of file while parsing.");
            }
            if (tokens.Count > 0)
                return tokens.Dequeue();
            _endOfFileReached = true;
            return new EndOfFile(input.Row, input.Col);
        }

        // Passes through the code once and builds a queue of tokens.
        private void BuildTokenList()
        {
            IToken token;
            do
            {
                token = MatchNextToken();
                tokens.Enqueue(token);
            } while (!(token is EndOfFile));
        }

        private IToken MatchNextToken()
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
                return new EndOfFile(input.Row, input.Col);

            _startRow = input.Row;     // store starting row and column for the token object 
            _startCol = input.Col + 1;

            char inputChar = input.Peek();
            if (SingleCharOperators.Contains(inputChar))
                return MakeSingleCharOperatorToken();
            else if (MultiCharOperatorSymbols.Contains(inputChar))
                return MakeAssignmentOrMultiCharOperatorToken();
            else if (Symbols.Contains(inputChar))
                return MakeSymbolToken();
            else if (Char.IsDigit(inputChar))
                return MakeIntegerLiteralToken();
            else if (Char.IsLetter(inputChar))
                return MakeIdentifierOrKeywordToken();
            else
            {
                string token = input.Read();
                return new ErrorToken(token, "Invalid token \"" + token + ".",
                    _startRow, _startCol);
            }
        }

        private IToken MakeSingleCharOperatorToken()
        {
            if (input.Peek().Equals('!'))
            {
                input.Read();
                return new UnaryNotToken(_startRow, _startCol);
            }
            else
                return new BinaryOperatorToken(input.Read(), _startRow, _startCol);
        }

        private IToken MakeAssignmentOrMultiCharOperatorToken()
        {
            string symbol = input.Read();
            if (input.InputLeft() && input.Peek().ToString().Equals(symbol))
            {
                symbol += input.Read();
                return new BinaryOperatorToken(symbol, _startRow, _startCol);
            }
            else if (symbol.Equals("="))
                return new AssignmentToken(_startRow, _startCol);
            else
                return new ErrorToken(symbol, "Unexpected token " + symbol + ".",
                    _startRow, _startCol);
        }

        private IToken MakeSymbolToken()
        {
            string token = input.Read();
            switch (token)
            {
                case "(":
                    return new LeftParenthesis(_startRow, _startCol);
                case ")":
                    return new RightParenthesis(_startRow, _startCol);
                case "[":
                    return new LeftBracket(_startRow, _startCol);
                case "]":
                    return new RightBracket(_startRow, _startCol);
                case "{":
                    return new LeftCurlyBrace(_startRow, _startCol);
                case "}":
                    return new RightCurlyBrace(_startRow, _startCol);
                case ".":
                    return new MethodInvocationToken(_startRow, _startCol);
                case ",":
                    return new ParameterSeparator(_startRow, _startCol);
                default:
                    return new EndLine(_startRow, _startCol);
            }
        }

        private IntegerLiteralToken MakeIntegerLiteralToken()
        {
            string token = "";
            while (input.InputLeft() && Char.IsDigit(input.Peek()))
                token += input.Read();
            return new IntegerLiteralToken(token, _startRow, _startCol);
        }

        private IToken MakeIdentifierOrKeywordToken()
        {
            string token = "";
            while (input.InputLeft() && (Char.IsLetterOrDigit(input.Peek()) ||
                                         input.Peek().Equals('_')))
                token += input.Read();
            if (Types.Contains(token))
                return new MiniJavaType(token, _startRow, _startCol);
            if (Keywords.Contains(token))
                return new KeywordToken(token, _startRow, _startCol);
            return new Identifier(token, _startRow, _startCol);
        }
    }
}