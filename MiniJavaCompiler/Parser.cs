using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support.TokenTypes;

namespace MiniJavaCompiler
{
    namespace SyntaxAnalysis
    {
        public class Parser
        {
            private Scanner scanner;
            private Token input_token;

            public Parser(Scanner scanner)
            {
                this.scanner = scanner;
                this.input_token = null;
            }

            public Program Parse()
            {
                this.input_token = scanner.NextToken();
                return Program();
            }

            private Program Program()
            {
                var main = MainClass();
                var declarations = ClassDeclarationList();
                return new Program(main, declarations);
            }

            private MainClassDeclaration MainClass()
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

            private Statement Statement()
            {
                if (input_token is KeywordToken)
                {
                    KeywordToken token = (KeywordToken)input_token;
                    switch (token.Value)
                    {
                        case "assert":
                            Match<KeywordToken>("assert");
                            Match<LeftParenthesis>(); // btw, Java asserts apparently do not use parentheses
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
                            throw new NotImplementedException();
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
                { // has to be an assignment or a method invocation
                    var startToken = input_token;
                    var expression = Expression();
                    if (input_token is AssignmentToken)
                    {
                        Match<AssignmentToken>();
                        Expression rhs = Expression();
                        Match<EndLine>();
                        return new AssignmentStatement(expression, rhs,
                            startToken.Row, startToken.Col);
                    }
                    else
                    {
                        Match<EndLine>();
                        if (expression is MethodInvocation)
                            return (MethodInvocation)expression;
                        else // error
                            throw new NotImplementedException();
                    }
                }
            }

            private Statement OptionalElseBranch()
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

            private Expression Expression()
            {
                throw new NotImplementedException();
            }

            private ClassDeclaration ClassDeclaration()
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

            private string OptionalInheritance()
            {
                if (!(input_token is LeftCurlyBrace))
                {
                    Match<KeywordToken>("extends");
                    return Match<Identifier>().Value;
                }
                return null;
            }

            private Declaration Declaration()
            {
                if (input_token is KeywordToken)
                {
                    return MethodDeclaration();
                }
                else if (input_token is MiniJavaType || input_token is Identifier)
                {
                    VariableDeclaration variable = VariableDeclaration();
                    Match<EndLine>();
                    return variable;
                }
                else
                    throw new NotImplementedException();
            }

            private VariableDeclaration VariableDeclaration()
            {
                var typeInfo = Type();
                var type = (StringToken)typeInfo.Item1;
                Identifier variableIdent = Match<Identifier>();
                return new VariableDeclaration(variableIdent.Value, type.Value,
                    typeInfo.Item2, type.Row, type.Col);
            }

            private MethodDeclaration MethodDeclaration()
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

            private List<VariableDeclaration> FormalParameters(bool isListTail = false)
            {
                var formals = new List<VariableDeclaration>();
                if (!(input_token is RightParenthesis))
                {
                    if (isListTail) Match<ParameterSeparator>();
                    VariableDeclaration firstParam = VariableDeclaration();
                    formals.Add(firstParam);
                    formals.AddRange(FormalParameters(true));
                }
                return formals;
            }

            // Returns a 2-tuple with the matched type token as the first element and
            // a bool value indicating whether the type is an array or not as the
            // second element.
            private Tuple<TypeToken, bool> Type()
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
                        // return an error node or throw an exception?
                        throw new NotImplementedException();
                    }
                }
                else if (input_token is ErrorToken)
                {
                    // return an error node or throw an exception?
                    // some recovery needs to be done at this point
                    throw new NotImplementedException();
                }
                else
                {
                    // return an error node or throw an exception?
                    throw new NotImplementedException();
                }
            }
        }
    }
}
