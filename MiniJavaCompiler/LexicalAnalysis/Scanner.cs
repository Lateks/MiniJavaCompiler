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
        private static readonly char[]
            Symbols = new [] { ';', '(', ')', '[', ']', '.', '{', '}', ',' },
            SingleCharOperatorSymbols = new [] { '/', '+', '-', '*', '<', '>', '%', '!' },
            MultiCharOperatorSymbols = new [] { '&', '=', '|' };

        private static readonly string[]
            Keywords = new [] { "this", "true", "false", "new",
                                                        "length", "System", "out", "println",
                                                        "if", "else", "while", "return", "assert",
                                                        "public", "static", "main", "class", "extends" },
            Types = new [] { "int", "boolean", "void" };

        private readonly ScannerInputReader _input;
        private readonly Queue<IToken> _tokens;
        private int _startRow;
        private int _startCol;
        private bool _endOfFileReached;

        public MiniJavaScanner(TextReader input)
        {
            _input = new ScannerInputReader(input);
            _tokens = new Queue<IToken>();
            BuildTokenList();
            _endOfFileReached = false;
        }

        public IToken NextToken()
        {
            if (_endOfFileReached)
            {
                throw new OutOfInput("Reached end of file while parsing.");
            }
            if (_tokens.Count > 0)
                return _tokens.Dequeue();
            _endOfFileReached = true;
            return new EndOfFile(_input.Row, _input.Col);
        }

        // Passes through the code once and builds a queue of tokens.
        private void BuildTokenList()
        {
            IToken token;
            do
            {
                token = MatchNextToken();
                _tokens.Enqueue(token);
            } while (!(token is EndOfFile));
        }

        private IToken MatchNextToken()
        {
            try
            {
                _input.SkipWhiteSpaceAndComments();
            }
            catch (EndlessCommentError e)
            {
                return new ErrorToken("", e.Message, e.Row, e.Col);
            }
            if (!_input.InputLeft())
                return new EndOfFile(_input.Row, _input.Col);

            _startRow = _input.Row;     // store starting row and column for the token object 
            _startCol = _input.Col + 1;

            char inputSymbol = _input.Peek();
            if (SingleCharOperatorSymbols.Contains(inputSymbol))
                return MakeSingleCharBinaryOperatorToken();
            else if (MultiCharOperatorSymbols.Contains(inputSymbol))
                return MakeAssignmentOrMultiCharOperatorToken();
            else if (Symbols.Contains(inputSymbol))
                return MakeSymbolToken();
            else if (Char.IsDigit(inputSymbol))
                return MakeIntegerLiteralToken();
            else if (Char.IsLetter(inputSymbol))
                return MakeIdentifierOrKeywordToken();
            else
            {
                string token = _input.Read();
                return new ErrorToken(token, "Invalid token \"" + token + ".",
                    _startRow, _startCol);
            }
        }

        private IToken MakeSingleCharBinaryOperatorToken()
        {
            return new OperatorToken(_input.Read(), _startRow, _startCol);
        }

        private IToken MakeAssignmentOrMultiCharOperatorToken()
        {
            string symbol = _input.Read();
            if (_input.InputLeft() && _input.Peek().ToString().Equals(symbol))
            {
                symbol += _input.Read();
                return new OperatorToken(symbol, _startRow, _startCol);
            }
            else if (symbol.Equals("="))
                return new OperatorToken(symbol, _startRow, _startCol);
            else
                return new ErrorToken(symbol, "Unexpected token " + symbol + ".",
                    _startRow, _startCol);
        }

        private IToken MakeSymbolToken()
        {
            string token = _input.Read();
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
            while (_input.InputLeft() && Char.IsDigit(_input.Peek()))
                token += _input.Read();
            return new IntegerLiteralToken(token, _startRow, _startCol);
        }

        private IToken MakeIdentifierOrKeywordToken()
        {
            string token = "";
            while (_input.InputLeft() && (Char.IsLetterOrDigit(_input.Peek()) ||
                                         _input.Peek().Equals('_')))
                token += _input.Read();
            if (Types.Contains(token))
                return new MiniJavaType(token, _startRow, _startCol);
            if (Keywords.Contains(token))
                return new KeywordToken(token, _startRow, _startCol);
            return new Identifier(token, _startRow, _startCol);
        }
    }
}