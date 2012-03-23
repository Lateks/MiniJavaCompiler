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
                    return VariableDeclaration();
                else
                    throw new NotImplementedException();
            }

            private Declaration VariableDeclaration()
            {
                throw new NotImplementedException();
            }

            private Declaration MethodDeclaration()
            {
                Token startToken = Match<KeywordToken>("public");
                string type = Type();
                Identifier methodName = Match<Identifier>();
                Match<LeftParenthesis>();
                List<VariableDeclaration> parameters = FormalParameters();
                Match<RightParenthesis>();
                Match<LeftCurlyBrace>();
                List<Statement> methodBody = StatementList();
                Match<RightCurlyBrace>();
                return new MethodDeclaration(methodName.Value, type, parameters,
                    methodBody, startToken.Row, startToken.Col);
            }

            private List<VariableDeclaration> FormalParameters()
            {
                throw new NotImplementedException();
            }

            private string Type()
            {
                throw new NotImplementedException();
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
