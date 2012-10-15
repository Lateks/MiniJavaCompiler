using System.Collections.Generic;
using System.Diagnostics;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.SemanticAnalysis
{
    public class SymbolTableBuilder : INodeVisitor
    {
        private readonly SymbolTable _symbolTable;
        private readonly Program _syntaxTree;
        private readonly IErrorReporter _errorReporter;
        private bool _errorsFound;
        private bool _methodScopeDefinitionFailed;

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
            _methodScopeDefinitionFailed = false;

            _symbolTable = new SymbolTable();

            SetupGlobalScope(userDefinedTypes);
            _scopeStack = new Stack<IScope>();
            EnterScope(_symbolTable.GlobalScope);
        }

        private void SetupGlobalScope(IEnumerable<string> userDefinedTypes)
        {
            foreach (var type in MiniJavaInfo.BuiltIns)
            {
                var sym = new BuiltinTypeSymbol(type, _symbolTable.GlobalScope);
                _symbolTable.GlobalScope.Define(sym);
            }
            foreach (var type in userDefinedTypes)
            {
                var sym = new UserDefinedTypeSymbol(type, _symbolTable.GlobalScope);
                _symbolTable.GlobalScope.Define(sym);
            }
        }

        public bool BuildSymbolTable(out SymbolTable symbolTable)
        {
            _syntaxTree.Accept(this);
            symbolTable = _symbolTable;
            return !_errorsFound;
        }

        public void Visit(Program node)
        {
            _symbolTable.Scopes.Add(node, _symbolTable.GlobalScope);
        }

        public void Visit(ClassDeclaration node)
        {
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve<SimpleTypeSymbol>(node.Name);
            if (node.InheritedClass != null)
            {
                var inheritedType = (UserDefinedTypeSymbol)CurrentScope.Resolve<SimpleTypeSymbol>(node.InheritedClass);
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
            var typeSymbol = (UserDefinedTypeSymbol)CurrentScope.Resolve<SimpleTypeSymbol>(node.Name);
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
            if (_methodScopeDefinitionFailed) return;
            Debug.Assert(CurrentScope is IVariableScope);

            var variableType = CheckDeclaredType(node);
            var symbol = new VariableSymbol(node.Name, variableType, CurrentScope);
            if ((CurrentScope as IVariableScope).Define(symbol))
            {
                _symbolTable.Definitions.Add(symbol, node);
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
                _methodScopeDefinitionFailed = true; // set recovery flag (recover until method scope ends)
                return;
            }

            _symbolTable.Definitions.Add(methodSymbol, node);
            _symbolTable.Scopes.Add(node, methodSymbol);

            EnterScope(methodSymbol);
        }

        private IType CheckDeclaredType(Declaration node)
        {
            var nodeSimpleType = (SimpleTypeSymbol)_symbolTable.GlobalScope.Resolve<SimpleTypeSymbol>(node.Type);
            if (nodeSimpleType == null)
            {
                _errorReporter.ReportError("Unknown type '" + node.Type + "'.", node.Row, node.Col);
                _errorsFound = true;
                return null;
            }
            IType actualType = node.IsArray ?
                new MiniJavaArrayType(nodeSimpleType) :
                (IType)nodeSimpleType;

            return actualType;
        }

        private void ReportSymbolDefinitionError(Declaration node)
        {
            _errorReporter.ReportError("Symbol '" + node.Name + "' is already defined.", node.Row, node.Col);
            _errorsFound = true;
        }

        public void Exit(MethodDeclaration node)
        {
            if (_methodScopeDefinitionFailed)
            {
                _methodScopeDefinitionFailed = false; // recovery ends here
                return;
            }
            ExitScope();
        }

        public void Visit(BlockStatement node)
        {
            if (_methodScopeDefinitionFailed) return;
            var blockScope = new LocalScope(CurrentScope);
            _symbolTable.Scopes.Add(node, blockScope);
            EnterScope(blockScope);
        }

        public void Exit(BlockStatement node)
        {
            if (_methodScopeDefinitionFailed) return;
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

        public void Visit(BinaryOpExpression node)
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
            if (_methodScopeDefinitionFailed) return;
            _symbolTable.Scopes.Add(node, CurrentScope);
        }
    }
}
