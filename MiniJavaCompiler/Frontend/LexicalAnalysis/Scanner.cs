using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.Frontend.LexicalAnalysis
{
    public interface ITokenizer
    {
        IToken NextToken();
    }

    // This exception is thrown by the scanner if NextToken is called after
    // end of file.
    public class OutOfInput : Exception
    {
        public OutOfInput(string message) : base(message) { }
    }

    public class MiniJavaScanner : ITokenizer
    {
        private readonly ScannerInputReader _input;
        private readonly Queue<IToken> _tokens;
        private int _startRow; // the starting row of the current token
        private int _startCol; // the starting column of the current token

        public MiniJavaScanner(TextReader input)
        {
            _input = new ScannerInputReader(input);
            _tokens = new Queue<IToken>();
            BuildTokenQueue(); // The token queue is built right up front. This ensures
                               // that no one can close the TextReader before we are done.
        }

        /* Note: the parser outputs an error token whenever an unexpected token is encountered
         * (such as an endless comment or a character that is not allowed).
         *
         * If the method is called after end of file, an exception is thrown.
         */
        public IToken NextToken()
        {
            if (_tokens.Count > 0)
                return _tokens.Dequeue();
            throw new OutOfInput("Reached end of file while parsing.");
        }

        /* Passes through the code once and builds a queue of tokens.
         * This could also be done lazily so that the next token is produced
         * every time NextToken is called.
         * 
         * In this case, the Scanner would preferably need to manage the
         * TextReader on its own so it would never be closed before the
         * whole program has been passed through.
         */
        private void BuildTokenQueue()
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
            if (MiniJavaInfo.IsSingleCharOperatorSymbol(inputSymbol))
            {
                return MakeSingleCharBinaryOperatorToken();
            }
            else if (MiniJavaInfo.IsMultiCharOperatorSymbol(inputSymbol))
            {
                return MakeMultiCharOperatorOrAssignmentToken();
            }
            else if (MiniJavaInfo.IsPunctuationCharacter(inputSymbol))
            {
                return MakePunctuationToken();
            }
            else if (Char.IsDigit(inputSymbol))
            {
                return MakeIntegerLiteralToken();
            }
            else if (Char.IsLetter(inputSymbol)) // Identifiers always start with a letter.
            {
                return MakeIdentifierOrKeywordToken();
            }
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

        private IToken MakeMultiCharOperatorOrAssignmentToken()
        {
            string symbol = _input.Read();
            if (_input.InputLeft() && _input.Peek().ToString() == symbol) // All two-character operators in Mini-Java consist of the same character twice.
            {
                symbol += _input.Read();
                return new OperatorToken(symbol, _startRow, _startCol);
            }
            else if (symbol.Equals("=")) // An assignment operator.
                return new OperatorToken(symbol, _startRow, _startCol);
            else
                return new ErrorToken(symbol, String.Format("Unexpected token '{0}'", symbol),
                    _startRow, _startCol);
        }

        private IToken MakePunctuationToken()
        {
            return new PunctuationToken(_input.Read(), _startRow, _startCol);
        }

        private IntegerLiteralToken MakeIntegerLiteralToken()
        {
            var token = new StringBuilder();
            while (_input.InputLeft() && Char.IsDigit(_input.Peek()))
                token.Append(_input.Read());
            return new IntegerLiteralToken(token.ToString(), _startRow, _startCol);
        }

        private IToken MakeIdentifierOrKeywordToken()
        {
            var tokenBuilder = new StringBuilder();
            while (_input.InputLeft() && (Char.IsLetterOrDigit(_input.Peek()) ||
                                         _input.Peek().Equals('_')))
                tokenBuilder.Append(_input.Read());
            var token = tokenBuilder.ToString();

            if (MiniJavaInfo.IsTypeKeyword(token))
                return new MiniJavaTypeToken(token, _startRow, _startCol);
            if (MiniJavaInfo.IsKeyword(token))
                return new KeywordToken(token, _startRow, _startCol);
            return new IdentifierToken(token, _startRow, _startCol);
        }
    }
}