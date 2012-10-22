using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.Frontend.SyntaxAnalysis
{
    public interface IParserInputReader
    {
        // Returns the next token without consuming it.
        IToken Peek();

        // Attempts to peek some steps forward (steps must be a positive number).
        // Will throw an exception if there is not enough input.
        IToken PeekForward(int steps);

        /* Checks that the current input token is of the expected type and matches the expected value.
         * If the check succeeds, the token is consumed and returned. Otherwise a syntax error or a
         * lexical error is thrown.
         */
        TExpectedType MatchAndConsume<TExpectedType>(string expectedValue) where TExpectedType : IToken;

        /* Checks that the current input token is of the expected type. If the check succeeds, the
         * token is consumed and returned. Otherwise a syntax error or a lexical error is thrown.
         */
        TExpectedType MatchAndConsume<TExpectedType>() where TExpectedType : IToken;

        // Consumes the current input token, casts it to the type given as parameter
        // and returns it.
        TTokenType Consume<TTokenType>() where TTokenType : IToken;

        // Checks whether the current input token is of the expected type without consuming it.
        bool NextTokenIs<TExpectedType>() where TExpectedType : IToken;

        // Checks whether the current input token is of the expected type and bears the
        // expected value (lexeme) without consuming it.
        bool NextTokenIs<TExpectedType>(string expectedValue) where TExpectedType : IToken;

        // Checks whether the current input token is of the expected type and matches one
        // of the values given in the parameter list. Does not consume the token.
        bool NextTokenOneOf<TExpectedType>(params string[] valueCollection) where TExpectedType : IToken;
    }

    public class LexicalError : Exception { }

    public class SyntaxError : Exception
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        public SyntaxError(string message, int row, int col)
            : base(message)
        {
            Row = row;
            Col = col;
        }
    }

    // This class handles matching individual tokens, reporting lexical errors and peeking at input.
    public class ParserInputReader : IParserInputReader
    {
        private class TokenStreamBuffer // This class separates token stream handling from the main input reader.
        {
            private readonly ITokenizer _scanner;
            private IToken _inputToken;
            private readonly Queue<IToken> _inputBuffer; // This stack is used for buffering when we need to peek forward.

            public TokenStreamBuffer(ITokenizer scanner)
            {
                _scanner = scanner;
                _inputBuffer = new Queue<IToken>();
                _inputToken = null;
            }

            public IToken CurrentToken
            {
                get
                {
                    RefreshInputToken();
                    return _inputToken;
                }
                private set { _inputToken = value; }
            }

            public void Consume()
            {
                CurrentToken = null; // The next token is not fetched here. We do not want an out of input error e.g. when consuming an EndOfFile.
            }

            // Note: this may throw an OutOfInput exception.
            public IToken PeekForward(int tokens)
            {
                Debug.Assert(tokens >= 0);
                RefreshInputToken();
                if (tokens == 0)
                {
                    return CurrentToken;
                }

                IToken token;
                if (_inputBuffer.Count < tokens)
                {
                    Buffer(tokens - _inputBuffer.Count);
                    token = _inputBuffer.Last();
                }
                else
                {
                    token = _inputBuffer.ElementAt(tokens - 1);
                }
                return token;
            }

            private void Buffer(int tokens)
            {
                while (tokens > 0)
                {
                    _inputBuffer.Enqueue(_scanner.NextToken());
                    tokens--;
                }
            }

            private void RefreshInputToken()
            {
                if (_inputToken == null)
                {
                    _inputToken = _inputBuffer.Count > 0 ? _inputBuffer.Dequeue() : _scanner.NextToken();
                }
            }
        }

        private readonly IErrorReporter _errorReporter;
        private readonly TokenStreamBuffer _tokenStream;

        public ParserInputReader(ITokenizer scanner, IErrorReporter errorReporter)
        {
            _tokenStream = new TokenStreamBuffer(scanner);
            _errorReporter = errorReporter;
        }

        // Throws an exception if called after input is completely consumed.
        public IToken Peek()
        {
            return _tokenStream.CurrentToken;
        }

        public IToken PeekForward(int steps)
        {
            if (steps < 0)
            {
                throw new ArgumentOutOfRangeException("Cannot peek backwards.");
            }
            return _tokenStream.PeekForward(steps);
        }

        public TExpectedType MatchAndConsume<TExpectedType>(string expectedValue)
            where TExpectedType : IToken
        {
            if (_tokenStream.CurrentToken is TExpectedType && _tokenStream.CurrentToken.Lexeme == expectedValue)
            {
                return Consume<TExpectedType>();
            }
            throw ConstructMatchException<TExpectedType>(Consume<IToken>(), expectedValue);
        }

        public TExpectedType MatchAndConsume<TExpectedType>()
            where TExpectedType : IToken
        {
            if (NextTokenIs<TExpectedType>())
            {
                return Consume<TExpectedType>();
            }
            throw ConstructMatchException<TExpectedType>(Consume<IToken>());
        }

        // Consumes a token from input and returns it after casting to the
        // given type.
        //
        // This method should only be called when the input token's type
        // has already been verified to avoid errors in class casting.
        // (Unless consuming tokens as type Token for e.g. recovery purposes.)
        public TTokenType Consume<TTokenType>() where TTokenType : IToken
        {
            var returnToken = GetTokenAndReportErrors();
            _tokenStream.Consume();
            return (TTokenType) returnToken;
        }

        // Checks the type and lexeme of the current input token without consuming it.
        public bool NextTokenIs<TExpectedType>(string expectedValue)
            where TExpectedType : IToken
        {
            return NextTokenIs<TExpectedType>() && _tokenStream.CurrentToken.Lexeme == expectedValue;
        }

        // Checks the type of the current input token without consuming it.
        public bool NextTokenIs<TExpectedType>()
            where TExpectedType : IToken
        {
            return _tokenStream.CurrentToken is TExpectedType;
        }

        // Checks that the next token is of the expected type and matches one of the expected string values.
        // Used to match e.g. a subset of punctuation or operator symbols.
        public bool NextTokenOneOf<TExpectedType>(params string[] valueCollection) where TExpectedType : IToken
        {
            if (!NextTokenIs<TExpectedType>())
                return false;
            else
                return valueCollection.Contains(_tokenStream.CurrentToken.Lexeme);
        }

        private IToken GetTokenAndReportErrors()
        {   // Lexical errors are reported here, so no errors are left unreported
            // when consuming tokens because of recovery.
            if (_tokenStream.CurrentToken is ErrorToken)
            {
                var error = (ErrorToken)_tokenStream.CurrentToken;
                _errorReporter.ReportError(error.Message, error.Row, error.Col);
                return error;
            }
            else
                return _tokenStream.CurrentToken;
        }

        // Constructs an appropriate exception for a token when matching has failed.
        private Exception ConstructMatchException<TExpectedType>(IToken token, string expectedValue = null)
            where TExpectedType : IToken
        {
            if (token is ErrorToken)
            {
                return new LexicalError(); // No message needed: this error has already been reported when the token was consumed,
            }                              // but we probably need to recover.

            var expected = String.IsNullOrEmpty(expectedValue)
                               ? TokenDescriptions.Describe(typeof(TExpectedType))
                               : "'" + expectedValue + "'";
            if (token is EndOfFile)
                return new SyntaxError(String.Format("Reached end of file while parsing for {0}.", expected),
                                       token.Row, token.Col);
            else
            {
                return new SyntaxError(String.Format("Expected {0} but got {1} '{2}'.", expected,
                    TokenDescriptions.Describe(token.GetType()), token.Lexeme),
                    token.Row, token.Col);
            }
        }
    }
}