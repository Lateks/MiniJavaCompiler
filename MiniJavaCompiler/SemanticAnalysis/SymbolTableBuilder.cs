using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.SemanticAnalysis
{
    public class SymbolTableBuilder : INodeVisitor
    {
        private Program syntaxTree;
        private GlobalScope symbolTable;
        private IScope currentScope;
        private Dictionary<ISyntaxTreeNode, IScope> scopes;
        private string[] builtins = new [] { "int", "boolean" };

        public SymbolTableBuilder(Program node, IEnumerable<string> types)
        {
            syntaxTree = node;
            symbolTable = new GlobalScope();
            SetupGlobalScope(types);
            currentScope = symbolTable;
            scopes = new Dictionary<ISyntaxTreeNode, IScope>();
        }

        private void SetupGlobalScope(IEnumerable<string> types)
        {
            foreach (string type in builtins)
            {
                Symbol.CreateAndDefine<BuiltinTypeSymbol>(type, symbolTable);
            }
            foreach (string type in types)
            {
                Symbol.CreateAndDefine<UserDefinedTypeSymbol>(type, symbolTable);
            }
        }

        public IScope BuildSymbolTable()
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

        public void Visit(BinaryOpExpression node)
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
