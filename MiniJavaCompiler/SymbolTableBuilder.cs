using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler
{
    namespace SemanticAnalysis
    {
        public class SymbolTableBuilder : INodeVisitor
        {
            private Program syntaxTree;
            private GlobalScope symbolTable;
            private BaseScope currentScope;
            private Dictionary<ISyntaxTreeNode, IScope> scopes;

            public SymbolTableBuilder(Program node, List<string> types)
            {
                syntaxTree = node;
                symbolTable = new GlobalScope();
                currentScope = symbolTable;
                scopes = new Dictionary<ISyntaxTreeNode, IScope>();
            }

            public BaseScope BuildSymbolTable()
            {
                syntaxTree.Accept(this);
                return symbolTable;
            }

            public Dictionary<ISyntaxTreeNode, IScope> GetScopeMapping()
            {
                return scopes;
            }

            public void Visit(Program node) { }

            public void Visit(ClassDeclaration node) { }

            public void Visit(MainClassDeclaration node)
            {
                throw new NotImplementedException();
            }

            public void Visit(VariableDeclaration node)
            {
                throw new NotImplementedException();
            }

            public void Visit(MethodDeclaration node)
            {
                throw new NotImplementedException();
            }

            public void Visit(PrintStatement node)
            {
                throw new NotImplementedException();
            }

            public void Visit(ReturnStatement node)
            {
                throw new NotImplementedException();
            }

            public void Visit(BlockStatement node)
            {
                throw new NotImplementedException();
            }

            public void Visit(AssertStatement node)
            {
                throw new NotImplementedException();
            }

            public void Visit(AssignmentStatement node)
            {
                throw new NotImplementedException();
            }

            public void Visit(IfStatement node)
            {
                throw new NotImplementedException();
            }

            public void Visit(WhileStatement node)
            {
                throw new NotImplementedException();
            }

            public void Visit(MethodInvocation node)
            {
                throw new NotImplementedException();
            }

            public void Visit(InstanceCreationExpression node)
            {
                throw new NotImplementedException();
            }

            public void Visit(UnaryNotExpression node)
            {
                throw new NotImplementedException();
            }

            public void Visit(ArithmeticOpExpression node)
            {
                throw new NotImplementedException();
            }

            public void Visit(LogicalOpExpression node)
            {
                throw new NotImplementedException();
            }

            public void Visit(BooleanLiteralExpression node)
            {
                throw new NotImplementedException();
            }

            public void Visit(ThisExpression node)
            {
                throw new NotImplementedException();
            }

            public void Visit(ArrayIndexingExpression node)
            {
                throw new NotImplementedException();
            }

            public void Visit(VariableReferenceExpression node)
            {
                throw new NotImplementedException();
            }

            public void Visit(IntegerLiteralExpression node)
            {
                throw new NotImplementedException();
            }
        }
    }
}
