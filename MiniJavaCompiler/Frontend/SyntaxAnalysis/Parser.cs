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

    public class SyntaxAnalysisFailed : Exception { }

    public abstract class ParserBase
    {
        internal bool DebugMode;
        internal bool ParsingFailed;
        protected IParserInputReader Input { get; set; }
        protected readonly IErrorReporter ErrorReporter;

        protected ParserBase(IParserInputReader input, IErrorReporter reporter, bool debugMode = false)
        {
            DebugMode = debugMode;
            ErrorReporter = reporter;
            Input = input;
        }
    }

    public class Parser : ParserBase, IParser
    {
        public Parser(IParserInputReader input, IErrorReporter reporter, bool debugMode = false) : base(input, reporter, debugMode) { }

        public Program Parse()
        {
            return Program();
        }

        public Program Program()
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
                throw new SyntaxAnalysisFailed();
            }

            // Invariant:
            // If end of file has been consumed (in place of a different, expected token),
            // it should have led into recovery that would have ended up in an OutOfInput error.
            // ClassDeclarationList stops parsing at the end of file.
            // => Therefore the next token here should always be EndOfFile.
            Debug.Assert(Input.NextTokenIs<EndOfFile>());
            Input.MatchAndConsume<EndOfFile>();

            if (ParsingFailed)
            {
                throw new SyntaxAnalysisFailed();
            }
            return new Program(main, declarations);
        }

        public MainClassDeclaration MainClass()
        {
            try
            {
                IToken startToken = Input.MatchAndConsume<KeywordToken>("class");
                var classIdent = Input.MatchAndConsume<IdentifierToken>();
                Input.MatchAndConsume<PunctuationToken>("{");
                IToken methodStartToken = Input.MatchAndConsume<KeywordToken>("public");
                Input.MatchAndConsume<KeywordToken>("static");
                Input.MatchAndConsume<MiniJavaTypeToken>("void");
                Input.MatchAndConsume<KeywordToken>("main");

                Input.MatchAndConsume<PunctuationToken>("(");
                Input.MatchAndConsume<PunctuationToken>(")");

                Input.MatchAndConsume<PunctuationToken>("{");
                var mainStatements = StatementList();
                Input.MatchAndConsume<PunctuationToken>("}");

                Input.MatchAndConsume<PunctuationToken>("}");
                return new MainClassDeclaration(classIdent.Lexeme,
                    mainStatements, startToken.Row, startToken.Col,
                    methodStartToken.Row, methodStartToken.Col);
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                ParsingFailed = true;
                RecoverFromClassMatching();
                return null;
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                RecoverFromClassMatching();
                ParsingFailed = true;
                return null;
            }
        }

        public ClassDeclaration ClassDeclaration()
        {
            try
            {
                IToken startToken = Input.MatchAndConsume<KeywordToken>("class");
                var classIdent = Input.MatchAndConsume<IdentifierToken>();
                var inheritedClass = OptionalInheritance();
                Input.MatchAndConsume<PunctuationToken>("{");
                var declarations = DeclarationList();
                Input.MatchAndConsume<PunctuationToken>("}");
                return new ClassDeclaration(classIdent.Lexeme, inheritedClass,
                    declarations, startToken.Row, startToken.Col);
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                ParsingFailed = true;
                RecoverFromClassMatching();
                return null;
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                ParsingFailed = true;
                RecoverFromClassMatching();
                return null;
            }
        }

        public string OptionalInheritance()
        {
            if (Input.NextTokenIs<PunctuationToken>("{"))
            {
                return null;
            }
            Input.MatchAndConsume<KeywordToken>("extends");
            return Input.MatchAndConsume<IdentifierToken>().Lexeme;
        }

        public IStatement Statement()
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
                ParsingFailed = true;
                RecoverUntilPunctuationToken(followSet);
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                ParsingFailed = true;
                RecoverUntilPunctuationToken(followSet);
            }
            return null;
        }

        // This is a workaround method that is needed because the language is not LL(1).
        // Some buffering is done because several tokens must be peeked at to decide
        // which kind of statement should be parsed.
        private IStatement MakeExpressionStatementOrVariableDeclaration()
        {
            IExpression expression = null;
            IStatement statement = null;
            if (Input.NextTokenIs<IdentifierToken>())
            {
                var ident = Input.Consume<IdentifierToken>();
                if (Input.NextTokenIs<PunctuationToken>("["))
                {
                    var leftBracket = Input.Consume<PunctuationToken>();
                    if (Input.NextTokenIs<PunctuationToken>("]"))
                    { // The statement is a local array variable declaration.
                        Input.Consume<PunctuationToken>();
                        statement = FinishParsingLocalVariableDeclaration(ident, true);
                    }
                    else
                    {   // Brackets are used to index into an array, beginning an expression.
                        // Push back the tokens that were already consumed so the expression parser
                        // can match them again.
                        Input.PushBack(leftBracket);
                        Input.PushBack(ident);
                        expression = Expression();
                    }
                }
                else if (Input.NextTokenIs<IdentifierToken>()) // non-array variable declaration
                    statement = FinishParsingLocalVariableDeclaration(ident, false);
                else
                {   // The consumed identifier token is a reference to a variable
                    // and begins an expression.
                    Input.PushBack(ident);
                    expression = Expression();
                }
            }
            else
                expression = Expression();

            return statement ?? CompleteStatement(expression);
        }

        private IStatement CompleteStatement(IExpression expression)
        {
            if (expression == null) // there was a parse error in the expression
            {
                RecoverUntilPunctuationToken(";");
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
                case "return":
                    return MakeReturnStatement();
                default:
                    return null;
            }
        }

        private IStatement MakeReturnStatement()
        {
            var returnToken = Input.Consume<KeywordToken>();
            var expression = Expression();
            Input.MatchAndConsume<PunctuationToken>(";");
            return new ReturnStatement(expression,
                returnToken.Row, returnToken.Col);
        }

        private IStatement MakePrintStatement()
        {
            var systemToken = Input.Consume<KeywordToken>();
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
            var whileToken = Input.Consume<KeywordToken>();
            Input.MatchAndConsume<PunctuationToken>("(");
            var booleanExpr = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            var whileBody = Statement();
            return new WhileStatement(booleanExpr, whileBody,
                whileToken.Row, whileToken.Col);
        }

        private IStatement MakeIfStatement()
        {
            var ifToken = Input.Consume<KeywordToken>();
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
            var assertToken = Input.Consume<KeywordToken>();
            Input.MatchAndConsume<PunctuationToken>("(");
            IExpression expr = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            Input.MatchAndConsume<PunctuationToken>(";"); // not in the original CFG, probably a mistake?
            return new AssertStatement(expr, assertToken.Row, assertToken.Col);
        }

        private IStatement FinishParsingLocalVariableDeclaration(IdentifierToken variableTypeName, bool isArray)
        {
            var variableName = Input.MatchAndConsume<IdentifierToken>();
            Input.MatchAndConsume<PunctuationToken>(";");
            return new VariableDeclaration(variableName.Lexeme, variableTypeName.Lexeme, isArray,
                variableTypeName.Row, variableTypeName.Col);
        }

        public IStatement OptionalElseBranch()
        {
            if (Input.NextTokenIs<KeywordToken>("else"))
            {
                Input.MatchAndConsume<KeywordToken>();
                return Statement();
            }
            else
                return null;
        }

        public IExpression Expression()
        {
            var expressionParser = new ExpressionParser(Input, ErrorReporter, DebugMode);
            var expr = expressionParser.Parse();
            ParsingFailed = ParsingFailed || expressionParser.ParsingFailed;
            return expr;
        }

        public Declaration Declaration()
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
                    followSet = new string[] { ";", "}" }; // If we're here, we don't know what kind of a declaration they tried to make, so just let
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
                Debug.Assert(followSet != null);
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                ParsingFailed = true;
                RecoverUntilPunctuationToken(followSet);
            }
            catch (LexicalErrorEncountered)
            {
                Debug.Assert(followSet != null);
                if (DebugMode) throw;
                ParsingFailed = true;
                RecoverUntilPunctuationToken(followSet);
            }
            return null;
        }

        public VariableDeclaration VariableDeclaration()
        {
            var variableDecl = VariableOrFormalParameterDeclaration(";");
            Input.MatchAndConsume<PunctuationToken>(";");
            return variableDecl;
        }

        private VariableDeclaration VariableOrFormalParameterDeclaration(params string[] followSet)
        {
            try
            {
                var typeInfo = Type();
                var type = typeInfo.Item1;
                var variableIdent = Input.MatchAndConsume<IdentifierToken>();
                return new VariableDeclaration(variableIdent.Lexeme, type.Lexeme,
                    typeInfo.Item2, type.Row, type.Col);
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                ParsingFailed = true;
                RecoverFromVariableDeclarationMatching(followSet);
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                ParsingFailed = true;
                RecoverFromVariableDeclarationMatching(followSet);
            }
            return null;
        }

        public MethodDeclaration MethodDeclaration()
        {
            IToken startToken = Input.MatchAndConsume<KeywordToken>("public");
            var typeInfo = Type();
            var type = typeInfo.Item1;
            var methodName = Input.MatchAndConsume<IdentifierToken>();
            Input.MatchAndConsume<PunctuationToken>("(");
            List<VariableDeclaration> parameters = FormalParameters();
            Input.MatchAndConsume<PunctuationToken>(")");
            Input.MatchAndConsume<PunctuationToken>("{");
            List<IStatement> methodBody = StatementList();
            Input.MatchAndConsume<PunctuationToken>("}");
            return new MethodDeclaration(methodName.Lexeme, type.Lexeme,
                typeInfo.Item2, parameters, methodBody, startToken.Row,
                startToken.Col);
        }

        // Returns a 2-tuple with the matched type token as the first element and
        // a bool value indicating whether the type is an array or not as the
        // second element.
        public Tuple<ITypeToken, bool> Type()
        {
            var type = Input.MatchAndConsume<ITypeToken>();
            if (Input.NextTokenIs<PunctuationToken>("["))
            {
                Input.MatchAndConsume<PunctuationToken>();
                Input.MatchAndConsume<PunctuationToken>("]");
                return new Tuple<ITypeToken, bool>(type, true);
            }
            return new Tuple<ITypeToken, bool>(type, false);
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

        private void RecoverUntilPunctuationToken(params string[] followSet)
        {
            while (!(Input.NextTokenIs<EndOfFile>() || Input.NextTokenOneOf<PunctuationToken>(followSet)))
            {
                Input.Consume<IToken>();
            }
            if (Input.NextTokenIs<PunctuationToken>()) // discard the follow token as well (assume it to be a part of the declaration/statement)
            {
                Input.Consume<IToken>();
            }
        }

        private void RecoverFromVariableDeclarationMatching(params string[] followSet)
        {
            while (!(Input.NextTokenIs<EndOfFile>() || Input.NextTokenOneOf<PunctuationToken>(followSet)))
                Input.Consume<IToken>();
            // The punctuation tokens in the follow set are not consumed so as not to
            // confuse list or statement parsers.
        }
    }
}