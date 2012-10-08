using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.SyntaxAnalysis
{
    public interface IParserInputReader
    {
        IToken Peek();
        void PushBack(IToken token);
        TExpectedType MatchAndConsume<TExpectedType>(string expectedValue) where TExpectedType : StringToken;
        TExpectedType MatchAndConsume<TExpectedType>() where TExpectedType : IToken;
        TTokenType Consume<TTokenType>() where TTokenType : IToken;
        bool NextTokenIs<TExpectedType>() where TExpectedType : IToken;
        bool NextTokenIs<TExpectedType>(string expectedValue) where TExpectedType : StringToken;
        bool NextTokenOneOf<TExpectedType>(params string[] valueCollection) where TExpectedType : StringToken;
    }

    internal class LexicalErrorEncountered : Exception { }

    internal class SyntaxError : Exception
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public new string Message
        {
            get { return String.Format("{0} (On row {1}, col {2}.)", base.Message, Row, Col); }
        }

        public SyntaxError(string message, int row, int col)
            : base(message)
        {
            Row = row;
            Col = col;
        }
    }

    internal class ParserInputReader : IParserInputReader
    {
        private readonly IScanner _scanner;
        private readonly Stack<IToken> _inputBuffer; // This stack is used for buffering when we need to peek forward.
        private readonly IErrorReporter _errorReporter;

        private IToken InputToken
        {
            get;
            set;
        }

        public ParserInputReader(IScanner scanner, IErrorReporter errorReporter)
        {
            this._scanner = scanner;
            this._errorReporter = errorReporter;
            this._inputBuffer = new Stack<IToken>();
            InputToken = scanner.NextToken();
        }

        public IToken Peek()
        {
            return InputToken;
        }

        // Pushes an already consumed token back into the input.
        public void PushBack(IToken token)
        {
            _inputBuffer.Push(InputToken);
            InputToken = token;
        }

        // Checks that the input token is of the expected type and matches the
        // expected value. If the input token matches, it is returned and
        // cast to the expected type. Otherwise an error is reported.
        // The token is consumed even when it does not match expectations.
        // 
        // Note: MatchAndConsume always consumes the next token regardless of match
        // failure.
        public TExpectedType MatchAndConsume<TExpectedType>(string expectedValue)
            where TExpectedType : StringToken
        {
            if (InputToken is TExpectedType)
            {
                if (((StringToken)InputToken).Value == expectedValue)
                {
                    return Consume<TExpectedType>();
                }
                else
                {
                    var token = Consume<StringToken>();
                    throw new SyntaxError("Expected value \"" + expectedValue + "\" but got " +
                        token.Value + ".", token.Row, token.Col);
                }
            }
            else
            {
                throw ConstructMatchException<TExpectedType>(Consume<IToken>());
            }
        }

        // Like above but does not check value and accepts all kinds of tokens.
        public TExpectedType MatchAndConsume<TExpectedType>()
            where TExpectedType : IToken
        {
            if (NextTokenIs<TExpectedType>())
                return Consume<TExpectedType>();
            else
            {
                throw ConstructMatchException<TExpectedType>(Consume<IToken>());
            }
        }

        private Exception ConstructMatchException<TExpectedType>(IToken token)
        {
            if (token is ErrorToken)
                return new LexicalErrorEncountered();
            else
                return new SyntaxError("Expected type " + typeof(TExpectedType).Name +
                    " but got " + token.GetType().Name + ".", token.Row, token.Col);
        }

        // Consumes a token from input and returns it after casting to the
        // given type (unless input token is an error token, in which case
        // an ErrorToken is returned regardless of the type parameter).
        //
        // This method should only be called when the input token's type
        // has already been verified to avoid errors in class casting.
        // (Unless consuming tokens as type Token for e.g. recovery purposes.)
        public TTokenType Consume<TTokenType>() where TTokenType : IToken
        {
            var temp = GetTokenOrReportError<TTokenType>();
            InputToken = _inputBuffer.Count > 0 ? _inputBuffer.Pop() : _scanner.NextToken();
            return temp;
        }

        private dynamic GetTokenOrReportError<TTokenType>() where TTokenType : IToken
        {
            if (InputToken is ErrorToken)
            {   // Lexical errors are reported here, so no errors are left unreported
                // when consuming tokens because of recovery.
                var temp = (ErrorToken)InputToken;
                _errorReporter.ReportError(temp.Message, temp.Row, temp.Col);
                return temp;
            }
            else
                return (TTokenType)InputToken;
        }

        // Checks whether the input token matches the expected type and value or not.
        public bool NextTokenIs<TExpectedType>(string expectedValue)
            where TExpectedType : StringToken
        {
            return NextTokenIs<TExpectedType>() && ((StringToken)InputToken).Value == expectedValue;
        }

        public bool NextTokenIs<TExpectedType>()
            where TExpectedType : IToken
        {
            return InputToken is TExpectedType;
        }


        public bool NextTokenOneOf<TExpectedType>(params string[] valueCollection) where TExpectedType : StringToken
        {
            if (!NextTokenIs<TExpectedType>())
                return false;
            else
                return valueCollection.Contains(((StringToken) InputToken).Value);
        }
    }
}