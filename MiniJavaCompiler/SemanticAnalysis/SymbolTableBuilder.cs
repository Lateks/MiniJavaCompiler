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
        private GlobalScope globalScope;
        private Stack<IScope> scopeStack;
        private Dictionary<ISyntaxTreeNode, IScope> scopes;
        private string[] builtins = new [] { "int", "boolean" };
        private IScope CurrentScope
        {
            get { return scopeStack.Peek(); }
        }

        public SymbolTableBuilder(Program node, IEnumerable<string> types)
        {
            syntaxTree = node;
            globalScope = new GlobalScope();
            SetupGlobalScope(types);
            scopeStack = new Stack<IScope>();
            scopeStack.Push(globalScope);
            scopes = new Dictionary<ISyntaxTreeNode, IScope>();
        }

        private void EnterScope(IScope scope)
        {
            scopeStack.Push(scope);
        }

        private void ExitScope()
        {
            scopeStack.Pop();
        }

        private void SetupGlobalScope(IEnumerable<string> types)
        {
            foreach (string type in builtins)
            {
                Symbol.CreateAndDefine<BuiltinTypeSymbol>(type, globalScope);
            }
            foreach (string type in types)
            {
                Symbol.CreateAndDefine<UserDefinedTypeSymbol>(type, globalScope);
            }
        }

        public void BuildSymbolTable()
        {
            syntaxTree.Accept(this);
        }

        public Dictionary<ISyntaxTreeNode, IScope> GetScopeMapping()
        {
            return scopes;
        }

        public void Visit(Program node) { }

        public void Visit(ClassDeclaration node)
        {
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve(node.Name);
            if (node.InheritedClass != null)
            {
                var inheritedType = (UserDefinedTypeSymbol)CurrentScope.Resolve(node.InheritedClass);
                typeSymbol.SuperClass = inheritedType;
            }
            scopes[node] = typeSymbol;
            node.Symbol = typeSymbol;
            EnterScope(typeSymbol);
        }

        public void Exit(ClassDeclaration node)
        {
            ExitScope();
        }

        public void Visit(MainClassDeclaration node)
        {
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve(node.Name);
            scopes[node] = typeSymbol;
            node.Symbol = typeSymbol;
            EnterScope(typeSymbol);
        }

        public void Exit(MainClassDeclaration node)
        {
            ExitScope();
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

        public void Exit(MethodDeclaration node)
        {
            throw new NotImplementedException();
        }
    }
}
