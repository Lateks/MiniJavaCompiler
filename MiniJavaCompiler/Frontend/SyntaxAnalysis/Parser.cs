using System;
using System.Collections.Generic;
using System.Diagnostics;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;

namespace MiniJavaCompiler.Frontend.SyntaxAnalysis
{
    public interface IParser
    {
        Program Parse();
    }

    public abstract class ParserBase
    {
        public bool DebugMode { get; protected set; }
        public bool ParsingFailed { get; protected set; }
        protected IParserInputReader Input { get; set; }
        protected readonly IErrorReporter ErrorReporter;

        protected ParserBase(IParserInputReader input, IErrorReporter reporter, bool debugMode = false)
        {
            DebugMode = debugMode;
            ErrorReporter = reporter;
            Input = input;
        }

        // Used to return detailed type data from certain parser methods.
        protected struct TypeData
        {
            public ITypeToken typeToken;
            public bool isArray;
            public IExpression arraySize;
        }
    }

    public class Parser : ParserBase, IParser
    {
        public Parser(ITokenizer input, IErrorReporter reporter, bool debugMode = false)
            : base(new ParserInputReader(input, reporter), reporter, debugMode) { }

        public Program Parse()
        {
            var program = Program();
            if (ParsingFailed) // This exception is thrown if either lexical or syntactic errors are found in the token stream.
            {
                throw new CompilationError();
            }
            return program;
        }

        private Program Program()
        {
            MainClassDeclaration main;
            List<ClassDeclaration> declarations;
            try
            {
                main = MainClass();
                declarations = ClassDeclarationList();
            }
            catch (OutOfInput e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, 0, 0);
                throw new CompilationError();
            }

            // Invariant:
            // If end of file has been consumed (in place of a different, expected token),
            // it should have led into recovery that would have ended up in an OutOfInput error.
            // ClassDeclarationList stops parsing at the end of file.
            // => Therefore the next token here should always be EndOfFile.
            Debug.Assert(Input.NextTokenIs<EndOfFile>());
            Input.MatchAndConsume<EndOfFile>();

            return new Program(main, declarations);
        }

        private MainClassDeclaration MainClass()
        {
            try
            {
                IToken startToken = Input.MatchAndConsume<KeywordToken>("class");

                var classIdent = Input.MatchAndConsume<IdentifierToken>();
                Input.MatchAndConsume<PunctuationToken>("{"); // class body begins
                IToken methodStartToken = Input.MatchAndConsume<KeywordToken>("public");
                Input.MatchAndConsume<KeywordToken>("static");
                Input.MatchAndConsume<MiniJavaTypeToken>("void");
                Input.MatchAndConsume<KeywordToken>("main");
                Input.MatchAndConsume<PunctuationToken>("(");
                Input.MatchAndConsume<PunctuationToken>(")");

                Input.MatchAndConsume<PunctuationToken>("{"); // main method body begins
                var mainStatements = StatementList();
                Input.MatchAndConsume<PunctuationToken>("}");

                Input.MatchAndConsume<PunctuationToken>("}"); // class body ends
                return new MainClassDeclaration(classIdent.Lexeme,
                    mainStatements, startToken.Row, startToken.Col,
                    methodStartToken.Row, methodStartToken.Col);
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
            RecoverFromClassMatching();
            return null;
        }

        private ClassDeclaration ClassDeclaration()
        {
            try
            {
                IToken startToken = Input.MatchAndConsume<KeywordToken>("class");
                var classIdent = Input.MatchAndConsume<IdentifierToken>();
                var inheritedClass = OptionalInheritance(); // inherited class can be null

                Input.MatchAndConsume<PunctuationToken>("{"); // class body begins
                var declarations = DeclarationList();
                Input.MatchAndConsume<PunctuationToken>("}");
                return new ClassDeclaration(classIdent.Lexeme, inheritedClass,
                    declarations, startToken.Row, startToken.Col);
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
            RecoverFromClassMatching();
            return null;
        }

        private string OptionalInheritance()
        {
            if (Input.NextTokenIs<PunctuationToken>("{")) // the beginning of the class body
            {
                return null;
            }
            Input.MatchAndConsume<KeywordToken>("extends");
            return Input.MatchAndConsume<IdentifierToken>().Lexeme;
        }

        private IStatement Statement()
        {
            var followSet = new string[] {";"}; // Possible recovery is done until the statement end symbol (usually ;), which is also consumed.
            try
            {
                if (Input.NextTokenIs<MiniJavaTypeToken>())
                    return VariableDeclaration();
                else if (Input.NextTokenOneOf<KeywordToken>("assert", "if", "while", "System", "return"))
                    return MakeKeywordStatement();
                else if (Input.NextTokenIs<PunctuationToken>("{"))
                {
                    followSet = new string[] {"}"};
                    return MakeBlockStatement();
                }
                else // Can be an assignment, a method invocation or a variable
                     // declaration for a user defined type.
                    return MakeExpressionStatementOrVariableDeclaration();
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Col, e.Row);
            }
            catch (LexicalError)
            {
                if (DebugMode) throw;
            }
            ParsingFailed = true;
            RecoverUntilPunctuationTokenAndConsume(followSet);
            return null;
        }

        // This is a workaround method that is needed because the language is not LL(1).
        private IStatement MakeExpressionStatementOrVariableDeclaration()
        {
            if (Input.NextTokenIs<IdentifierToken>())
            {
                var peekOne = Input.PeekForward(1);
                if (peekOne is PunctuationToken && peekOne.Lexeme == "[")
                {
                    var peekTwo = Input.PeekForward(2);
                    if (peekTwo is PunctuationToken && peekTwo.Lexeme == "]") // The statement is a local array variable declaration.
                    {
                        return ParseLocalVariableDeclaration(true);
                    }
                    // Otherwise this is an array indexing expression.
                }
                else if (peekOne is IdentifierToken) // A non-array variable declaration.
                {
                    return ParseLocalVariableDeclaration(false);
                }
                // Otherwise the first identifier is a variable reference and begins an expression.
            }

            var expression = Expression();
            return CompleteStatement(expression);
        }

        private IStatement ParseLocalVariableDeclaration(bool isArray)
        {
            var typeName = Input.MatchAndConsume<IdentifierToken>();
            if (isArray)
            {
                Input.MatchAndConsume<PunctuationToken>("[");
                Input.MatchAndConsume<PunctuationToken>("]");
            }
            var variableName = Input.MatchAndConsume<IdentifierToken>();
            Input.MatchAndConsume<PunctuationToken>(";");
            return new VariableDeclaration(variableName.Lexeme, typeName.Lexeme, isArray,
                typeName.Row, typeName.Col);
        }

        private IStatement CompleteStatement(IExpression expression)
        {
            if (expression == null) // there was a parse error in the expression
            {
                RecoverUntilPunctuationTokenAndConsume(";");
                return null;
            }
            else if (Input.NextTokenIs<OperatorToken>("="))
            {
                return MakeAssignmentStatement(expression);
            }
            else if (expression is MethodInvocation)
            {
                Input.MatchAndConsume<PunctuationToken>(";");
                return (MethodInvocation)expression;
            }
            else
            {
                var expr = (SyntaxElement)expression;
                throw new SyntaxError(String.Format("Expression of type {0} cannot form a statement on its own.", expression.Describe()),
                    expr.Row, expr.Col);
            }
        }

        private IStatement MakeAssignmentStatement(IExpression lhs)
        {
            var assignment = Input.MatchAndConsume<OperatorToken>("=");
            IExpression rhs = Expression();
            Input.MatchAndConsume<PunctuationToken>(";");
            return new AssignmentStatement(lhs, rhs,
                assignment.Row, assignment.Col);
        }

        private IStatement MakeBlockStatement()
        {
            IToken blockStart = Input.MatchAndConsume<PunctuationToken>("{");
            var statements = StatementList();
            Input.MatchAndConsume<PunctuationToken>("}");
            return new BlockStatement(statements, blockStart.Row, blockStart.Col);
        }

        // Keyword statements are statements starting with a keyword.
        // (Other than new or this which start an expression that can
        // also begin a statement.)
        private IStatement MakeKeywordStatement()
        {
            Debug.Assert(Input.NextTokenOneOf<KeywordToken>("assert", "if", "while", "System", "return")); // should not be here otherwise
            var token = (KeywordToken) Input.Peek();
            switch (token.Lexeme)
            {
                case "assert":
                    return MakeAssertStatement();
                case "if":
                    return MakeIfStatement();
                case "while":
                    return MakeWhileStatement();
                case "System":
                    return MakePrintStatement();
                default:
                    return MakeReturnStatement();
            }
        }

        private IStatement MakeReturnStatement()
        {
            var returnToken = Input.MatchAndConsume<KeywordToken>("return");
            var expression = Expression();
            Input.MatchAndConsume<PunctuationToken>(";");
            return new ReturnStatement(expression,
                returnToken.Row, returnToken.Col);
        }

        private IStatement MakePrintStatement()
        {
            var systemToken = Input.MatchAndConsume<KeywordToken>("System");
            Input.MatchAndConsume<PunctuationToken>(".");
            Input.MatchAndConsume<KeywordToken>("out");
            Input.MatchAndConsume<PunctuationToken>(".");
            Input.MatchAndConsume<KeywordToken>("println");
            Input.MatchAndConsume<PunctuationToken>("(");
            var integerExpression = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            Input.MatchAndConsume<PunctuationToken>(";");
            return new PrintStatement(integerExpression,
                systemToken.Row, systemToken.Col);
        }

        private IStatement MakeWhileStatement()
        {
            var whileToken = Input.MatchAndConsume<KeywordToken>("while");
            Input.MatchAndConsume<PunctuationToken>("(");
            var booleanExpr = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            var whileBody = Statement();
            return new WhileStatement(booleanExpr, whileBody,
                whileToken.Row, whileToken.Col);
        }

        private IStatement MakeIfStatement()
        {
            var ifToken = Input.MatchAndConsume<KeywordToken>("if");
            Input.MatchAndConsume<PunctuationToken>("(");
            IExpression booleanExpr = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            var thenBranch = Statement();
            var elseBranch = OptionalElseBranch();
            return new IfStatement(booleanExpr, thenBranch, elseBranch,
                ifToken.Row, ifToken.Col);
        }

        private IStatement MakeAssertStatement()
        {
            var assertToken = Input.MatchAndConsume<KeywordToken>("assert");
            Input.MatchAndConsume<PunctuationToken>("(");
            IExpression expr = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            Input.MatchAndConsume<PunctuationToken>(";"); // not in the original CFG, probably a mistake?
            return new AssertStatement(expr, assertToken.Row, assertToken.Col);
        }

        private IStatement OptionalElseBranch()
        {
            if (Input.NextTokenIs<KeywordToken>("else"))
            {
                Input.MatchAndConsume<KeywordToken>();
                return Statement();
            }
            else
                return null;
        }

        private IExpression Expression()
        {
            var expressionParser = new ExpressionParser(Input, ErrorReporter, DebugMode);
            var expr = expressionParser.Parse();
            ParsingFailed = ParsingFailed || expressionParser.ParsingFailed;
            return expr;
        }

        private Declaration Declaration()
        {
            string[] followSet = null;
            try
            {
                if (Input.NextTokenIs<MiniJavaTypeToken>() || Input.NextTokenIs<IdentifierToken>())
                {
                    followSet = new string[] { ";" };
                    return VariableDeclaration();
                }
                else if (Input.NextTokenIs<KeywordToken>("public"))
                {
                    followSet = new string[] { "}" };
                    return MethodDeclaration();
                }
                else
                {
                    followSet = new string[] { ";", "}" }; // If we're here, we don't know what kind of a declaration was attempted, so we just let
                    var token = Input.Consume<IToken>();   // the recovery routine parse until whichever punctuation token comes first and try to continue.
                    string errorMsg = "";
                    if (token is EndOfFile)
                    {
                        errorMsg = String.Format("Reached end of file while parsing a declaration.");
                    }
                    else if (token is ErrorToken)
                    {
                        errorMsg = String.Format("Lexical error while parsing a declaration.");
                    }
                    else
                    {
                        errorMsg = String.Format("Invalid token '{0}' of type {1} starting a declaration.",
                            token.Lexeme, TokenDescriptions.Describe(token.GetType()));
                    }
                    throw new SyntaxError(errorMsg, token.Row, token.Col);
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
            Debug.Assert(followSet != null);
            ParsingFailed = true;
            RecoverUntilPunctuationTokenAndConsume(followSet);
            return null;
        }

        private VariableDeclaration VariableDeclaration()
        {
            var variableDecl = VariableOrFormalParameterDeclaration(";");
            Input.MatchAndConsume<PunctuationToken>(";");
            return variableDecl;
        }

        private MethodDeclaration MethodDeclaration()
        {
            IToken startToken = Input.MatchAndConsume<KeywordToken>("public");
            var typeInfo = Type();
            var type = typeInfo.typeToken;
            var methodName = Input.MatchAndConsume<IdentifierToken>();

            Input.MatchAndConsume<PunctuationToken>("("); // parameter list
            List<VariableDeclaration> parameters = FormalParameters();
            Input.MatchAndConsume<PunctuationToken>(")");

            Input.MatchAndConsume<PunctuationToken>("{"); // method body
            List<IStatement> methodBody = StatementList();
            Input.MatchAndConsume<PunctuationToken>("}");

            return new MethodDeclaration(methodName.Lexeme, type.Lexeme,
                typeInfo.isArray, parameters, methodBody, startToken.Row,
                startToken.Col);
        }

        private VariableDeclaration VariableOrFormalParameterDeclaration(params string[] followSet)
        {
            try
            {
                var typeInfo = Type();
                var type = typeInfo.typeToken;
                var variableIdent = Input.MatchAndConsume<IdentifierToken>();
                return new VariableDeclaration(variableIdent.Lexeme, type.Lexeme,
                    typeInfo.isArray, type.Row, type.Col);
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
            RecoverFromVariableDeclarationMatching(followSet);
            return null;
        }

        private TypeData Type()
        {
            var type = Input.MatchAndConsume<ITypeToken>();
            if (Input.NextTokenIs<PunctuationToken>("["))
            {
                Input.MatchAndConsume<PunctuationToken>();
                Input.MatchAndConsume<PunctuationToken>("]");
                return new TypeData()
                {
                    typeToken = type,
                    isArray = true,
                    arraySize = null
                };
            }
            return new TypeData()
            {
                typeToken = type,
                isArray = false,
                arraySize = null
            };
        }

        // List parsing with sub-parsers.

        private List<ClassDeclaration> ClassDeclarationList()
        {
            var parser = new ListParser(Input, ErrorReporter);
            return parser.ParseList(ClassDeclaration);
        }

        private List<Declaration> DeclarationList()
        {
            var parser = new ListParser(Input, ErrorReporter);
            return parser.ParseList<Declaration, PunctuationToken>(Declaration, "}");
        }

        private List<IStatement> StatementList()
        {
            var parser = new ListParser(Input, ErrorReporter);
            return parser.ParseList<IStatement, PunctuationToken>(Statement, "}");
        }

        private List<VariableDeclaration> FormalParameters()
        {
            var parser = new CommaSeparatedListParser(Input, ErrorReporter);
            return parser.ParseList<VariableDeclaration, PunctuationToken>(
                () => VariableOrFormalParameterDeclaration(",", ")"), ")");
        }

        // Recovery routines.

        private void RecoverFromClassMatching()
        {
            while (!(Input.NextTokenIs<EndOfFile>() || Input.NextTokenIs<KeywordToken>("class")))
                Input.Consume<IToken>();
        }

        // Note: the follow token (punctuation) will be consumed.
        private void RecoverUntilPunctuationTokenAndConsume(params string[] followSet)
        {
            while (!(Input.NextTokenIs<EndOfFile>() || Input.NextTokenOneOf<PunctuationToken>(followSet)))
            {
                Input.Consume<IToken>();
            }
            if (!Input.NextTokenIs<EndOfFile>())
            {
                Input.Consume<IToken>();
            }
        }

        // This recovery procedure does not consume punctuation tokens in the follow set
        // so as not to confuse list or statement parsers.
        private void RecoverFromVariableDeclarationMatching(params string[] followSet)
        {
            while (!(Input.NextTokenIs<EndOfFile>() || Input.NextTokenOneOf<PunctuationToken>(followSet)))
                Input.Consume<IToken>();
        }
    }
}