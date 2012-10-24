using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;

namespace MiniJavaCompiler.Frontend.SyntaxAnalysis
{
    // A sub-parser that parses expressions and solves operator precedences.
    // The grammar has a separate level for each operator precedence level.
    public class ExpressionParser : ParserBase
    {
        private readonly int _maxPrecedenceLevel;

        public ExpressionParser(IParserInputReader input, IErrorReporter reporter, bool debugMode = false)
            : base(input, reporter, debugMode)
        {
            _maxPrecedenceLevel = MiniJavaInfo.MaxPrecedenceLevel();
        }

        public IExpression Parse()
        {
            return ParseExpression(); // All syntax and lexical errors should be caught on the term level, so they should not propagate this far.
        }

        private IExpression ParseExpression()
        {
            return ParseBinaryOpExpression(0);
        }

        private IExpression ParseBinaryOpExpression(int precedenceLevel)
        {
            Debug.Assert(precedenceLevel >= 0 && precedenceLevel <= _maxPrecedenceLevel);

            var parseOperand = GetParserFunction(precedenceLevel);
            return ParseBinaryOpTail(parseOperand(), parseOperand, MiniJavaInfo.GetOperatorsForPrecedenceLevel(precedenceLevel));
        }

        private Func<IExpression> GetParserFunction(int precedenceLevel)
        {
            Debug.Assert(precedenceLevel >= 0 && precedenceLevel <= _maxPrecedenceLevel);
            if (precedenceLevel == _maxPrecedenceLevel)
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
            if (Input.NextTokenOneOf<OperatorToken>(MiniJavaInfo.UnaryOperatorSymbols()))
            {
                var token = Input.Consume<OperatorToken>();
                var term = Term();
                return new UnaryOperatorExpression(token.Lexeme, term, token.Row, token.Col);
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
                var rightHandOperand = parseRightHandSideOperand();
                var operatorExp = new BinaryOpExpression(opToken.Lexeme, leftHandOperand, rightHandOperand, opToken.Row, opToken.Col);
                return ParseBinaryOpTail(operatorExp, parseRightHandSideOperand, operators);
            }
            else
                return leftHandOperand;
        }

        private IExpression Term()
        {
            try
            {
                if (Input.NextTokenIs<KeywordToken>())
                {
                    return MakeKeywordExpression();
                }
                else if (Input.NextTokenIs<IdentifierToken>())
                {
                    return MakeVariableReferenceExpression();
                }
                else if (Input.NextTokenIs<IntegerLiteralToken>())
                {
                    return MakeIntegerLiteralExpression();
                }
                else if (Input.NextTokenIs<PunctuationToken>("("))
                {
                    return MakeParenthesisedExpression();
                }
                else
                {
                    var token = Input.Consume<IToken>();
                    var errorMessage = "";
                    if (token is ErrorToken)
                    {
                        errorMessage = "Encountered a lexical error while parsing an expression.";
                    }
                    else if (token is EndOfFile)
                    {
                        errorMessage = "Reached end of file while parsing an expression.";
                    }
                    else
                    {
                        errorMessage = String.Format("Invalid start token '{0}' of type {1} for an expression.",
                            token.Lexeme, TokenDescriptions.Describe(token.GetType()));
                    }
                    throw new SyntaxError(errorMessage, token.Row, token.Col);
                }
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
            }
            catch (LexicalError)
            {
                if (DebugMode) throw;
            }
            ParsingFailed = true;
            RecoverFromTermParsing();
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
            return OptionalTermTail(new IntegerLiteralExpression(token.Lexeme,
                token.Row, token.Col));
        }

        private IExpression MakeVariableReferenceExpression()
        {
            var identifier = Input.MatchAndConsume<IdentifierToken>();
            return OptionalTermTail(new VariableReferenceExpression(
                identifier.Lexeme, identifier.Row, identifier.Col));
        }

        // Similarly to keyword statements, keyword expressions are expressions
        // that start with a keyword.
        private IExpression MakeKeywordExpression()
        {
            var token = (KeywordToken)Input.Peek();
            switch (token.Lexeme)
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
                    throw new SyntaxError(String.Format("Invalid keyword '{0}' starting an expression.",
                        token.Lexeme, TokenDescriptions.Describe(token.GetType())), token.Row, token.Col);
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
            var thisToken = Input.MatchAndConsume<KeywordToken>("this");
            return OptionalTermTail(new ThisExpression(thisToken.Row,
                thisToken.Col));
        }

        private IExpression MakeInstanceCreationExpression()
        {
            var newToken = Input.MatchAndConsume<KeywordToken>("new");
            var typeInfo = NewType();
            var type = typeInfo.typeToken;
            return OptionalTermTail(new InstanceCreationExpression(type.Lexeme,
                newToken.Row, newToken.Col, typeInfo.arraySize));
        }

        private IExpression OptionalTermTail(IExpression lhs)
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
            IToken methodName;
            if (Input.NextTokenIs<KeywordToken>("main"))
            {
                methodName = Input.MatchAndConsume<KeywordToken>();
            }
            else
            {
                methodName = Input.MatchAndConsume<IdentifierToken>();
            }
            Input.MatchAndConsume<PunctuationToken>("(");
            var parameters = MethodInvocationArguments();
            Input.MatchAndConsume<PunctuationToken>(")");
            return OptionalTermTail(new MethodInvocation(methodOwner,
                methodName.Lexeme, parameters, methodName.Row, methodName.Col));
        }

        private List<IExpression> MethodInvocationArguments()
        {
            var parser = new CommaSeparatedListParser(Input, ErrorReporter);
            return parser.ParseList<IExpression, PunctuationToken>(ParseExpression, ")");
        }

        // The length field in Array class is treated as a method invocation in the syntax tree
        // since there are no other public fields in Mini-Java, so the distinction would not
        // be very meaningful.
        private IExpression MakeLengthMethodInvocation(IExpression methodOwner)
        {
            var methodName = Input.MatchAndConsume<KeywordToken>("length");
            var parameters = new List<IExpression>();
            return OptionalTermTail(new MethodInvocation(methodOwner, methodName.Lexeme,
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

        private TypeData NewType()
        {
            var type = Input.MatchAndConsume<ITypeToken>();
            if (type is MiniJavaTypeToken || Input.NextTokenIs<PunctuationToken>("["))
            {
                Input.MatchAndConsume<PunctuationToken>("[");
                var arraySize = Parse();
                Input.MatchAndConsume<PunctuationToken>("]");
                return new TypeData()
                {
                    typeToken = type,
                    isArray = true,
                    arraySize = arraySize
                };
            }
            else
            {
                Input.MatchAndConsume<PunctuationToken>("(");
                Input.MatchAndConsume<PunctuationToken>(")");
                return new TypeData()
                {
                    typeToken = type,
                    isArray = false,
                    arraySize = null
                };
            }
        }

        // Recovery routines

        // This is not a very efficient recovery routine since the whole follow set for expression terms is rather large.
        //
        // E.g. the brackets are slightly problematic here because they could either be a part of the term
        // itself (if the term contains an array type) or they could be a part of the term tail (in which case they
        // do belong in the follow set). This may cause a slight cascade of errors in some cases.
        private void RecoverFromTermParsing()
        {
            while (!(Input.NextTokenIs<EndOfFile>()
                     || Input.NextTokenOneOf<PunctuationToken>(")", ";", ".", ",", "[", "]")
                     || Input.NextTokenIs<OperatorToken>()))
                Input.Consume<IToken>();
        }
    }
}
