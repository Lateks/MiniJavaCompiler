﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.Frontend.LexicalAnalysis
{
    public class OutOfInput : Exception
    {
        public OutOfInput(string message) : base(message) { }
    }

    public interface ITokenizer
    {
        IToken NextToken();
    }

    public class MiniJavaScanner : ITokenizer
    {
        private readonly ScannerInputReader _input;
        private readonly Queue<IToken> _tokens;
        private int _startRow;
        private int _startCol;

        public MiniJavaScanner(TextReader input)
        {
            _input = new ScannerInputReader(input);
            _tokens = new Queue<IToken>();
            BuildTokenList();
        }

        // Note: the parser outputs an error token whenever an unexpected token is encountered
        // (such as an endless comment or a character that is not allowed).
        public IToken NextToken()
        {
            if (_tokens.Count > 0)
                return _tokens.Dequeue();
            throw new OutOfInput("Reached end of file while parsing.");
        }

        // Passes through the code once and builds a queue of tokens.
        // This could also be done lazily so that the next token is matched
        // every time NextToken is called.
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
            if (MiniJavaInfo.SingleCharOperatorSymbols.Contains(inputSymbol))
                return MakeSingleCharBinaryOperatorToken();
            else if (MiniJavaInfo.MultiCharOperatorSymbols.Contains(inputSymbol))
                return MakeMultiCharOperatorToken();
            else if (MiniJavaInfo.Punctuation.Contains(inputSymbol))
                return MakePunctuationToken();
            else if (Char.IsDigit(inputSymbol))
                return MakeIntegerLiteralToken();
            else if (Char.IsLetter(inputSymbol))
                return MakeIdentifierOrKeywordToken();
            else
            {
                string token = _input.Read();
                return new ErrorToken(token, String.Format("Unexpected token '{0}'.", token),
                    _startRow, _startCol);
            }
        }

        private IToken MakeSingleCharBinaryOperatorToken()
        {
            return new OperatorToken(_input.Read(), _startRow, _startCol);
        }

        private IToken MakeMultiCharOperatorToken()
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
                return new ErrorToken(symbol, String.Format("Unexpected token '{0}'", symbol),
                    _startRow, _startCol);
        }

        private IToken MakePunctuationToken()
        {
            string token = _input.Read();
            return new PunctuationToken(token, _startRow, _startCol);
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
            if (MiniJavaInfo.Types.Contains(token))
                return new MiniJavaTypeToken(token, _startRow, _startCol);
            if (MiniJavaInfo.Keywords.Contains(token))
                return new KeywordToken(token, _startRow, _startCol);
            return new IdentifierToken(token, _startRow, _startCol);
        }
    }
}