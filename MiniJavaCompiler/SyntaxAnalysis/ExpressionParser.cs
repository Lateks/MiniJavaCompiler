using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly string[][] operatorsByLevel = new []
            {
                new [] { "||" },                                           
                new [] { "&&" },
                new [] { "==" },
                new [] { "<", ">" },
                new [] { "+", "-" },
                new [] { "*", "/", "%" }, 
            };

        public ExpressionParser(IParserInputReader input, IErrorReporter reporter)
            : base(input, reporter) { }

        public new IExpression Parse()
        {
            try
            {
                return ParseExpression();
            }
            catch (SyntaxError e)
            {
                errorReporter.ReportError(e.Message, e.Row, e.Col);
                RecoverFromExpressionParsing();
            }
            catch (LexicalErrorEncountered)
            {
                RecoverFromExpressionParsing();
            }
            return null;
        }

        private List<IExpression> ExpressionList()
        {
            var parser = new CommaSeparatedListParser(Input, errorReporter);
            return parser.ParseList<IExpression, RightParenthesis>(Parse);
        }

        private void RecoverFromExpressionParsing()
        {
            while (!(Input.NextTokenIs<EndOfFile>()
                || Input.NextTokenIs<RightParenthesis>()
                || Input.NextTokenIs<EndLine>()))
                Input.Consume<IToken>();
        }

        private IExpression ParseExpression()
        {
            return ParseBinaryOpExpression(0);
        }

        private IExpression ParseBinaryOpExpression(int precedenceLevel)
        {
            var parseOperand = GetParserFunction(precedenceLevel);
            return ParseBinaryOpTail(parseOperand(), parseOperand, operatorsByLevel[precedenceLevel]);
        }

        private Func<IExpression> GetParserFunction(int precedenceLevel)
        {
            if (precedenceLevel == operatorsByLevel.Count() - 1)
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
            if (Input.NextTokenIs<UnaryNotToken>())
            {
                var token = Input.Consume<UnaryNotToken>();
                var term = Term();
                return new UnaryNotExpression(term, token.Row, token.Col);
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
            if (Input.NextTokenOneOf<BinaryOperatorToken>(operators))
            {
                var opToken = Input.Consume<BinaryOperatorToken>();
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
                else if (Input.NextTokenIs<Identifier>())
                    return MakeVariableReferenceExpression();
                else if (Input.NextTokenIs<IntegerLiteralToken>())
                    return MakeIntegerLiteralExpression();
                else if (Input.NextTokenIs<LeftParenthesis>())
                    return MakeParenthesisedExpression();
                else
                {
                    var token = Input.Consume<IToken>();
                    throw new SyntaxError("Invalid start token of type " +
                        token.GetType().Name + " for a term in an expression.",
                        token.Row, token.Col);
                }
            }
            catch (SyntaxError e)
            {
                errorReporter.ReportError(e.Message, e.Row, e.Col);
                return RecoverFromTermMatching();
            }
            catch (LexicalErrorEncountered)
            {
                return RecoverFromTermMatching();
            }
        }

        private IExpression RecoverFromTermMatching()
        { // could be parameterised on follow set
            while (!(Input.NextTokenIs<EndOfFile>()
                || Input.NextTokenIs<RightParenthesis>()
                || Input.NextTokenIs<EndLine>()
                || Input.NextTokenIs<BinaryOperatorToken>()))
                Input.Consume<IToken>();
            return null;
        }

        private IExpression MakeParenthesisedExpression()
        {
            Input.Consume<LeftParenthesis>();
            IExpression parenthesisedExpression = Parse();
            Input.MatchAndConsume<RightParenthesis>();
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
            var identifier = Input.MatchAndConsume<Identifier>();
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
                        " for expression.", token.Row, token.Col);
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
            if (Input.NextTokenIs<LeftBracket>())
                return MakeArrayIndexingExpression(lhs);
            else if (Input.NextTokenIs<MethodInvocationToken>())
                return MakeMethodInvocationExpression(lhs);
            else
                return lhs;
        }

        private IExpression MakeMethodInvocationExpression(IExpression methodOwner)
        {
            Input.Consume<MethodInvocationToken>();
            if (Input.NextTokenIs<KeywordToken>())
                return MakeLengthMethodInvocation(methodOwner);
            else
                return MakeUserDefinedMethodInvocation(methodOwner);
        }

        private IExpression MakeUserDefinedMethodInvocation(IExpression methodOwner)
        {
            var methodName = Input.MatchAndConsume<Identifier>();
            Input.MatchAndConsume<LeftParenthesis>();
            var parameters = ExpressionList();
            Input.MatchAndConsume<RightParenthesis>();
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
            var startToken = Input.MatchAndConsume<LeftBracket>();
            var indexExpression = Parse();
            Input.MatchAndConsume<RightBracket>();
            return OptionalTermTail(new ArrayIndexingExpression(lhs, indexExpression,
                startToken.Row, startToken.Col));
        }

        public Tuple<ITypeToken, IExpression> NewType()
        {
            var type = Input.MatchAndConsume<ITypeToken>();
            if (type is MiniJavaType || !(Input.NextTokenIs<LeftParenthesis>()))
            { // must be an array
                Input.MatchAndConsume<LeftBracket>();
                var arraySize = Parse();
                Input.MatchAndConsume<RightBracket>();
                return new Tuple<ITypeToken, IExpression>(type, arraySize);
            }
            else
            {
                Input.MatchAndConsume<LeftParenthesis>();
                Input.MatchAndConsume<RightParenthesis>();
                return new Tuple<ITypeToken, IExpression>(type, null);
            }
        }
    }
}
