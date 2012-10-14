using System;
using System.Collections.Generic;
using System.Linq;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.LexicalAnalysis;

namespace MiniJavaCompiler.SyntaxAnalysis
{
    // A sub-parser that parses expressions and solves operator precedences.
    // Precedences are solved using an implementation of the Shunting Yard
    // algorithm with the stack structure encoded into the recursive descent
    // parser (no explicit stack).
    internal class ExpressionParser : ParserBase
    {
        private readonly string[][] _operatorsByLevel = new []
            {
                new [] { "||" },                                           
                new [] { "&&" },
                new [] { "==" },
                new [] { "<", ">" },
                new [] { "+", "-" },
                new [] { "*", "/", "%" }, 
            };

        public ExpressionParser(IParserInputReader input, IErrorReporter reporter, bool debugMode = false)
            : base(input, reporter, debugMode) { }

        public IExpression Parse()
        {
            try
            {
                return ParseExpression();
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                ParsingFailed = true;
                RecoverFromExpressionParsing();
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                ParsingFailed = true;
                RecoverFromExpressionParsing();
            }
            return null;
        }

        private List<IExpression> ExpressionList()
        {
            var parser = new CommaSeparatedListParser(Input, ErrorReporter);
            return parser.ParseList<IExpression, PunctuationToken>(Parse, ")");
        }

        private void RecoverFromExpressionParsing()
        {
            while (!(Input.NextTokenIs<EndOfFile>()
                || Input.NextTokenIs<PunctuationToken>(")")
                || Input.NextTokenIs<PunctuationToken>(";")))
                Input.Consume<IToken>();
        }

        private IExpression ParseExpression()
        {
            return ParseBinaryOpExpression(0);
        }

        private IExpression ParseBinaryOpExpression(int precedenceLevel)
        {
            var parseOperand = GetParserFunction(precedenceLevel);
            return ParseBinaryOpTail(parseOperand(), parseOperand, _operatorsByLevel[precedenceLevel]);
        }

        private Func<IExpression> GetParserFunction(int precedenceLevel)
        {
            if (precedenceLevel == _operatorsByLevel.Count() - 1)
            {
                return MultiplicationOperand;
            }
            else
            {
                return () => ParseBinaryOpExpression(precedenceLevel + 1);
            }
        }

        private IExpression MultiplicationOperand()
        {
            if (Input.NextTokenOneOf<OperatorToken>(MiniJavaInfo.UnaryOperators))
            {
                var token = Input.Consume<OperatorToken>();
                var term = Term();
                return new UnaryOperatorExpression(token.Value, term, token.Row, token.Col);
            }
            else
                return Term();
        }

        // A general purpose helper function for parsing the tail of a
        // binary operation (the operator and the right hand term).
        // Continues parsing binary operations on the same precedence level
        // recursively.
        private IExpression ParseBinaryOpTail(IExpression leftHandOperand,
            Func<IExpression> parseRightHandSideOperand, params string[] operators)
        {
            if (Input.NextTokenOneOf<OperatorToken>(operators))
            {
                var opToken = Input.Consume<OperatorToken>();
                var rhs = parseRightHandSideOperand();
                var operatorExp = new BinaryOpExpression(opToken.Value, leftHandOperand, rhs, opToken.Row, opToken.Col);
                return ParseBinaryOpTail(operatorExp, parseRightHandSideOperand, operators);
            }
            else
                return leftHandOperand;
        }

        public IExpression Term()
        {
            try
            {
                if (Input.NextTokenIs<KeywordToken>())
                    return MakeKeywordExpression();
                else if (Input.NextTokenIs<IdentifierToken>())
                    return MakeVariableReferenceExpression();
                else if (Input.NextTokenIs<IntegerLiteralToken>())
                    return MakeIntegerLiteralExpression();
                else if (Input.NextTokenIs<PunctuationToken>("("))
                    return MakeParenthesisedExpression();
                else
                {
                    var token = Input.Consume<IToken>();
                    throw new SyntaxError(String.Format("Invalid start token {0} for a term in an expression.",
                        token is StringToken ? (token as StringToken).Value : token.GetType().Name), token.Row, token.Col);
                }
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                ParsingFailed = true;
                return RecoverFromTermMatching();
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                ParsingFailed = true;
                return RecoverFromTermMatching();
            }
        }

        private IExpression RecoverFromTermMatching()
        { // TODO: could be parameterised on follow set
            while (!(Input.NextTokenIs<EndOfFile>()
                || Input.NextTokenIs<PunctuationToken>(")")
                || Input.NextTokenIs<PunctuationToken>(";")
                || Input.NextTokenIs<OperatorToken>()))
                Input.Consume<IToken>();
            return null;
        }

        private IExpression MakeParenthesisedExpression()
        {
            Input.MatchAndConsume<PunctuationToken>("(");
            IExpression parenthesisedExpression = Parse();
            Input.MatchAndConsume<PunctuationToken>(")");
            return OptionalTermTail(parenthesisedExpression);
        }

        private IExpression MakeIntegerLiteralExpression()
        {
            var token = Input.MatchAndConsume<IntegerLiteralToken>();
            return OptionalTermTail(new IntegerLiteralExpression(token.Value,
                token.Row, token.Col));
        }

        private IExpression MakeVariableReferenceExpression()
        {
            var identifier = Input.MatchAndConsume<IdentifierToken>();
            return OptionalTermTail(new VariableReferenceExpression(
                identifier.Value, identifier.Row, identifier.Col));
        }

        // Similarly to keyword statements, keyword expressions are expressions
        // that start with a keyword.
        private IExpression MakeKeywordExpression()
        {
            var token = (KeywordToken)Input.Peek();
            switch (token.Value)
            {
                case "new":
                    return MakeInstanceCreationExpression();
                case "this":
                    return MakeThisExpression();
                case "true":
                    return MakeBooleanLiteral(true);
                case "false":
                    return MakeBooleanLiteral(false);
                default:
                    throw new SyntaxError("Invalid start token " + token.Value +
                        " for an expression.", token.Row, token.Col);
            }
        }

        private IExpression MakeBooleanLiteral(bool value)
        {
            var boolToken = Input.Consume<KeywordToken>();
            return OptionalTermTail(new BooleanLiteralExpression(value,
                boolToken.Row, boolToken.Col));
        }

        private IExpression MakeThisExpression()
        {
            var thisToken = Input.Consume<KeywordToken>();
            return OptionalTermTail(new ThisExpression(thisToken.Row,
                thisToken.Col));
        }

        private IExpression MakeInstanceCreationExpression()
        {
            var newToken = Input.Consume<KeywordToken>();
            var typeInfo = NewType();
            var type = (StringToken)typeInfo.Item1;
            return OptionalTermTail(new InstanceCreationExpression(type.Value,
                newToken.Row, newToken.Col, typeInfo.Item2));
        }

        public IExpression OptionalTermTail(IExpression lhs)
        {
            if (Input.NextTokenIs<PunctuationToken>("["))
                return MakeArrayIndexingExpression(lhs);
            else if (Input.NextTokenIs<PunctuationToken>("."))
                return MakeMethodInvocationExpression(lhs);
            else
                return lhs;
        }

        private IExpression MakeMethodInvocationExpression(IExpression methodOwner)
        {
            Input.MatchAndConsume<PunctuationToken>(".");
            if (Input.NextTokenIs<KeywordToken>("length"))
                return MakeLengthMethodInvocation(methodOwner);
            else
                return MakeUserDefinedMethodInvocation(methodOwner);
        }

        private IExpression MakeUserDefinedMethodInvocation(IExpression methodOwner)
        {
            StringToken methodName;
            if (Input.NextTokenIs<KeywordToken>("main"))
            {
                methodName = Input.MatchAndConsume<KeywordToken>();
            }
            else
            {
                methodName = Input.MatchAndConsume<IdentifierToken>();
            }
            Input.MatchAndConsume<PunctuationToken>("(");
            var parameters = ExpressionList();
            Input.MatchAndConsume<PunctuationToken>(")");
            return OptionalTermTail(new MethodInvocation(methodOwner,
                methodName.Value, parameters, methodName.Row, methodName.Col));
        }

        private IExpression MakeLengthMethodInvocation(IExpression methodOwner)
        {
            var methodName = Input.MatchAndConsume<KeywordToken>("length");
            var parameters = new List<IExpression>();
            return OptionalTermTail(new MethodInvocation(methodOwner, methodName.Value,
                parameters, methodName.Row, methodName.Col));
        }

        private IExpression MakeArrayIndexingExpression(IExpression lhs)
        {
            var startToken = Input.MatchAndConsume<PunctuationToken>("[");
            var indexExpression = Parse();
            Input.MatchAndConsume<PunctuationToken>("]");
            return OptionalTermTail(new ArrayIndexingExpression(lhs, indexExpression,
                startToken.Row, startToken.Col));
        }

        public Tuple<ITypeToken, IExpression> NewType()
        {
            var type = Input.MatchAndConsume<ITypeToken>();
            if (type is MiniJavaTypeToken || !(Input.NextTokenIs<PunctuationToken>("(")))
            { // must be an array
                Input.MatchAndConsume<PunctuationToken>("[");
                var arraySize = Parse();
                Input.MatchAndConsume<PunctuationToken>("]");
                return new Tuple<ITypeToken, IExpression>(type, arraySize);
            }
            else
            {
                Input.MatchAndConsume<PunctuationToken>("(");
                Input.MatchAndConsume<PunctuationToken>(")");
                return new Tuple<ITypeToken, IExpression>(type, null);
            }
        }
    }
}
