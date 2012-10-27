using System.Collections.Generic;
using System.Diagnostics;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.Frontend.SemanticAnalysis
{
    public class SymbolTableBuilder : INodeVisitor
    {
        private readonly SymbolTable _symbolTable;
        private readonly Program _syntaxTree;
        private readonly IErrorReporter _errorReporter;
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

            _symbolTable = new SymbolTable();

            SetupGlobalScope(userDefinedTypes);
            _scopeStack = new Stack<IScope>();
        }

        public SymbolTable BuildSymbolTable()
        {
            EnterScope(_symbolTable.GlobalScope);
            _syntaxTree.Accept(this);

            if (_errorsFound)
            {
                throw new CompilationFailed();
            }
            return _symbolTable;
        }

        private void SetupGlobalScope(IEnumerable<string> userDefinedTypes)
        {
            foreach (var type in MiniJavaInfo.BuiltInTypes())
            {
                var sym = new BuiltInTypeSymbol(type, _symbolTable.GlobalScope);
                _symbolTable.GlobalScope.Define(sym);
            }
            foreach (var type in userDefinedTypes)
            {
                // Note: classes are set up without superclass information because
                // all class symbols need to be created before superclass relations
                // can be set.
                var sym = new UserDefinedTypeSymbol(type, _symbolTable.GlobalScope);
                _symbolTable.GlobalScope.Define(sym);
            }
        }

        public void Visit(Program node)
        {
            _symbolTable.Scopes.Add(node, _symbolTable.GlobalScope);
        }

        public void Visit(ClassDeclaration node)
        {   // Resolve inheritance relationships.
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.ResolveType(node.Name);
            if (node.InheritedClass != null)
            {
                var inheritedType = (UserDefinedTypeSymbol)CurrentScope.ResolveType(node.InheritedClass);
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
        {   // Main class cannot inherit.
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.ResolveType(node.Name);
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
                var recoveryScope = new LocalScope(CurrentScope); // Make a local scope to stand in for the method scope for purposes of recovery.
                EnterScope(recoveryScope);                        // (Both are IVariableScopes. Note: normally a class does not hold local (block) scopes.)
                return;
            }

            _symbolTable.Definitions.Add(methodSymbol, node);
            _symbolTable.Scopes.Add(node, methodSymbol);

            EnterScope(methodSymbol);
        }

        private IType CheckDeclaredType(Declaration node)
        {
            var nodeSimpleType = (SimpleTypeSymbol)_symbolTable.GlobalScope.ResolveType(node.Type);
            if (nodeSimpleType == null)
            {
                // Note: this error is also reported when a void type is encountered
                // for something other than a method declaration.
                _errorReporter.ReportError("Unknown type '" + node.Type + "'.", node.Row, node.Col);
                _errorsFound = true;
                return null;
            }
            IType actualType = node.IsArray
                ? MiniJavaArrayType.OfType(nodeSimpleType)
                : (IType)nodeSimpleType;

            return actualType;
        }

        private void ReportSymbolDefinitionError(Declaration node)
        {
            _errorReporter.ReportError("Symbol '" + node.Name + "' is already defined.", node.Row, node.Col);
            _errorsFound = true;
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
