using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.SyntaxAnalysis
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

            try
            {
                Input.MatchAndConsume<EndOfFile>();
            }
            catch (SyntaxError e)
            { // Found something other than end of file.
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                throw new SyntaxAnalysisFailed();
            }
            catch (OutOfInput e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, 0, 0);
                throw new SyntaxAnalysisFailed();
            }

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
                var classIdent = Input.MatchAndConsume<Identifier>();
                Input.MatchAndConsume<PunctuationToken>("{");
                IToken methodStartToken = Input.MatchAndConsume<KeywordToken>("public");
                Input.MatchAndConsume<KeywordToken>("static");
                Input.MatchAndConsume<MiniJavaType>("void");
                Input.MatchAndConsume<KeywordToken>("main");

                Input.MatchAndConsume<PunctuationToken>("(");
                Input.MatchAndConsume<PunctuationToken>(")");

                Input.MatchAndConsume<PunctuationToken>("{");
                var mainStatements = StatementList();
                Input.MatchAndConsume<PunctuationToken>("}");

                Input.MatchAndConsume<PunctuationToken>("}");
                return new MainClassDeclaration(classIdent.Value,
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

        private void RecoverFromClassMatching()
        {
            while (!(Input.NextTokenIs<EndOfFile>() || Input.NextTokenIs<KeywordToken>("class")))
                Input.Consume<IToken>();
        }

        public IStatement Statement()
        {
            try
            {
                if (Input.NextTokenIs<MiniJavaType>())
                    return VariableDeclaration();
                else if (Input.NextTokenIs<KeywordToken>())
                    return MakeKeywordStatement();
                else if (Input.NextTokenIs<PunctuationToken>("{"))
                    return MakeBlockStatement();
                else // Can be an assignment, a method invocation or a variable
                    // declaration for a user defined type.
                    return MakeExpressionStatementOrVariableDeclaration();
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Col, e.Row);
                ParsingFailed = true;
                RecoverFromStatementMatching();
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                ParsingFailed = true;
                RecoverFromStatementMatching();
            }
            return null;
        }

        private void RecoverFromStatementMatching()
        {
            while (!Input.NextTokenIs<EndOfFile>())
            {
                var token = Input.Consume<IToken>();
                if (token is PunctuationToken && ((PunctuationToken)token).Value == ";")
                    break;
            }
        }

        // This is a workaround method that is needed because the language is not LL(1).
        // Some buffering is done because several tokens must be peeked at to decide
        // which kind of statement should be parsed.
        private IStatement MakeExpressionStatementOrVariableDeclaration()
        {
            IExpression expression;
            if (Input.NextTokenIs<Identifier>())
            {
                var ident = Input.Consume<Identifier>();
                if (Input.NextTokenIs<PunctuationToken>("["))
                {
                    var leftBracket = Input.Consume<PunctuationToken>();
                    if (Input.NextTokenIs<PunctuationToken>("]"))
                    { // The statement is a local array variable declaration.
                        Input.Consume<PunctuationToken>();
                        return FinishParsingLocalVariableDeclaration(ident, true);
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
                else if (Input.NextTokenIs<Identifier>())
                    return FinishParsingLocalVariableDeclaration(ident, false);
                else
                {   // The consumed identifier token is a reference to a variable
                    // and begins an expression.
                    Input.PushBack(ident);
                    expression = Expression();
                }
            }
            else
                expression = Expression();

            return CompleteStatement(expression);
        }

        private IStatement CompleteStatement(IExpression expression)
        {
            if (Input.NextTokenIs<OperatorToken>("="))
                return MakeAssignmentStatement(expression);
            else
                return MakeMethodInvocationStatement(expression);
        }

        private IStatement MakeMethodInvocationStatement(IExpression expression)
        {
            Input.MatchAndConsume<PunctuationToken>(";");
            if (expression is MethodInvocation)
                return (MethodInvocation)expression;
            else
            {
                var expr = (SyntaxElement)expression;
                throw new SyntaxError("Expression of type " + expression.GetType().Name +
                    " cannot form a statement on its own.", expr.Row, expr.Col);
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
        private IStatement MakeKeywordStatement()
        {
            var token = (KeywordToken) Input.Peek();
            switch (token.Value)
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
                    throw new SyntaxError("Invalid keyword " + token.Value + " starting a statement.",
                        token.Row, token.Col);
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

        private IStatement FinishParsingLocalVariableDeclaration(Identifier variableTypeName, bool isArray)
        {
            var variableName = Input.MatchAndConsume<Identifier>();
            Input.MatchAndConsume<PunctuationToken>(";");
            return new VariableDeclaration(variableName.Value, variableTypeName.Value, isArray,
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
            ParsingFailed = ParsingFailed && expressionParser.ParsingFailed;
            return expr;
        }

        public ClassDeclaration ClassDeclaration()
        {
            try
            {
                IToken startToken = Input.MatchAndConsume<KeywordToken>("class");
                var classIdent = Input.MatchAndConsume<Identifier>();
                var inheritedClass = OptionalInheritance();
                Input.MatchAndConsume<PunctuationToken>("{");
                var declarations = DeclarationList();
                Input.MatchAndConsume<PunctuationToken>("}");
                return new ClassDeclaration(classIdent.Value, inheritedClass,
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
            if (!(Input.NextTokenIs<PunctuationToken>("{")))
            {
                Input.MatchAndConsume<KeywordToken>("extends");
                return Input.MatchAndConsume<Identifier>().Value;
            }
            return null;
        }

        public Declaration Declaration()
        {
            try
            {
                if (Input.NextTokenIs<MiniJavaType>() || Input.NextTokenIs<Identifier>())
                {
                    return VariableDeclaration();
                }
                else if (Input.NextTokenIs<KeywordToken>())
                {
                    return MethodDeclaration();
                }
                else
                {
                    var token = Input.Consume<IToken>();
                    throw new SyntaxError("Invalid token of type " + token.GetType().Name +
                        " starting a declaration.", token.Row, token.Col);
                }
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                ParsingFailed = true;
                RecoverFromDeclarationMatching();
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                ParsingFailed = true;
                RecoverFromDeclarationMatching();
            }
            return null;
        }

        private void RecoverFromDeclarationMatching()
        {
            while (!Input.NextTokenIs<EndOfFile>())
            {
                var token = Input.Consume<IToken>();
                if (token is PunctuationToken && ((PunctuationToken)token).Value == "}")
                    break;
            }
        }

        public VariableDeclaration VariableDeclaration()
        {
            var variableDecl = VariableOrFormalParameterDeclaration();
            Input.MatchAndConsume<PunctuationToken>(";");
            return variableDecl;
        }

        private VariableDeclaration VariableOrFormalParameterDeclaration()
        {
            try
            {
                var typeInfo = Type();
                var type = (StringToken)typeInfo.Item1;
                var variableIdent = Input.MatchAndConsume<Identifier>();
                return new VariableDeclaration(variableIdent.Value, type.Value,
                    typeInfo.Item2, type.Row, type.Col);
            }
            catch (SyntaxError e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
                ParsingFailed = true;
                RecoverFromVariableDeclarationMatching();
            }
            catch (LexicalErrorEncountered)
            {
                if (DebugMode) throw;
                ParsingFailed = true;
                RecoverFromVariableDeclarationMatching();
            }
            return null;
        }

        private void RecoverFromVariableDeclarationMatching()
        { // TODO: This could be parameterised on follow set.
            while (!(Input.NextTokenIs<EndOfFile>()
                || Input.NextTokenIs<PunctuationToken>(";")
                || Input.NextTokenIs<PunctuationToken>(",")
                || Input.NextTokenIs<PunctuationToken>(")")))
                Input.Consume<IToken>();
        }

        public MethodDeclaration MethodDeclaration()
        {
            IToken startToken = Input.MatchAndConsume<KeywordToken>("public");
            var typeInfo = Type();
            var type = (StringToken)typeInfo.Item1;
            var methodName = Input.MatchAndConsume<Identifier>();
            Input.MatchAndConsume<PunctuationToken>("(");
            List<VariableDeclaration> parameters = FormalParameters();
            Input.MatchAndConsume<PunctuationToken>(")");
            Input.MatchAndConsume<PunctuationToken>("{");
            List<IStatement> methodBody = StatementList();
            Input.MatchAndConsume<PunctuationToken>("}");
            return new MethodDeclaration(methodName.Value, type.Value,
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
                VariableOrFormalParameterDeclaration, ")");
        }
    }
}