using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.SemanticAnalysis
{
    public class SymbolTableBuilder : INodeVisitor
    {
        private readonly Program syntaxTree;
        private readonly GlobalScope globalScope;
        private readonly Stack<IScope> scopeStack;
        private readonly Dictionary<ISyntaxTreeNode, IScope> scopes;
        private readonly string[] builtins = new [] { "int", "boolean" }; // TODO: Refactor this into Support
        private readonly IErrorReporter errorReporter;
        private IScope CurrentScope
        {
            get { return scopeStack.Peek(); }
        }

        public SymbolTableBuilder(Program node, IEnumerable<string> types, IErrorReporter errorReporter)
        {
            this.errorReporter = errorReporter;
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
            foreach (var type in builtins)
            {
                Symbol.CreateAndDefine<BuiltinTypeSymbol>(type, globalScope);
            }
            foreach (var type in types)
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
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.Name);
            if (node.InheritedClass != null)
            {
                var inheritedType = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.InheritedClass);
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
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.Name);
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
            // TODO: Refactor this check into a separate method.
            Symbol varType = globalScope.Resolve<TypeSymbol>(node.Type);
            if (varType == null)
            {
                errorReporter.ReportError("Unknown type '" + node.Type + "'.", node.Row, node.Col);
                throw new Exception("placeholder"); // TODO: recover?
            }

            IType actualType = node.IsArray ?
                new MiniJavaArrayType((ISimpleType)varType) :
                (IType) varType;
            var symbol = Symbol.CreateAndDefine<VariableSymbol>(node.Name, actualType, CurrentScope);

            // TODO: Refactor this check into a separate method.
            if (symbol == null)
            {
                errorReporter.ReportError("Symbol '" + node.Name + "' is already defined in this scope.", node.Row, node.Col);
                throw new Exception("placeholder"); // TODO: recover?
            }
            node.Symbol = symbol;
        }

        public void Visit(MethodDeclaration node)
        {
            if (!(CurrentScope is UserDefinedTypeSymbol))
            {
                errorReporter.ReportError("Method declarations are not allowed in this context.", node.Row, node.Col);
                throw new Exception("placeholder"); // TODO: recover?
            }

            Symbol mType = globalScope.Resolve<TypeSymbol>(node.Type);
            if (mType == null)
            {
                errorReporter.ReportError("Unknown type '" + node.Type + "'.", node.Row, node.Col);
                throw new Exception("placeholder"); // TODO: recover?
            }

            IType actualType = node.IsArray ?
                new MiniJavaArrayType((ISimpleType)mType) :
                (IType)mType;
            var methodSymbol = Symbol.CreateAndDefine<MethodSymbol>(
                node.Name, actualType, CurrentScope);

            // TODO: Refactor this check into a separate method.
            if (methodSymbol == null)
            {
                errorReporter.ReportError("Symbol '" + node.Name + "' is already defined.", node.Row, node.Col);
                throw new Exception("placeholder"); // TODO: recover?
            }

            node.Symbol = methodSymbol;
            EnterScope((IScope) methodSymbol);
        }

        public void Exit(MethodDeclaration node)
        {
            ExitScope();
        }

        public void Visit(VariableReferenceExpression node)
        {
            var resolvedVariable = CurrentScope.Resolve<VariableSymbol>(node.Name);
            if (resolvedVariable == null)
            {
                errorReporter.ReportError("Reference to unknown variable '" + node.Name + "'.", node.Row, node.Col);
                throw new Exception("placeholder"); // TODO: recover?
            }
        }

        public void Visit(BlockStatement node)
        {
            var blockScope = new LocalScope();
            scopes[node] = blockScope;
            EnterScope(blockScope);
        }

        public void Exit(BlockStatement node)
        {
            ExitScope();
        }

        public void Visit(PrintStatement node) { }

        public void Visit(ReturnStatement node) { }

        public void Visit(AssertStatement node) { }

        public void Visit(AssignmentStatement node) { }

        public void Visit(IfStatement node) { }

        public void Visit(WhileStatement node) { }

        public void Visit(MethodInvocation node) { }

        public void Visit(InstanceCreationExpression node) { }

        public void Visit(UnaryNotExpression node) { }

        public void Visit(BinaryOpExpression node) { }

        public void Visit(BooleanLiteralExpression node) { }

        public void Visit(ThisExpression node) { }

        public void Visit(ArrayIndexingExpression node) { }

        public void Visit(IntegerLiteralExpression node) { }
    }
}
