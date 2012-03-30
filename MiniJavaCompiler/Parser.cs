using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support.TokenTypes;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler
{
    namespace SyntaxAnalysis
    {
        public class SyntaxError : Exception
        {
            public SyntaxError(string message)
                : base(message) { }
        }

        public class Parser
        {
            private Scanner scanner;
            private Token input_token;

            public Parser(Scanner scanner)
            {
                this.scanner = scanner;
                this.input_token = scanner.NextToken();
            }

            public Program Parse()
            {
                return Program();
            }

            public Program Program()
            {
                var main = MainClass();
                var declarations = ClassDeclarationList();
                Match<EOF>();
                return new Program(main, declarations);
            }

            public MainClassDeclaration MainClass()
            {
                Token startToken = Match<KeywordToken>("class");
                Identifier classIdent = Match<Identifier>();
                Match<LeftCurlyBrace>();
                Match<KeywordToken>("public");
                Match<KeywordToken>("static");
                Match<MiniJavaType>("void");
                Match<KeywordToken>("main");
                Match<LeftParenthesis>();
                Match<RightParenthesis>();
                Match<LeftCurlyBrace>();
                List<Statement> main_statements = StatementList();
                Match<RightCurlyBrace>();
                Match<RightCurlyBrace>();
                return new MainClassDeclaration(classIdent.Value,
                    main_statements, startToken.Row, startToken.Col);
            }

            public Statement Statement()
            {
                if (input_token is MiniJavaType)      // Local variable declaration for one of the base types.
                {                                     // Variable declarations for user defined types are handled
                    var decl = VariableDeclaration(); // separately.
                    Match<EndLine>();
                    return decl;
                }
                else if (input_token is KeywordToken)
                {
                    KeywordToken token = (KeywordToken)input_token;
                    switch (token.Value)
                    {
                        case "assert":
                            Match<KeywordToken>("assert");
                            Match<LeftParenthesis>();
                            Expression expr = Expression();
                            Match<RightParenthesis>();
                            Match<EndLine>(); // not in the CFG, probably a bug?
                            return new AssertStatement(expr, token.Row, token.Col);
                        case "if":
                            Match<KeywordToken>("if");
                            Match<LeftParenthesis>();
                            Expression booleanExpr = Expression();
                            Match<RightParenthesis>();
                            return new IfStatement(booleanExpr, Statement(), OptionalElseBranch(),
                                token.Row, token.Col);
                        case "while":
                            Match<KeywordToken>("while");
                            Match<LeftParenthesis>();
                            booleanExpr = Expression();
                            Match<RightParenthesis>();
                            return new WhileStatement(booleanExpr, Statement(), token.Row, token.Col);
                        case "System":
                            Match<KeywordToken>("System");
                            Match<MethodInvocationToken>();
                            Match<KeywordToken>("out");
                            Match<MethodInvocationToken>();
                            Match<KeywordToken>("println");
                            Match<LeftParenthesis>();
                            var integerExpression = Expression();
                            Match<RightParenthesis>();
                            Match<EndLine>();
                            return new PrintStatement(integerExpression, token.Row, token.Col);
                        case "return":
                            Match<KeywordToken>("return");
                            var expression = Expression();
                            Match<EndLine>();
                            return new ReturnStatement(expression, token.Row, token.Col);
                        default: // error
                            throw new SyntaxError("Invalid keyword " + token.Value + " starting a statement.");
                    }
                }
                else if (input_token is LeftCurlyBrace)
                {
                    Token blockStart = Match<LeftCurlyBrace>();
                    var statements = StatementList();
                    Match<RightCurlyBrace>();
                    return new BlockStatement(statements, blockStart.Row, blockStart.Col);
                }
                else
                { // Can be an assignment, a method invocation or a variable declaration for a user defined type.
                  // This is really messy, must be refactored somehow.
                    var startRow = input_token.Row;
                    var startCol = input_token.Col;
                    Expression expression;
                    if (input_token is Identifier)
                    {
                        var ident1 = Match<Identifier>();
                        if (input_token is LeftBracket)
                        {
                            Match<LeftBracket>();
                            if (input_token is RightBracket)
                            {
                                Match<RightBracket>();
                                var ident2 = Match<Identifier>();
                                Match<EndLine>();
                                return new VariableDeclaration(ident2.Value, ident1.Value, true,
                                    startRow, startCol);
                            }
                            else
                            { // array indexing
                                var variable = new VariableReference(ident1.Value, startRow, startCol);
                                var indexExpression = Expression();
                                Match<RightBracket>();
                                expression = OptionalExpressionTail(
                                    new ArrayIndexExpression(variable, indexExpression,
                                    startRow, startCol));
                            }
                        }
                        else if (input_token is Identifier)
                        {
                            var ident2 = Match<Identifier>();
                            Match<EndLine>();
                            return new VariableDeclaration(ident2.Value, ident1.Value, false,
                                startRow, startCol);
                        }
                        else
                        {
                            expression = OptionalExpressionTail(new VariableReference(ident1.Value,
                                startRow, startCol));
                        }
                    }
                    else
                    {
                        expression = Expression();
                    }

                    if (input_token is AssignmentToken)
                    {
                        Match<AssignmentToken>();
                        Expression rhs = Expression();
                        Match<EndLine>();
                        return new AssignmentStatement(expression, rhs,
                            startRow, startCol);
                    }
                    else // should be a method invocation according to the original grammar
                    {
                        Match<EndLine>();
                        if (expression is MethodInvocation)
                            return (MethodInvocation)expression;
                        else
                            throw new SyntaxError("A " + expression.GetType().Name +
                                " cannot form a statement on its own.");
                    }
                }
            }

            public Statement OptionalElseBranch()
            {
                if (input_token is KeywordToken &&
                    ((KeywordToken)input_token).Value == "else")
                {
                    Match<KeywordToken>("else");
                    return Statement();
                }
                else
                    return null;
            }

            public Expression Expression()
            {
                if (input_token is KeywordToken)
                {
                    var token = (KeywordToken)input_token;
                    switch (token.Value)
                    {
                        case "new":
                            Match<KeywordToken>("new");
                            var typeInfo = NewType();
                            var type = (StringToken)typeInfo.Item1;
                            return OptionalExpressionTail(new InstanceCreation(type.Value,
                                token.Row, token.Col, typeInfo.Item2));
                        case "this":
                            Match<KeywordToken>("this");
                            return OptionalExpressionTail(new ThisExpression(token.Row,
                                token.Col));
                        case "true":
                            Match<KeywordToken>("true");
                            return OptionalExpressionTail(new BooleanLiteral(true,
                                token.Row, token.Col));
                        case "false":
                            Match<KeywordToken>("false");
                            return OptionalExpressionTail(new BooleanLiteral(false,
                                token.Row, token.Col));
                        default: // error, invalid start token for expression
                            throw new SyntaxError("Invalid start token " + token.Value +
                                " for expression.");
                    }
                }
                else if (input_token is Identifier)
                {
                    var identifier = Match<Identifier>();
                    return OptionalExpressionTail(new VariableReference(
                        identifier.Value, identifier.Row, identifier.Col));
                }
                else if (input_token is IntegerLiteralToken)
                {
                    var token = Match<IntegerLiteralToken>();
                    return OptionalExpressionTail(new IntegerLiteral(token.Value,
                        token.Row, token.Col));
                }
                else if (input_token is UnaryNotToken)
                {
                    var token = Match<UnaryNotToken>();
                    Expression booleanExpression = Expression();
                    return OptionalExpressionTail(new UnaryNot(booleanExpression,
                        token.Row, token.Col));
                }
                else if (input_token is LeftParenthesis)
                {
                    var token = Match<LeftParenthesis>();
                    Expression parenthesisedExpression = Expression();
                    return OptionalExpressionTail(parenthesisedExpression);
                }
                throw new SyntaxError("Invalid start token of type " + input_token.GetType().Name +
                    " for expression.");
            }

            public Expression OptionalExpressionTail(Expression lhs)
            {
                if (input_token is BinaryOperatorToken)
                {
                    return BinaryOperation(lhs);
                }
                else if (input_token is LeftBracket)
                {
                    var startToken = Match<LeftBracket>();
                    var indexExpression = Expression();
                    Match<RightBracket>();
                    return OptionalExpressionTail(new ArrayIndexExpression(lhs, indexExpression, startToken.Row,
                        startToken.Col)); // slightly dubious row and column numbers here
                }
                else if (input_token is MethodInvocationToken)
                {
                    var startToken = Match<MethodInvocationToken>();
                    string methodname;
                    List<Expression> parameters;
                    if (input_token is KeywordToken)
                    {
                        methodname = Match<KeywordToken>("length").Value;
                        parameters = new List<Expression>();
                    }
                    else
                    {
                        methodname = Match<Identifier>().Value;
                        Match<LeftParenthesis>();
                        parameters = ExpressionList();
                        Match<RightParenthesis>();
                    }
                    return OptionalExpressionTail(new MethodInvocation(lhs, methodname, parameters,
                        startToken.Row, startToken.Col));
                }
                else
                    return lhs;
            }

            private Expression BinaryOperation(Expression lhs)
            {
                var operatorToken = Match<BinaryOperatorToken>();
                Expression rhs = Expression();
                if (operatorToken is ArithmeticOperatorToken)
                    return OptionalExpressionTail(new ArithmeticOp(operatorToken.Value,
                        lhs, rhs, operatorToken.Row, operatorToken.Col));
                else
                    return OptionalExpressionTail(new LogicalOp(operatorToken.Value,
                        lhs, rhs, operatorToken.Row, operatorToken.Col));
            }

            public Tuple<TypeToken, Expression> NewType()
            {
                var type = Match<TypeToken>();
                if (type is MiniJavaType || !(input_token is LeftParenthesis))
                { // must be an array
                    Match<LeftBracket>();
                    var arraySize = Expression();
                    Match<RightBracket>();
                    return new Tuple<TypeToken, Expression>(type, arraySize);
                }
                else
                {
                    Match<LeftParenthesis>();
                    Match<RightParenthesis>();
                    return new Tuple<TypeToken, Expression>(type, null);
                }
            }

            public ClassDeclaration ClassDeclaration()
            {
                Token startToken = Match<KeywordToken>("class");
                Identifier classIdent = Match<Identifier>();
                string inheritedClass = OptionalInheritance();
                Match<LeftCurlyBrace>();
                List<Declaration> declarations = DeclarationList();
                Match<RightCurlyBrace>();
                return new ClassDeclaration(classIdent.Value, inheritedClass,
                    declarations, startToken.Row, startToken.Col);
            }

            public string OptionalInheritance()
            {
                if (!(input_token is LeftCurlyBrace))
                {
                    Match<KeywordToken>("extends");
                    return Match<Identifier>().Value;
                }
                return null;
            }

            public Declaration Declaration()
            {
                if (input_token is MiniJavaType || input_token is Identifier)
                {
                    VariableDeclaration variable = VariableDeclaration();
                    Match<EndLine>();
                    return variable;
                }
                else if (input_token is KeywordToken)
                {
                    return MethodDeclaration();
                }
                else
                    throw new SyntaxError("Invalid token of type " + input_token.GetType().Name +
                        " starting a declaration.");
            }

            public VariableDeclaration VariableDeclaration()
            {
                var typeInfo = Type();
                var type = (StringToken)typeInfo.Item1;
                Identifier variableIdent = Match<Identifier>();
                return new VariableDeclaration(variableIdent.Value, type.Value,
                    typeInfo.Item2, type.Row, type.Col);
            }

            public MethodDeclaration MethodDeclaration()
            {
                Token startToken = Match<KeywordToken>("public");
                var typeInfo = Type();
                var type = (StringToken)typeInfo.Item1;
                Identifier methodName = Match<Identifier>();
                Match<LeftParenthesis>();
                List<VariableDeclaration> parameters = FormalParameters();
                Match<RightParenthesis>();
                Match<LeftCurlyBrace>();
                List<Statement> methodBody = StatementList();
                Match<RightCurlyBrace>();
                return new MethodDeclaration(methodName.Value, type.Value,
                    typeInfo.Item2, parameters, methodBody, startToken.Row,
                    startToken.Col);
            }

            // Returns a 2-tuple with the matched type token as the first element and
            // a bool value indicating whether the type is an array or not as the
            // second element.
            public Tuple<TypeToken, bool> Type()
            {
                var type = Match<TypeToken>();
                if (input_token is LeftBracket)
                {
                    Match<LeftBracket>();
                    Match<RightBracket>();
                    return new Tuple<TypeToken, bool>(type, true);
                }
                return new Tuple<TypeToken, bool>(type, false);
            }

            private T Match<T>(string value = null) where T : Token
            {
                if (input_token is T)
                {
                    if (value == null ||
                        ((StringToken)input_token).Value == value)
                    {
                        var temp = (T)input_token;
                        input_token = scanner.NextToken();
                        return temp;
                    }
                    else
                    {
                        throw new SyntaxError("Expected value \"" + value + "\" but got " +
                            ((StringToken)input_token).Value + ".");
                    }
                }
                else if (input_token is ErrorToken)
                {
                    throw new SyntaxError("Received an error token. Wat?");
                }
                else
                {
                    throw new SyntaxError("Expected type " + typeof(T).Name +
                        " but got " + input_token.GetType().Name + ".");
                }
            }

            // List parsers.

            private List<ClassDeclaration> ClassDeclarationList()
            {
                return NodeList<ClassDeclaration, EOF>(ClassDeclaration);
            }

            private List<Declaration> DeclarationList()
            {
                return NodeList<Declaration, RightCurlyBrace>(Declaration);
            }

            private List<Statement> StatementList()
            {
                return NodeList<Statement, RightCurlyBrace>(Statement);
            }

            private List<NodeType> NodeList<NodeType, FollowToken>(Func<NodeType> ParseNode)
                where NodeType : SyntaxTreeNode
                where FollowToken : Token
            {
                var nodeList = new List<NodeType>();
                if (!(input_token is FollowToken))
                {
                    nodeList.Add(ParseNode());
                    nodeList.AddRange(NodeList<NodeType, FollowToken>(ParseNode));
                }
                return nodeList;
            }

            private List<VariableDeclaration> FormalParameters(bool isListTail = false)
            {
                return CommaSeparatedList<VariableDeclaration, RightParenthesis>(VariableDeclaration);
            }

            private List<Expression> ExpressionList(bool isListTail = false)
            {
                return CommaSeparatedList<Expression, RightParenthesis>(Expression);
            }

            private List<NodeType> CommaSeparatedList<NodeType, FollowToken>
                (Func<NodeType> ParseNode, bool isListTail = false)
                where NodeType : SyntaxTreeNode
                where FollowToken : Token
            {
                var list = new List<NodeType>();
                if (!(input_token is FollowToken))
                {
                    if (isListTail) Match<ParameterSeparator>();
                    list.Add(ParseNode());
                    list.AddRange(CommaSeparatedList<NodeType, FollowToken>(
                        ParseNode, true));
                }
                return list;
            }
        }
    }
}
