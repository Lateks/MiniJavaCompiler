﻿using System.Collections.Generic;
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
    public partial class SymbolTableBuilder : NodeVisitorBase
    {
        public enum ExitCode
        {
            Success,
            NonFatalError,
            FatalError
        }
        private readonly GlobalScope _globalScope;
        private readonly Program _syntaxTree;
        private readonly IErrorReporter _errorReporter;
        private readonly Stack<IScope> _scopeStack;
        private IEnumerable<string> _scalarTypeNames;
        private bool _error;

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
            _globalScope = new GlobalScope();

            _scopeStack = new Stack<IScope>();
        }

        public ExitCode BuildSymbolTable()
        {
            bool fatalError;
            if (GetTypes())
            {
                SetupGlobalScope();
                EnterScope(_globalScope);
                _syntaxTree.Accept(this);
                fatalError = CheckForCyclicInheritance();
            }
            else
            {
                fatalError = true;
            }

            if (fatalError)
            {
                return ExitCode.FatalError;
            }
            else if (_error)
            {
                return ExitCode.NonFatalError;
            }
            else
            {
                return ExitCode.Success;
            }
        }

        private bool GetTypes()
        {
            bool success = new TypeSetBuilder(_syntaxTree, _errorReporter).BuildTypeSet(out _scalarTypeNames);
            return success;
        }

        private void SetupGlobalScope()
        {
            SetUpBuiltInTypes();
            SetUpUserDefinedTypes();
        }

        private void SetUpUserDefinedTypes()
        {
            foreach (var typeName in _scalarTypeNames)
            {
                // Note: classes are set up without superclass information because
                // all class symbols need to be created before superclass relations
                // can be set.
                var sym = TypeSymbol.MakeScalarTypeSymbol(typeName, _globalScope);
                _globalScope.Define(sym);
            }
        }

        private void SetUpBuiltInTypes()
        {
            foreach (var type in MiniJavaInfo.BuiltInTypes())
            {
                var sym = TypeSymbol.MakeScalarTypeSymbol(type, _globalScope);
                _globalScope.Define(sym);
            }
            SetUpArrayBase();
        }

        private void SetUpArrayBase()
        {
            var anyTypeSym = TypeSymbol.MakeScalarTypeSymbol(
                MiniJavaInfo.AnyType, _globalScope);
            _globalScope.Define(anyTypeSym);
            var arrayBaseSym = TypeSymbol.MakeArrayTypeSymbol(
                (ScalarType)anyTypeSym.Type, _globalScope);
            _globalScope.Define(arrayBaseSym);

            var intType = _globalScope.ResolveType(MiniJavaInfo.IntType).Type;
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
            var dependentClasses = new List<string>();
            foreach (var typeName in _globalScope.UserDefinedTypeNames) {
                var typeSymbol = _globalScope.ResolveType(typeName);
                if (ClassDependsOnSelf((ScalarType)typeSymbol.Type))
                {
                    var node = (SyntaxElement) typeSymbol.Declaration;
                    ReportError(
                        ErrorTypes.CyclicInheritance,
                        String.Format("Cyclic inheritance involving {0}.",
                        typeSymbol.Type.Name), node);
                    cyclicInheritanceFound = true;
                }
            }
            return cyclicInheritanceFound;
        }

        // Check implemented according to description in:
        // http://docs.oracle.com/javase/specs/jls/se7/html/jls-8.html#jls-8.1.4
        private bool ClassDependsOnSelf(ScalarType classSymbol)
        {
            ScalarType currentClass = classSymbol;
            while (currentClass.SuperType != null && currentClass.SuperType != classSymbol)
            {
                currentClass = currentClass.SuperType;
            }
            return currentClass.SuperType == classSymbol;
        }

        public override void Visit(Program node)
        {
            node.Scope = _globalScope;
        }

        // Detects references to unknown types in class inheritance
        // declarations. If the inherited type cannot be resolved,
        // InheritedType will remain null (instead of being set
        // to ErrorType).
        public override void Visit(ClassDeclaration node)
        {   // Resolve inheritance relationships.
            var typeSymbol = CurrentScope.ResolveType(node.Name);
            if (node.InheritedClassName != null)
            {
                var inheritedType = (TypeSymbol) CurrentScope.ResolveType(node.InheritedClassName);
                if (inheritedType == null)
                {
                    ReportTypeNameError(node.InheritedClassName, node);
                }
                else
                {
                    typeSymbol.SuperClass = inheritedType;
                }
            }
            node.Scope = typeSymbol.Scope;
            typeSymbol.Declaration = node;
            EnterScope(typeSymbol.Scope);
        }

        public override void Exit(ClassDeclaration node)
        {
            ExitScope();
        }

        // Detects possible duplicate declaration of a variable identifier.
        public override void Visit(VariableDeclaration node)
        {
            Debug.Assert(CurrentScope is IVariableScope);

            node.Type = CheckDeclaredType(node);
            var variableSymbol = new VariableSymbol(node.Name, node.Type, CurrentScope);
            if ((CurrentScope as IVariableScope).Define(variableSymbol))
            {
                variableSymbol.Declaration = node;
                node.Scope = CurrentScope;
            }
            else
            {
                ReportSymbolDefinitionError(node);
            }
        }

        // Detects possible duplicate declaration of a method identifier.
        // A dummy scope is made to stand in for the method scope.
        public override void Visit(MethodDeclaration node)
        {
            Debug.Assert(CurrentScope is IMethodScope);

            node.Type = CheckDeclaredType(node);
            var methodScope = (IMethodScope) CurrentScope;
            
            // Note: the symbol is stored on the node even if the attempt
            // to define it fails. This feature can be used in the type
            // checking phase to e.g. check return types for even methods
            // that could not be defined due to a name clash.
            node.Symbol = new MethodSymbol(node.Name, node.Type, methodScope, node.IsStatic);
            IScope scope = node.Symbol.Scope;
            if (!methodScope.Define(node.Symbol))
            {
                ReportSymbolDefinitionError(node);
                scope = new ErrorScope(CurrentScope); // Make an error scope to stand in for the method scope for purposes of recovery.
            }                                         // (Both are IVariableScopes.)

            node.Symbol.Declaration = node;
            node.Scope = scope;

            EnterScope(scope);
        }

        public override void Visit(InstanceCreationExpression node)
        {   // Instance creation expressions are checked here instead of
            // the type checking phase because they might create an array
            // of a type not previously used, so this array type needs to
            // be added to the symbol table. This method does not detect
            // errors.
            if (node.CreatedTypeName != MiniJavaInfo.VoidType)
            {
                var scalarTypeSymbol = _globalScope.ResolveType(node.CreatedTypeName);
                if (scalarTypeSymbol != null && node.IsArrayCreation &&
                    _globalScope.ResolveArrayType(node.CreatedTypeName) == null)
                {
                    DefineArrayType((ScalarType)scalarTypeSymbol.Type);
                }
            }
            HandleExpressionOrStatementNode(node);
        }

        public override void Exit(MethodDeclaration node)
        {
            ExitScope();
        }

        public override void Visit(BlockStatement node)
        {
            Debug.Assert(CurrentScope is IVariableScope);
            var blockScope = new LocalScope((IVariableScope) CurrentScope);
            node.Scope = blockScope;
            EnterScope(blockScope);
        }

        public override void Exit(BlockStatement node)
        {
            ExitScope();
        }

        public override void Exit(IfStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(VariableReferenceExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(MethodInvocation node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(PrintStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(ReturnStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(AssertStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(AssignmentStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(WhileStatement node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(UnaryOperatorExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(BinaryOperatorExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(BooleanLiteralExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(ThisExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(ArrayIndexingExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        public override void Visit(IntegerLiteralExpression node)
        {
            HandleExpressionOrStatementNode(node);
        }

        private void HandleExpressionOrStatementNode(ISyntaxTreeNode node)
        {
            node.Scope = CurrentScope;
        }

        private void ReportTypeNameError(string typeName, SyntaxElement node)
        {
            ReportError(ErrorTypes.TypeReference,
                String.Format("Unknown type {0}.", typeName), node);
        }

        private void ReportSymbolDefinitionError(Declaration node)
        {
            string errorMessage = String.Format("Symbol {0} is already defined.", node.Name);
            ReportError(ErrorTypes.ConflictingDefinitions, errorMessage, node);
        }

        private void ReportError(ErrorTypes type, string message, SyntaxElement node)
        {
            _errorReporter.ReportError(type, message, node);
            _error = true;
        }

        private IType CheckDeclaredType(Declaration node)
        {
            IType declaredType;
            if (node.TypeName == MiniJavaInfo.VoidType)
            {
                if (node is VariableDeclaration)
                {
                    ReportError(ErrorTypes.TypeReference, "Illegal type void in variable declaration.", node);
                    declaredType = ErrorType.GetInstance();
                }
                else if (node.IsArray)
                {
                    ReportError(ErrorTypes.TypeReference, "Illegal type void for array elements.", node);
                    declaredType = ErrorType.GetInstance();
                }
                else // declaration is a void method
                {
                    declaredType = VoidType.GetInstance();
                }
            }
            else
            {
                var nodeScalarTypeSymbol = _globalScope.ResolveType(node.TypeName);
                if (nodeScalarTypeSymbol == null)
                {   // Note: this error is also reported when a void type is encountered
                    // for something other than a method declaration.
                    ReportTypeNameError(node.TypeName, node);
                    declaredType = ErrorType.GetInstance();
                }
                else
                {
                    declaredType = BuildType(node, (ScalarType)nodeScalarTypeSymbol.Type);
                }
            }
            return declaredType;
        }

        private IType BuildType(Declaration node, ScalarType nodeScalarType)
        {
            IType actualType;
            if (node.IsArray)
            {
                var arraySymbol = _globalScope.ResolveArrayType(node.TypeName);
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
            var sym = TypeSymbol.MakeArrayTypeSymbol(nodeScalarType, _globalScope);
            sym.SuperClass = _globalScope.ResolveArrayType(MiniJavaInfo.AnyType);
            _globalScope.Define(sym);
            return sym.Type;
        }
    }
}
