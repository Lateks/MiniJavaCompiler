using System.Collections.Generic;
using System.Diagnostics;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.SymbolTable.Scopes;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;

namespace MiniJavaCompiler.Frontend.SemanticAnalysis
{
    public class SymbolTableBuilder : INodeVisitor
    {
        private readonly SymbolTable _symbolTable;
        private readonly Program _syntaxTree;
        private readonly IErrorReporter _errorReporter;
        private readonly IEnumerable<string> _userDefinedTypes;
        private bool _errorsFound;

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

        public SymbolTableBuilder(Program node, IEnumerable<string> userDefinedTypes, IErrorReporter errorReporter)
        {
            _errorReporter = errorReporter;
            _errorsFound = false;
            _syntaxTree = node;
            _userDefinedTypes = userDefinedTypes;

            _symbolTable = new SymbolTable();

            SetupGlobalScope();
            _scopeStack = new Stack<IScope>();
        }

        public SymbolTable BuildSymbolTable()
        {
            EnterScope(_symbolTable.GlobalScope);
            _syntaxTree.Accept(this);
            CheckForCyclicInheritance();

            if (_errorsFound)
            {
                throw new CompilationError();
            }
            return _symbolTable;
        }

        private void SetupGlobalScope()
        {
            foreach (var type in MiniJavaInfo.BuiltInTypes())
            {
                var sym = new TypeSymbol(type, _symbolTable.GlobalScope, TypeSymbolKind.Scalar);
                _symbolTable.GlobalScope.Define(sym);
            }
            foreach (var type in _userDefinedTypes)
            {
                // Note: classes are set up without superclass information because
                // all class symbols need to be created before superclass relations
                // can be set.
                var sym = new TypeSymbol(type, _symbolTable.GlobalScope, TypeSymbolKind.Scalar);
                _symbolTable.GlobalScope.Define(sym);
            }
        }

        private void CheckForCyclicInheritance()
        {
            foreach (var typeName in _userDefinedTypes) {
                var type = (ScalarType) _symbolTable.ResolveType(typeName);
                if (classDependsOnSelf(type))
                {
                    // TODO: should get actual row and column numbers here.
                    ReportError(String.Format("Class {0} depends on itself.", type.Name), 0, 0);
                }
            }
        }

        // http://docs.oracle.com/javase/specs/jls/se7/html/jls-8.html#jls-8.1.4
        private bool classDependsOnSelf(ScalarType classSymbol)
        {
            ScalarType currentClass = classSymbol;
            while (currentClass.SuperType != null && currentClass.SuperType != classSymbol)
            {
                currentClass = currentClass.SuperType;
            }
            return currentClass.SuperType != null;
        }

        public void Visit(Program node)
        {
            _symbolTable.Scopes.Add(node, _symbolTable.GlobalScope);
        }

        public void Visit(ClassDeclaration node)
        {   // Resolve inheritance relationships.
            var typeSymbol = (TypeSymbol) CurrentScope.ResolveType(node.Name);
            if (node.InheritedClass != null)
            {
                var inheritedType = (TypeSymbol) CurrentScope.ResolveType(node.InheritedClass);
                if (inheritedType == null)
                {
                    ReportError(String.Format("Unknown type '{0}'.", node.InheritedClass), node.Row, node.Col);
                }
                else
                {
                    typeSymbol.SuperClass = inheritedType;
                }
            }
            _symbolTable.Scopes.Add(node, typeSymbol.Scope);
            _symbolTable.Definitions.Add(typeSymbol, node);
            EnterScope(typeSymbol.Scope);
        }

        public void Exit(ClassDeclaration node)
        {
            ExitScope();
        }

        public void Visit(MainClassDeclaration node)
        {   // Main class cannot inherit.
            var typeSymbol = CurrentScope.ResolveType(node.Name);
            _symbolTable.Scopes.Add(node, typeSymbol.Scope);
            _symbolTable.Definitions.Add(typeSymbol, node);
            EnterScope(typeSymbol.Scope);
        }

        public void Exit(MainClassDeclaration node)
        {
            ExitScope();
        }

        public void Visit(VariableDeclaration node)
        {
            Debug.Assert(CurrentScope is IVariableScope);

            var variableType = CheckDeclaredType(node);
            var variableSymbol = new VariableSymbol(node.Name, variableType, CurrentScope);
            if ((CurrentScope as IVariableScope).Define(variableSymbol))
            {
                _symbolTable.Definitions.Add(variableSymbol, node);
                _symbolTable.Scopes.Add(node, CurrentScope);
            }
            else
            {
                ReportSymbolDefinitionError(node);
            }
        }

        public void Visit(MethodDeclaration node)
        {
            Debug.Assert(CurrentScope is IMethodScope);

            var methodReturnType = node.Type == "void" ? VoidType.GetInstance() : CheckDeclaredType(node);
            var methodScope = (IMethodScope) CurrentScope;
            var methodSymbol = new MethodSymbol(node.Name, methodReturnType, methodScope, node.IsStatic);
            if (!methodScope.Define(methodSymbol))
            {
                ReportSymbolDefinitionError(node);
                var recoveryScope = new ErrorScope(CurrentScope); // Make an error scope to stand in for the method scope for purposes of recovery.
                EnterScope(recoveryScope);                        // (Both are IVariableScopes.)
                return;
            }

            _symbolTable.Definitions.Add(methodSymbol, node);
            _symbolTable.Scopes.Add(node, methodSymbol.Scope);

            EnterScope(methodSymbol.Scope);
        }

        private IType CheckDeclaredType(Declaration node)
        {
            var nodeScalarTypeSymbol = _symbolTable.GlobalScope.ResolveType(node.Type);
            if (nodeScalarTypeSymbol == null)
            {
                // Note: this error is also reported when a void type is encountered
                // for something other than a method declaration.
                ReportError(String.Format("Unknown type '{0}'.", node.Type), node.Row, node.Col);
                return null;
            }
            IType actualType = node.IsArray
                ? new ArrayType((ScalarType) nodeScalarTypeSymbol.Type)
                : nodeScalarTypeSymbol.Type;

            return actualType;
        }

        private void ReportSymbolDefinitionError(Declaration node)
        {
            string errorMessage = String.Format("Symbol '{0}' is already defined.", node.Name);
            ReportError(errorMessage, node.Row, node.Col);
        }

        private void ReportError(string message, int row, int col)
        {
            _errorReporter.ReportError(message, row, col);
            _errorsFound = true;
        }

        public void Exit(MethodDeclaration node)
        {
            ExitScope();
        }

        public void Visit(BlockStatement node)
        {
            Debug.Assert(CurrentScope is IVariableScope);
            var blockScope = new LocalScope((IVariableScope) CurrentScope);
            _symbolTable.Scopes.Add(node, blockScope);
            EnterScope(blockScope);
        }

        public void Exit(BlockStatement node)
        {
            ExitScope();
        }

        public void Visit(IfStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(VariableReferenceExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(MethodInvocation node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(PrintStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(ReturnStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(AssertStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(AssignmentStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(WhileStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(InstanceCreationExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(UnaryOperatorExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(BinaryOperatorExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(BooleanLiteralExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(ThisExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(ArrayIndexingExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public void Visit(IntegerLiteralExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        private void HandleExpressionOrStatementNode(ISyntaxTreeNode node)
        {
            _symbolTable.Scopes.Add(node, CurrentScope);
        }
    }
}
