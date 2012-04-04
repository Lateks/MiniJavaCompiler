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
            private Stack<Token> inputBuffer;
            Token InputToken
            {
                get;
                set;
            }

            private void buffer(Token token)
            {
                inputBuffer.Push(InputToken);
                InputToken = token;
            }

            public Parser(Scanner scanner)
            {
                this.scanner = scanner;
                this.inputBuffer = new Stack<Token>();
                InputToken = scanner.NextToken();
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
                if (InputToken is MiniJavaType)
                    return MakeLocalVariableDeclaration();
                else if (InputToken is KeywordToken)
                    return MakeKeywordStatement();
                else if (InputToken is LeftCurlyBrace)
                    return MakeBlockStatement();
                else // Can be an assignment, a method invocation or a variable
                     // declaration for a user defined type.
                    return MakeExpressionStatementOrVariableDeclaration();
            }

            // This is a workaround method that is needed because the language is not LL(1).
            // Some buffering is done because several tokens must be peeked at to decide
            // which kind of statement should be parsed.
            private Statement MakeExpressionStatementOrVariableDeclaration()
            {
                Expression expression;
                if (InputToken is Identifier)
                { 
                    var ident = Consume<Identifier>();
                    if (MatchWithoutConsuming<LeftBracket>())
                    {
                        var lBracket = Consume<LeftBracket>();
                        if (MatchWithoutConsuming<RightBracket>())
                        { // The statement is a local array variable declaration.
                            Consume<RightBracket>();
                            return MakeVariableDeclarationStatement(ident, true);
                        }
                        else
                        {   // Brackets are used to index into an array, beginning an expression.
                            // Buffer the tokens that were already consumed for the expression parser.
                            buffer(lBracket);
                            buffer(ident);
                            expression = Expression();
                        }
                    }
                    else if (MatchWithoutConsuming<Identifier>())
                        return MakeVariableDeclarationStatement(ident, false);
                    else
                    { // Input token is a reference to a variable and begins an expression.
                        buffer(ident);
                        expression = Expression();
                    }
                }
                else
                    expression = Expression();

                return CompleteStatement(expression);
            }

            private Statement CompleteStatement(Expression expression)
            {
                if (InputToken is AssignmentToken)
                    return MakeAssignmentStatement(expression);
                else
                    return MakeMethodInvocationStatement(expression);
            }

            private Statement MakeMethodInvocationStatement(Expression expression)
            {
                Match<EndLine>();
                if (expression is MethodInvocation)
                    return (MethodInvocation)expression;
                else
                    throw new SyntaxError("Expression of type " + expression.GetType().Name +
                        " cannot form a statement on its own.");
            }

            private Statement MakeAssignmentStatement(Expression lhs)
            {
                var assignment = Match<AssignmentToken>();
                Expression rhs = Expression();
                Match<EndLine>();
                return new AssignmentStatement(lhs, rhs,
                    assignment.Row, assignment.Col);
            }

            private Statement MakeBlockStatement()
            {
                Token blockStart = Match<LeftCurlyBrace>();
                var statements = StatementList();
                Match<RightCurlyBrace>();
                return new BlockStatement(statements, blockStart.Row, blockStart.Col);
            }

            private Statement MakeLocalVariableDeclaration()
            {
                var decl = VariableDeclaration();
                Match<EndLine>();
                return decl;
            }

            private Statement MakeKeywordStatement()
            {
                KeywordToken token = (KeywordToken)InputToken;
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
                        throw new SyntaxError("Invalid keyword " + token.Value + " starting a statement.");
                }
            }

            private Statement MakeReturnStatement()
            {
                var returnToken = Consume<KeywordToken>();
                var expression = Expression();
                Match<EndLine>();
                return new ReturnStatement(expression,
                    returnToken.Row, returnToken.Col);
            }

            private Statement MakePrintStatement()
            {
                var systemToken = Consume<KeywordToken>();
                Match<MethodInvocationToken>();
                Match<KeywordToken>("out");
                Match<MethodInvocationToken>();
                Match<KeywordToken>("println");
                Match<LeftParenthesis>();
                var integerExpression = Expression();
                Match<RightParenthesis>();
                Match<EndLine>();
                return new PrintStatement(integerExpression,
                    systemToken.Row, systemToken.Col);
            }

            private Statement MakeWhileStatement()
            {
                var whileToken = Consume<KeywordToken>();
                Match<LeftParenthesis>();
                var booleanExpr = Expression();
                Match<RightParenthesis>();
                var whileBody = Statement();
                return new WhileStatement(booleanExpr, whileBody,
                    whileToken.Row, whileToken.Col);
            }

            private Statement MakeIfStatement()
            {
                var ifToken = Consume<KeywordToken>();
                Match<LeftParenthesis>();
                Expression booleanExpr = Expression();
                Match<RightParenthesis>();
                var thenBranch = Statement();
                var elseBranch = OptionalElseBranch();
                return new IfStatement(booleanExpr, thenBranch, elseBranch,
                    ifToken.Row, ifToken.Col);
            }

            private Statement MakeAssertStatement()
            {
                var assertToken = Consume<KeywordToken>();
                Match<LeftParenthesis>();
                Expression expr = Expression();
                Match<RightParenthesis>();
                Match<EndLine>(); // not in the CFG, probably a bug?
                return new AssertStatement(expr, assertToken.Row, assertToken.Col);
            }

            private Statement MakeVariableDeclarationStatement(Identifier variableTypeName, bool isArray)
            {
                var variableName = Match<Identifier>();
                Match<EndLine>();
                return new VariableDeclaration(variableName.Value, variableTypeName.Value, isArray,
                    variableTypeName.Row, variableTypeName.Col);
            }

            public Statement OptionalElseBranch()
            {
                if (InputToken is KeywordToken &&
                    ((KeywordToken)InputToken).Value == "else")
                {
                    Match<KeywordToken>("else");
                    return Statement();
                }
                else
                    return null;
            }

            public Expression Expression()
            {
                var binOpParser = new ExpressionParser(this);
                return binOpParser.parse();
            }

            // An internal parser that solves operator precedences in expressions.
            private class ExpressionParser
            {
                Parser Parent
                {
                    get;
                    set;
                }

                public ExpressionParser(Parser parent)
                {
                    Parent = parent;
                }

                public Expression parse()
                {
                    return ParseExpression();
                }

                private Expression ParseExpression()
                {
                    var firstOp = OrOperand();
                    return OrOperandList(firstOp);
                }

                private Expression OrOperandList(Expression lhs)
                {
                    if (Parent.MatchWithoutConsuming<BinaryOperatorToken>("||"))
                    {
                        var opToken = Parent.Consume<BinaryOperatorToken>();
                        var rhs = OrOperand();
                        return OrOperandList(new LogicalOp(opToken.Value, lhs, rhs,
                            opToken.Row, opToken.Col));
                    }
                    else
                        return lhs;
                }

                private Expression OrOperand()
                {
                    var firstOp = AndOperand();
                    return AndOperandList(firstOp);
                }

                private Expression AndOperandList(Expression lhs)
                {
                    if (Parent.MatchWithoutConsuming<BinaryOperatorToken>("&&"))
                    {
                        var opToken = Parent.Consume<BinaryOperatorToken>();
                        var rhs = AndOperand();
                        return AndOperandList(new LogicalOp(opToken.Value, lhs, rhs,
                            opToken.Row, opToken.Col));
                    }
                    else
                        return lhs;
                }

                private Expression AndOperand()
                {
                    var firstOp = EqOperand();
                    return EqOperandList(firstOp);
                }

                private Expression EqOperandList(Expression lhs)
                {
                    if (Parent.MatchWithoutConsuming<BinaryOperatorToken>("=="))
                    {
                        var opToken = Parent.Consume<BinaryOperatorToken>();
                        var rhs = EqOperand();
                        return EqOperandList(new LogicalOp(opToken.Value, lhs, rhs,
                            opToken.Row, opToken.Col));
                    }
                    else
                        return lhs;
                }

                private Expression EqOperand()
                {
                    var firstOp = NotEqOperand();
                    return NotEqOperandList(firstOp);
                }

                private Expression NotEqOperandList(Expression lhs)
                {
                    if (Parent.MatchWithoutConsuming<BinaryOperatorToken>("<") ||
                        Parent.MatchWithoutConsuming<BinaryOperatorToken>(">"))
                    {
                        var opToken = Parent.Consume<BinaryOperatorToken>();
                        var rhs = NotEqOperand();
                        return NotEqOperandList(new LogicalOp(opToken.Value, lhs, rhs,
                            opToken.Row, opToken.Col));
                    }
                    else
                        return lhs;
                }

                private Expression NotEqOperand()
                {
                    var firstOp = AddOperand();
                    return AddOperandList(firstOp);
                }

                private Expression AddOperandList(Expression lhs)
                {
                    if (Parent.MatchWithoutConsuming<BinaryOperatorToken>("+") ||
                        Parent.MatchWithoutConsuming<BinaryOperatorToken>("-"))
                    {
                        var opToken = Parent.Consume<BinaryOperatorToken>();
                        var rhs = NotEqOperand();
                        return NotEqOperandList(new ArithmeticOp(opToken.Value, lhs, rhs,
                            opToken.Row, opToken.Col));
                    }
                    else
                        return lhs;
                }

                private Expression AddOperand()
                {
                    var firstOp = MultOperand();
                    return MultOperandList(firstOp);
                }

                private Expression MultOperandList(Expression lhs)
                {
                    if (Parent.MatchWithoutConsuming<BinaryOperatorToken>("*") ||
                        Parent.MatchWithoutConsuming<BinaryOperatorToken>("/") ||
                        Parent.MatchWithoutConsuming<BinaryOperatorToken>("%"))
                    {
                        var opToken = Parent.Consume<BinaryOperatorToken>();
                        var rhs = MultOperand();
                        return MultOperandList(new ArithmeticOp(opToken.Value, lhs, rhs,
                            opToken.Row, opToken.Col));
                    }
                    else
                        return lhs;
                }

                private Expression MultOperand()
                {
                    if (Parent.MatchWithoutConsuming<UnaryNotToken>())
                    {
                        var token = Parent.Consume<UnaryNotToken>();
                        var term = Term();
                        return new UnaryNot(term, token.Row, token.Col);
                    }
                    else
                        return Term();
                }

                public Expression Term()
                {
                    if (Parent.InputToken is KeywordToken)
                    {
                        var token = (KeywordToken)Parent.InputToken;
                        switch (token.Value)
                        {
                            case "new":
                                Parent.Match<KeywordToken>("new");
                                var typeInfo = NewType();
                                var type = (StringToken)typeInfo.Item1;
                                return OptionalTermTail(new InstanceCreation(type.Value,
                                    token.Row, token.Col, typeInfo.Item2));
                            case "this":
                                Parent.Match<KeywordToken>("this");
                                return OptionalTermTail(new ThisExpression(token.Row,
                                    token.Col));
                            case "true":
                                Parent.Match<KeywordToken>("true");
                                return OptionalTermTail(new BooleanLiteral(true,
                                    token.Row, token.Col));
                            case "false":
                                Parent.Match<KeywordToken>("false");
                                return OptionalTermTail(new BooleanLiteral(false,
                                    token.Row, token.Col));
                            default: // error, invalid start token for expression
                                throw new SyntaxError("Invalid start token " + token.Value +
                                    " for expression.");
                        }
                    }
                    else if (Parent.InputToken is Identifier)
                    {
                        var identifier = Parent.Match<Identifier>();
                        return OptionalTermTail(new VariableReference(
                            identifier.Value, identifier.Row, identifier.Col));
                    }
                    else if (Parent.InputToken is IntegerLiteralToken)
                    {
                        var token = Parent.Match<IntegerLiteralToken>();
                        return OptionalTermTail(new IntegerLiteral(token.Value,
                            token.Row, token.Col));
                    }
                    else if (Parent.InputToken is LeftParenthesis)
                    {
                        var token = Parent.Match<LeftParenthesis>();
                        Expression parenthesisedExpression = Parent.Expression();
                        return OptionalTermTail(parenthesisedExpression);
                    }
                    throw new SyntaxError("Invalid start token of type " +
                        Parent.InputToken.GetType().Name + " for expression.");
                }

                public Expression OptionalTermTail(Expression lhs)
                {
                    if (Parent.InputToken is LeftBracket)
                    {
                        var startToken = Parent.Match<LeftBracket>();
                        var indexExpression = Parent.Expression();
                        Parent.Match<RightBracket>();
                        return OptionalTermTail(new ArrayIndexExpression(lhs, indexExpression, startToken.Row,
                            startToken.Col)); // slightly dubious row and column numbers here
                    }
                    else if (Parent.InputToken is MethodInvocationToken)
                    {
                        var startToken = Parent.Match<MethodInvocationToken>();
                        string methodname;
                        List<Expression> parameters;
                        if (Parent.InputToken is KeywordToken)
                        {
                            methodname = Parent.Match<KeywordToken>("length").Value;
                            parameters = new List<Expression>();
                        }
                        else
                        {
                            methodname = Parent.Match<Identifier>().Value;
                            Parent.Match<LeftParenthesis>();
                            parameters = Parent.ExpressionList();
                            Parent.Match<RightParenthesis>();
                        }
                        return OptionalTermTail(new MethodInvocation(lhs, methodname, parameters,
                            startToken.Row, startToken.Col));
                    }
                    else
                        return lhs;
                }

                public Tuple<TypeToken, Expression> NewType()
                {
                    var type = Parent.Match<TypeToken>();
                    if (type is MiniJavaType || !(Parent.InputToken is LeftParenthesis))
                    { // must be an array
                        Parent.Match<LeftBracket>();
                        var arraySize = Parent.Expression();
                        Parent.Match<RightBracket>();
                        return new Tuple<TypeToken, Expression>(type, arraySize);
                    }
                    else
                    {
                        Parent.Match<LeftParenthesis>();
                        Parent.Match<RightParenthesis>();
                        return new Tuple<TypeToken, Expression>(type, null);
                    }
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
                if (!(InputToken is LeftCurlyBrace))
                {
                    Match<KeywordToken>("extends");
                    return Match<Identifier>().Value;
                }
                return null;
            }

            public Declaration Declaration()
            {
                if (InputToken is MiniJavaType || InputToken is Identifier)
                {
                    VariableDeclaration variable = VariableDeclaration();
                    Match<EndLine>();
                    return variable;
                }
                else if (InputToken is KeywordToken)
                {
                    return MethodDeclaration();
                }
                else
                    throw new SyntaxError("Invalid token of type " + InputToken.GetType().Name +
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
                if (InputToken is LeftBracket)
                {
                    Match<LeftBracket>();
                    Match<RightBracket>();
                    return new Tuple<TypeToken, bool>(type, true);
                }
                return new Tuple<TypeToken, bool>(type, false);
            }

            private T Match<T>(string value = null) where T : Token
            {
                if (MatchWithoutConsuming<T>(value))
                    return Consume<T>();
                else if (value == null)
                    throw new SyntaxError("Expected type " + typeof(T).Name +
                        " but got " + InputToken.GetType().Name + ".");
                else
                    throw new SyntaxError("Expected value \"" + value + "\" but got " +
                        ((StringToken)InputToken).Value + ".");
            }

            private T Consume<T>() where T : Token
            {
                var temp = (T)InputToken;
                InputToken = inputBuffer.Count > 0 ? inputBuffer.Pop() : scanner.NextToken();
                return temp;
            }

            private bool MatchWithoutConsuming<T>(string value = null) where T : Token
            {
                if (InputToken is ErrorToken)
                    throw new SyntaxError("Received an error token."); // recovery must be done

                if (InputToken is T)
                {
                    if (value == null || ((StringToken)InputToken).Value == value)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
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
                if (!(InputToken is FollowToken))
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
                if (!(InputToken is FollowToken))
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
