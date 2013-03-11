using System.Collections.Generic;
using System.Diagnostics;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.SymbolTable.Scopes;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;

namespace MiniJavaCompiler.FrontEnd.SemanticAnalysis
{
    public partial class SymbolTableBuilder : INodeVisitor
    {
        private readonly SymbolTable _symbolTable;
        private readonly Program _syntaxTree;
        private readonly IErrorReporter _errorReporter;
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

        public SymbolTableBuilder(Program node, IErrorReporter errorReporter)
        {
            _errorReporter = errorReporter;
            _syntaxTree = node;

            _symbolTable = new SymbolTable();

            _scopeStack = new Stack<IScope>();
        }

        public SymbolTable BuildSymbolTable()
        {
            bool fatalError;
            if (GetTypes())
            {
                SetupGlobalScope();
                EnterScope(_symbolTable.GlobalScope);
                _syntaxTree.Accept(this);
                fatalError = CheckForCyclicInheritance();
            }
            else
            {
                fatalError = true;
            }

            if (fatalError)
            {
                throw new CompilationError();
            }
            return _symbolTable;
        }

        private bool GetTypes()
        {
            IEnumerable<string> types;
            bool success = new TypeSetBuilder(_syntaxTree, _errorReporter).BuildTypeSet(out types);
            _symbolTable.ScalarTypeNames = types;
            return success;
        }

        private void SetupGlobalScope()
        {
            SetUpBuiltInTypes();
            SetUpUserDefinedTypes();
        }

        private void SetUpUserDefinedTypes()
        {
            foreach (var typeName in _symbolTable.ScalarTypeNames)
            {
                // Note: classes are set up without superclass information because
                // all class symbols need to be created before superclass relations
                // can be set.
                var sym = TypeSymbol.MakeScalarTypeSymbol(typeName, _symbolTable.GlobalScope);
                _symbolTable.GlobalScope.Define(sym);
            }
        }

        private void SetUpBuiltInTypes()
        {
            foreach (var type in MiniJavaInfo.BuiltInTypes())
            {
                var sym = TypeSymbol.MakeScalarTypeSymbol(type, _symbolTable.GlobalScope);
                _symbolTable.GlobalScope.Define(sym);
            }
            SetUpArrayBase();
        }

        private void SetUpArrayBase()
        {
            var anyTypeSym = TypeSymbol.MakeScalarTypeSymbol(MiniJavaInfo.AnyType, _symbolTable.GlobalScope);
            _symbolTable.GlobalScope.Define(anyTypeSym);
            var arrayBaseSym = TypeSymbol.MakeArrayTypeSymbol((ScalarType)anyTypeSym.Type, _symbolTable.GlobalScope);
            _symbolTable.GlobalScope.Define(arrayBaseSym);

            var intType = _symbolTable.GlobalScope.ResolveType(MiniJavaInfo.IntType).Type;
            var arrayBaseScope = (IMethodScope)arrayBaseSym.Scope;

            foreach (string methodName in MiniJavaInfo.ArrayMethodNames())
            {
                var methodSym = new MethodSymbol(methodName, intType, arrayBaseScope, false);
                arrayBaseScope.Define(methodSym);
            }
        }

        private bool CheckForCyclicInheritance()
        {
            bool cyclicInheritanceFound = false;
            foreach (var typeName in _symbolTable.ScalarTypeNames) {
                var typeSymbol = _symbolTable.ResolveTypeName(typeName);
                if (classDependsOnSelf((ScalarType)typeSymbol.Type))
                {
                    var node = (SyntaxElement) _symbolTable.Definitions[typeSymbol];
                    ReportError(String.Format("Class {0} depends on itself.", typeSymbol.Type.Name), node.Row, node.Col);
                    cyclicInheritanceFound = true;
                }
            }
            return cyclicInheritanceFound;
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
            var typeSymbol = CurrentScope.ResolveType(node.Name);
            if (node.InheritedClassName != null)
            {
                var inheritedType = (TypeSymbol) CurrentScope.ResolveType(node.InheritedClassName);
                if (inheritedType == null)
                {
                    ReportError(String.Format("Unknown type '{0}'.", node.InheritedClassName), node.Row, node.Col);
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

            var methodReturnType = node.Type == MiniJavaInfo.VoidType ? VoidType.GetInstance() : CheckDeclaredType(node);
            var methodScope = (IMethodScope) CurrentScope;
            var methodSymbol = new MethodSymbol(node.Name, methodReturnType, methodScope, node.IsStatic);
            IScope scope = methodSymbol.Scope;
            if (!methodScope.Define(methodSymbol))
            {
                ReportSymbolDefinitionError(node);
                scope = new ErrorScope(CurrentScope); // Make an error scope to stand in for the method scope for purposes of recovery.
            }                                         // (Both are IVariableScopes.)

            _symbolTable.Definitions.Add(methodSymbol, node);
            _symbolTable.Scopes.Add(node, scope);

            EnterScope(scope);
        }

        private IType CheckDeclaredType(Declaration node)
        {
            var nodeScalarTypeSymbol = _symbolTable.ResolveTypeName(node.Type);
            if (nodeScalarTypeSymbol == null)
            {
                // Note: this error is also reported when a void type is encountered
                // for something other than a method declaration.
                ReportError(String.Format("Unknown type '{0}'.", node.Type), node.Row, node.Col);
                return ErrorType.GetInstance();
            }
            return BuildType(node, (ScalarType) nodeScalarTypeSymbol.Type);
        }

        private IType BuildType(Declaration node, ScalarType nodeScalarType)
        {
            IType actualType;
            if (node.IsArray)
            {
                var arraySymbol = _symbolTable.ResolveTypeName(node.Type, node.IsArray);
                if (arraySymbol == null)
                {
                    actualType = DefineArrayType(nodeScalarType);
                }
                else
                {
                    actualType = arraySymbol.Type;
                }
            }
            else
            {
                actualType = nodeScalarType;
            }
            return actualType;
        }

        private IType DefineArrayType(ScalarType nodeScalarType)
        {
            var sym = TypeSymbol.MakeArrayTypeSymbol(nodeScalarType, _symbolTable.GlobalScope);
            sym.SuperClass = _symbolTable.ResolveTypeName(MiniJavaInfo.AnyType, true);
            _symbolTable.GlobalScope.Define(sym);
            return sym.Type;
        }

        public void Visit(InstanceCreationExpression node)
        {
            var scalarTypeSymbol = _symbolTable.ResolveTypeName(node.CreatedTypeName);
            if (scalarTypeSymbol == null)
            {
                ReportError(String.Format("Unknown type '{0}'.", node.CreatedTypeName), node.Row, node.Col);
            }
            else if (node.IsArrayCreation && _symbolTable.ResolveTypeName(node.CreatedTypeName, node.IsArrayCreation) == null)
            {
                DefineArrayType((ScalarType) scalarTypeSymbol.Type);
            }
            HandleExpressionOrStatementNode(node);
        }

        private void ReportSymbolDefinitionError(Declaration node)
        {
            string errorMessage = String.Format("Symbol '{0}' is already defined.", node.Name);
            ReportError(errorMessage, node.Row, node.Col);
        }

        private void ReportError(string message, int row, int col)
        {
            _errorReporter.ReportError(message, row, col);
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

        public void VisitAfterCondition(IfStatement node) { }

        public void VisitAfterThenBranch(IfStatement node) { }

        public void Exit(IfStatement node)
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

        public void VisitAfterBody(WhileStatement node) { }

        public void Exit(WhileStatement node) { }

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
