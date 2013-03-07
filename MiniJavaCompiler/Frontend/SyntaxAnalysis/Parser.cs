using System;
using System.Collections.Generic;
using System.Diagnostics;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Support;
using ast = MiniJavaCompiler.Support.AbstractSyntaxTree;

namespace MiniJavaCompiler.Frontend.SyntaxAnalysis
{
    public interface IParser
    {
        ast.Program Parse();
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
            public ast.IExpression arraySize;
        }
    }

    public class Parser : ParserBase, IParser
    {
        private int currentLocalIndex;

        public Parser(ITokenizer input, IErrorReporter reporter, bool debugMode = false)
            : base(new ParserInputReader(input, reporter), reporter, debugMode) { }

        public ast.Program Parse()
        {
            var program = Program();
            if (ParsingFailed) // This exception is thrown if either lexical or syntactic errors are found in the token stream.
            {
                throw new CompilationError();
            }
            return program;
        }

        private ast.Program Program()
        {
            ast.ClassDeclaration main;
            List<ast.ClassDeclaration> declarations;
            try
            {
                main = MainClass();
                declarations = ClassDeclarationList();
            }
            catch (OutOfInput e)
            {
                if (DebugMode) throw;
                ErrorReporter.ReportError(e.Message, -1, -1);
                throw new CompilationError();
            }

            // Invariant:
            // If end of file has been consumed (in place of a different, expected token),
            // it should have led into recovery that would have ended up in an OutOfInput error.
            // ClassDeclarationList stops parsing at the end of file.
            // => Therefore the next token here should always be EndOfFile.
            Debug.Assert(Input.NextTokenIs<EndOfFile>());
            Input.MatchAndConsume<EndOfFile>();

            return new ast.Program(main, declarations);
        }

        private ast.ClassDeclaration MainClass()
        {
            try
            {
                IToken startToken = Input.MatchAndConsume<KeywordToken>("class");

                var classIdent = Input.MatchAndConsume<IdentifierToken>();
                Input.MatchAndConsume<PunctuationToken>("{"); // class body begins
                IToken methodStartToken = Input.MatchAndConsume<KeywordToken>("public");
                Input.MatchAndConsume<KeywordToken>("static");
                Input.MatchAndConsume<MiniJavaTypeToken>("void");
                Input.MatchAndConsume<KeywordToken>(MiniJavaInfo.MainMethodIdent);
                Input.MatchAndConsume<PunctuationToken>("(");
                Input.MatchAndConsume<PunctuationToken>(")");

                Input.MatchAndConsume<PunctuationToken>("{"); // main method body begins
                var mainStatements = StatementList();
                Input.MatchAndConsume<PunctuationToken>("}");

                Input.MatchAndConsume<PunctuationToken>("}"); // class body ends

                var mainMethod = ast.MethodDeclaration.CreateMainMethodDeclaration(
                    mainStatements, methodStartToken.Row, methodStartToken.Col);
                return ast.ClassDeclaration.CreateMainClassDeclaration(classIdent.Lexeme,
                    mainMethod, startToken.Row, startToken.Col);
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

        private ast.ClassDeclaration ClassDeclaration()
        {
            try
            {
                IToken startToken = Input.MatchAndConsume<KeywordToken>("class");
                var classIdent = Input.MatchAndConsume<IdentifierToken>();
                var inheritedClass = OptionalInheritance(); // inherited class can be null

                Input.MatchAndConsume<PunctuationToken>("{"); // class body begins
                var declarations = DeclarationList();
                Input.MatchAndConsume<PunctuationToken>("}");
                return new ast.ClassDeclaration(classIdent.Lexeme, inheritedClass,
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

        private ast.IStatement Statement()
        {
            var followSet = new string[] {";"}; // Possible recovery is done until the statement end symbol (usually ;), which is also consumed.
            try
            {
                if (Input.NextTokenIs<MiniJavaTypeToken>())
                    return VariableDeclaration(ast.VariableDeclaration.Kind.Local);
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
                ErrorReporter.ReportError(e.Message, e.Row, e.Col);
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
        private ast.IStatement MakeExpressionStatementOrVariableDeclaration()
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

        private ast.IStatement ParseLocalVariableDeclaration(bool isArray)
        {
            var typeName = Input.MatchAndConsume<IdentifierToken>();
            if (isArray)
            {
                Input.MatchAndConsume<PunctuationToken>("[");
                Input.MatchAndConsume<PunctuationToken>("]");
            }
            var variableName = Input.MatchAndConsume<IdentifierToken>();
            Input.MatchAndConsume<PunctuationToken>(";");

            int localIndex = currentLocalIndex;
            currentLocalIndex++;
            return new ast.VariableDeclaration(variableName.Lexeme, typeName.Lexeme, isArray,
                ast.VariableDeclaration.Kind.Local, localIndex, typeName.Row, typeName.Col);
        }

        private ast.IStatement CompleteStatement(ast.IExpression expression)
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
            else if (expression is ast.MethodInvocation)
            {
                Input.MatchAndConsume<PunctuationToken>(";");
                return (ast.MethodInvocation)expression;
            }
            else
            {
                var expr = (ast.SyntaxElement)expression;
                throw new SyntaxError(String.Format("Expression of type {0} cannot form a statement on its own.", expression.Describe()),
                    expr.Row, expr.Col);
            }
        }

        private ast.IStatement MakeAssignmentStatement(ast.IExpression lhs)
        {
            var assignment = Input.MatchAndConsume<OperatorToken>("=");
            ast.IExpression rhs = Expression();
            Input.MatchAndConsume<PunctuationToken>(";");
            return new ast.AssignmentStatement(lhs, rhs,
                assignment.Row, assignment.Col);
        }

        private ast.IStatement MakeBlockStatement()
        {
            IToken blockStart = Input.MatchAndConsume<PunctuationToken>("{");
            var statements = StatementList();
            Input.MatchAndConsume<PunctuationToken>("}");
            return new ast.BlockStatement(statements, blockStart.Row, blockStart.Col);
        }

        // Keyword statements are statements starting with a keyword.
        // (Other than new or this which start an expression that can
        // also begin a statement.)
        private ast.IStatement MakeKeywordStatement()
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

        private ast.IStatement MakeReturnStatement()
        {
            var returnToken = Input.MatchAndConsume<KeywordToken>("return");
            var expression = Expression();
            Input.MatchAndConsume<PunctuationToken>(";");
            return new ast.ReturnStatement(expression,
                returnToken.Row, returnToken.Col);
        }

        private ast.IStatement MakePrintStatement()
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
            return new ast.PrintStatement(integerExpression,
                systemToken.Row, systemToken.Col);
        }

        private ast.IStatement MakeWhileStatement()
        {
            var whileToken = Input.MatchAndConsume<KeywordToken>("while");
            Input.MatchAndConsume<PunctuationToken>("(");
            var booleanExpr = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            var whileBody = Statement();
            return new ast.WhileStatement(booleanExpr, whileBody,
                whileToken.Row, whileToken.Col);
        }

        private ast.IStatement MakeIfStatement()
        {
            var ifToken = Input.MatchAndConsume<KeywordToken>("if");
            Input.MatchAndConsume<PunctuationToken>("(");
            ast.IExpression booleanExpr = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            var thenBranch = Statement();
            var elseBranch = OptionalElseBranch();
            return new ast.IfStatement(booleanExpr, thenBranch, elseBranch,
                ifToken.Row, ifToken.Col);
        }

        private ast.IStatement MakeAssertStatement()
        {
            var assertToken = Input.MatchAndConsume<KeywordToken>("assert");
            Input.MatchAndConsume<PunctuationToken>("(");
            ast.IExpression expr = Expression();
            Input.MatchAndConsume<PunctuationToken>(")");
            Input.MatchAndConsume<PunctuationToken>(";"); // not in the original CFG, probably a mistake?
            return new ast.AssertStatement(expr, assertToken.Row, assertToken.Col);
        }

        private ast.IStatement OptionalElseBranch()
        {
            if (Input.NextTokenIs<KeywordToken>("else"))
            {
                Input.MatchAndConsume<KeywordToken>();
                return Statement();
            }
            else
                return null;
        }

        private ast.IExpression Expression()
        {
            var expressionParser = new ExpressionParser(Input, ErrorReporter, DebugMode);
            var expr = expressionParser.Parse();
            ParsingFailed = ParsingFailed || expressionParser.ParsingFailed;
            return expr;
        }

        private ast.Declaration Declaration()
        {
            string[] followSet = null;
            try
            {
                if (Input.NextTokenIs<MiniJavaTypeToken>() || Input.NextTokenIs<IdentifierToken>())
                {
                    followSet = new string[] { ";" };
                    currentLocalIndex = 0; // class fields are always numbered with 0 (their ordering does not matter)
                    return VariableDeclaration(ast.VariableDeclaration.Kind.Class);
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

        private ast.VariableDeclaration VariableDeclaration(ast.VariableDeclaration.Kind kind)
        {
            var variableDecl = VariableOrFormalParameterDeclaration(kind, ";");
            Input.MatchAndConsume<PunctuationToken>(";");
            return variableDecl;
        }

        private ast.MethodDeclaration MethodDeclaration()
        {
            IToken startToken = Input.MatchAndConsume<KeywordToken>("public");
            var typeInfo = Type();
            var type = typeInfo.typeToken;
            var methodName = Input.MatchAndConsume<IdentifierToken>();

            Input.MatchAndConsume<PunctuationToken>("("); // parameter list
            currentLocalIndex = 0; // number parameters starting from 0
            List<ast.VariableDeclaration> parameters = FormalParameters();
            Input.MatchAndConsume<PunctuationToken>(")");

            Input.MatchAndConsume<PunctuationToken>("{"); // method body
            currentLocalIndex = 0; // number locals starting from 0
            List<ast.IStatement> methodBody = StatementList();
            Input.MatchAndConsume<PunctuationToken>("}");

            return new ast.MethodDeclaration(methodName.Lexeme, type.Lexeme,
                typeInfo.isArray, parameters, methodBody, startToken.Row,
                startToken.Col);
        }

        private ast.VariableDeclaration VariableOrFormalParameterDeclaration(ast.VariableDeclaration.Kind kind, params string[] followSet)
        {
            try
            {
                int localIndex = currentLocalIndex;
                currentLocalIndex++;
                var typeInfo = Type();
                var type = typeInfo.typeToken;
                var variableIdent = Input.MatchAndConsume<IdentifierToken>();
                return new ast.VariableDeclaration(variableIdent.Lexeme, type.Lexeme,
                    typeInfo.isArray, kind, localIndex, type.Row, type.Col);
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

        private List<ast.ClassDeclaration> ClassDeclarationList()
        {
            var parser = new ListParser(Input, ErrorReporter);
            return parser.ParseList(ClassDeclaration);
        }

        private List<ast.Declaration> DeclarationList()
        {
            var parser = new ListParser(Input, ErrorReporter);
            return parser.ParseList<ast.Declaration, PunctuationToken>(Declaration, "}");
        }

        private List<ast.IStatement> StatementList()
        {
            var parser = new ListParser(Input, ErrorReporter);
            return parser.ParseList<ast.IStatement, PunctuationToken>(Statement, "}");
        }

        private List<ast.VariableDeclaration> FormalParameters()
        {
            var parser = new CommaSeparatedListParser(Input, ErrorReporter);
            return parser.ParseList<ast.VariableDeclaration, PunctuationToken>(
                () => VariableOrFormalParameterDeclaration(
                    ast.VariableDeclaration.Kind.Formal ,",", ")"), ")");
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