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
        private readonly SymbolTable symbolTable; 
        private readonly Program syntaxTree;
        private readonly string[] builtins = new [] { "int", "boolean" }; // TODO: Refactor this into Support
        private readonly IErrorReporter errorReporter;

        private readonly Stack<IScope> scopeStack;
        private IScope CurrentScope
        {
            get { return scopeStack.Peek(); }
        }

        private void EnterScope(IScope scope)
        {
            scopeStack.Push(scope);
        }

        private void ExitScope()
        {
            scopeStack.Pop();
        }

        public SymbolTableBuilder(Program node, IEnumerable<string> types, IErrorReporter errorReporter)
        {
            this.errorReporter = errorReporter;
            syntaxTree = node;

            symbolTable = new SymbolTable
                              {
                                  GlobalScope = new GlobalScope(),
                                  Definitions = new Dictionary<Symbol, ISyntaxTreeNode>(),
                                  Scopes = new Dictionary<ISyntaxTreeNode, IScope>()
                              };

            SetupGlobalScope(types);
            scopeStack = new Stack<IScope>();
            EnterScope(symbolTable.GlobalScope);
        }

        private void SetupGlobalScope(IEnumerable<string> types)
        {
            foreach (var type in builtins)
            {
                Symbol.CreateAndDefine<BuiltinTypeSymbol>(type, symbolTable.GlobalScope);
            }
            foreach (var type in types)
            {
                Symbol.CreateAndDefine<UserDefinedTypeSymbol>(type, symbolTable.GlobalScope);
            }
        }

        public SymbolTable BuildSymbolTable()
        {
            syntaxTree.Accept(this);
            return symbolTable;
        }

        public void Visit(Program node)
        {
            symbolTable.Scopes.Add(node, symbolTable.GlobalScope);
        }

        public void Visit(ClassDeclaration node)
        {
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.Name);
            if (node.InheritedClass != null)
            {
                var inheritedType = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.InheritedClass);
                typeSymbol.SuperClass = inheritedType;
            }
            symbolTable.Scopes.Add(node, typeSymbol);
            symbolTable.Definitions.Add(typeSymbol, node);
            EnterScope(typeSymbol);
        }

        public void Exit(ClassDeclaration node)
        {
            ExitScope();
        }

        public void Visit(MainClassDeclaration node)
        {
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.Name);
            symbolTable.Scopes.Add(node, typeSymbol);
            symbolTable.Definitions.Add(typeSymbol, node);
            EnterScope(typeSymbol);
        }

        public void Exit(MainClassDeclaration node)
        {
            ExitScope();
        }

        public void Visit(VariableDeclaration node)
        {
            var variableType = CheckDeclaredType(node);
            var symbol = DefineSymbolOrFail<VariableSymbol>(node, variableType);
            symbolTable.Definitions.Add(symbol, node);
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(MethodDeclaration node)
        {
            var methodReturnType = node.Type == "void" ? null : CheckDeclaredType(node);
            var methodSymbol = (MethodSymbol)DefineSymbolOrFail<MethodSymbol>(node, methodReturnType);
            symbolTable.Definitions.Add(methodSymbol, node);
            symbolTable.Scopes.Add(node, methodSymbol);

            EnterScope(methodSymbol);
        }

        private IType CheckDeclaredType(Declaration node)
        {
            Symbol nodeSimpleType = symbolTable.GlobalScope.Resolve<TypeSymbol>(node.Type);
            if (nodeSimpleType == null)
            {
                errorReporter.ReportError("Unknown type '" + node.Type + "'.", node.Row, node.Col);
                throw new Exception("Failed to resolve type for declaration"); // TODO: recover?
            }
            IType actualType = node.IsArray ?
                new MiniJavaArrayType((ISimpleType)nodeSimpleType) :
                (IType)nodeSimpleType;

            return actualType;
        }

        private Symbol DefineSymbolOrFail<TSymbolType>(Declaration node, IType symbolType)
            where TSymbolType : Symbol
        {
            var symbol = Symbol.CreateAndDefine<TSymbolType>(
                node.Name, symbolType, CurrentScope);

            if (symbol == null)
            {
                errorReporter.ReportError("Symbol '" + node.Name + "' is already defined.", node.Row, node.Col);
                throw new Exception("Failed to define symbol"); // TODO: recover?
            }

            return symbol;
        }

        public void Exit(MethodDeclaration node)
        {
            ExitScope();
        }

        public void Visit(BlockStatement node)
        {
            var blockScope = new LocalScope(CurrentScope);
            symbolTable.Scopes.Add(node, blockScope);
            EnterScope(blockScope);
        }

        public void Exit(BlockStatement node)
        {
            ExitScope();
        }

        public void Visit(VariableReferenceExpression node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(MethodInvocation node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(PrintStatement node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(ReturnStatement node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(AssertStatement node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(AssignmentStatement node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(IfStatement node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(WhileStatement node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(InstanceCreationExpression node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(UnaryNotExpression node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(BinaryOpExpression node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(BooleanLiteralExpression node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(ThisExpression node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(ArrayIndexingExpression node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(IntegerLiteralExpression node)
        {
            symbolTable.Scopes.Add(node, CurrentScope);
        }
    }
}
