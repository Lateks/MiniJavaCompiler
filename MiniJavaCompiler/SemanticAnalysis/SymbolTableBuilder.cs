using System;
using System.Collections.Generic;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.SemanticAnalysis
{
    public class SymbolTableBuilder : INodeVisitor
    {
        private class DefinitionException : Exception { }

        private readonly SymbolTable _symbolTable;
        private readonly Program _syntaxTree;
        private readonly IErrorReporter _errorReporter;
        private int _errors;

        private readonly Stack<IScope> _scopeStack;
        private IScope CurrentScope
        {
            get { return _scopeStack.Peek(); }
        }

        private void EnterScope(IScope scope)
        {
            _scopeStack.Push(scope);
        }

        private void ExitScope()
        {
            _scopeStack.Pop();
        }

        public SymbolTableBuilder(Program node, IEnumerable<string> types, IErrorReporter errorReporter)
        {
            _errorReporter = errorReporter;
            _errors = 0;
            _syntaxTree = node;

            _symbolTable = new SymbolTable();

            SetupGlobalScope(types);
            _scopeStack = new Stack<IScope>();
            EnterScope(_symbolTable.GlobalScope);
        }

        private void SetupGlobalScope(IEnumerable<string> types)
        {
            foreach (var type in MiniJavaInfo.BuiltIns)
            {
                Symbol.CreateAndDefine<BuiltinTypeSymbol>(type, _symbolTable.GlobalScope);
            }
            foreach (var type in types)
            {
                Symbol.CreateAndDefine<UserDefinedTypeSymbol>(type, _symbolTable.GlobalScope);
            }
        }

        public bool BuildSymbolTable(out SymbolTable symbolTable)
        {
            try
            {
                _syntaxTree.Accept(this);
                return _errors == 0;
            }
            catch (DefinitionException)
            {
                return false;
            }
            finally
            {
                symbolTable = _symbolTable;
            }
        }

        public void Visit(Program node)
        {
            _symbolTable.Scopes.Add(node, _symbolTable.GlobalScope);
        }

        public void Visit(ClassDeclaration node)
        {
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.Name);
            if (node.InheritedClass != null)
            {
                var inheritedType = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.InheritedClass);
                typeSymbol.SuperClass = inheritedType;
            }
            _symbolTable.Scopes.Add(node, typeSymbol);
            _symbolTable.Definitions.Add(typeSymbol, node);
            EnterScope(typeSymbol);
        }

        public void Exit(ClassDeclaration node)
        {
            ExitScope();
        }

        public void Visit(MainClassDeclaration node)
        {
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve<TypeSymbol>(node.Name);
            _symbolTable.Scopes.Add(node, typeSymbol);
            _symbolTable.Definitions.Add(typeSymbol, node);
            EnterScope(typeSymbol);
        }

        public void Exit(MainClassDeclaration node)
        {
            ExitScope();
        }

        public void Visit(VariableDeclaration node)
        {
            var variableType = CheckDeclaredType(node);
            var symbol = TryDefineSymbol<VariableSymbol>(node, variableType);
            if (symbol != null)
            {
                _symbolTable.Definitions.Add(symbol, node);
                _symbolTable.Scopes.Add(node, CurrentScope);
            }
        }

        public void Visit(MethodDeclaration node)
        {
            var methodReturnType = node.Type == "void" ? VoidType.GetInstance() : CheckDeclaredType(node);
            var methodSymbol = (MethodSymbol)TryDefineSymbol<MethodSymbol>(node, methodReturnType);
            if (methodSymbol == null)
            {
                throw new DefinitionException();
            }
            methodSymbol.IsStatic = node.IsStatic;

            _symbolTable.Definitions.Add(methodSymbol, node);
            _symbolTable.Scopes.Add(node, methodSymbol);

            EnterScope(methodSymbol);
        }

        private IType CheckDeclaredType(Declaration node)
        {
            Symbol nodeSimpleType = _symbolTable.GlobalScope.Resolve<TypeSymbol>(node.Type);
            if (nodeSimpleType == null)
            {
                _errorReporter.ReportError("Unknown type '" + node.Type + "'.", node.Row, node.Col);
                _errors++;
                return null;
            }
            IType actualType = node.IsArray ?
                new MiniJavaArrayType((ISimpleType)nodeSimpleType) :
                (IType)nodeSimpleType;

            return actualType;
        }

        private Symbol TryDefineSymbol<TSymbolType>(Declaration node, IType symbolType)
            where TSymbolType : Symbol
        {
            var symbol = Symbol.CreateAndDefine<TSymbolType>(
                node.Name, symbolType, CurrentScope);

            if (symbol == null)
            {
                _errorReporter.ReportError("Symbol '" + node.Name + "' is already defined.", node.Row, node.Col);
                _errors++;
                return null;
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
            _symbolTable.Scopes.Add(node, blockScope);
            EnterScope(blockScope);
        }

        public void Exit(BlockStatement node)
        {
            ExitScope();
        }

        public void Visit(IfStatement node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(VariableReferenceExpression node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(MethodInvocation node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(PrintStatement node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(ReturnStatement node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(AssertStatement node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(AssignmentStatement node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(WhileStatement node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(InstanceCreationExpression node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(UnaryOperatorExpression node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(BinaryOpExpression node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(BooleanLiteralExpression node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(ThisExpression node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(ArrayIndexingExpression node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }

        public void Visit(IntegerLiteralExpression node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }
    }
}
