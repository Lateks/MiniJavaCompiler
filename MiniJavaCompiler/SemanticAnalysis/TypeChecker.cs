using System;
using System.Collections.Generic;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.SymbolTable;

namespace MiniJavaCompiler.SemanticAnalysis
{
    public class TypeError : Exception
    {
        public TypeError(string message) : base(message) { }
    }
    public class ReferenceError : Exception
    {
        public ReferenceError(string message) : base(message) { }
    }

    public class TypeChecker : INodeVisitor
    {
        private readonly SymbolTable _symbolTable;
        private readonly Stack<IType> _operandTypes;
        private readonly Stack<IType> _returnTypes;
        private readonly Program _programRoot;

        public TypeChecker(Program program, SymbolTable symbolTable)
        {
            _programRoot = program;
            _symbolTable = symbolTable;
            _operandTypes = new Stack<IType>();
            _returnTypes = new Stack<IType>();
        }

        // Throws an exception if there is a problem with types or references.
        public void CheckTypesAndReferences()
        {
            _programRoot.Accept(this);
        }

        public void Visit(Program node) { }

        public void Visit(ClassDeclaration node) { }

        public void Visit(MainClassDeclaration node) { }

        public void Visit(VariableDeclaration node) { }

        public void Visit(MethodDeclaration node) { }

        public void Visit(PrintStatement node)
        { // argument must be a basic type (int or boolean)
            var type = _operandTypes.Pop();
            if (!(type is BuiltinTypeSymbol))
            {
                throw new TypeError(String.Format("Cannot convert type '{0}' to string near row {1}, col {2}.",
                    type.Name, node.Row, node.Col));
            }
        }

        public void Visit(ReturnStatement node)
        {
            var argType = _operandTypes.Pop();
            _returnTypes.Push(argType); 
        }

        public void Visit(BlockStatement node) { }

        public void Visit(AssertStatement node)
        {
            CheckSingleBooleanArgument(node);
        }

        private void CheckSingleBooleanArgument(SyntaxElement node)
        {
            var argType = _operandTypes.Pop();
            if (!(argType is BuiltinTypeSymbol && argType.Name == MiniJavaInfo.IntType))
            {
                throw new TypeError(String.Format("Cannot convert expression of type '{0}' to boolean near row {1}, col {2}.",
                                                  argType.Name, node.Row, node.Col));
            }
        }

        public void Visit(AssignmentStatement node)
        { // the type of right hand side must match the left hand side
          // and left hand side needs to be an lvalue (in MiniJava
          // that practically means an identifier that references a
          // variable)
            var lhsType = _operandTypes.Pop();
            var rhsType = _operandTypes.Pop();
            if (node.LeftHandSide is VariableReferenceExpression)
            {
                if (lhsType != rhsType)
                {
                    throw new TypeError(String.Format("Cannot assign expression of type {0} to variable of type {1} " +
                        "near row {2}, col {3}.", rhsType.Name, lhsType.Name, node.Row, node.Col));
                }
            }
            else
            {
                throw new TypeError(String.Format("Expression is not assignable near row {0}, col {1}.",
                    node.Row, node.Col));
            }
        }

        public void Visit(IfStatement node)
        {
            CheckSingleBooleanArgument(node);
        }

        public void Visit(WhileStatement node)
        {
            CheckSingleBooleanArgument(node);
        }

        public void Visit(MethodInvocation node)
        {
            var expressionType = _operandTypes.Pop();
            MethodSymbol method;
            if (node.MethodOwner is ThisExpression)
            {
                method = (MethodSymbol)_symbolTable.Scopes[node].Resolve<MethodSymbol>(node.MethodName);
            }
            else if (node.MethodOwner is VariableReferenceExpression ||
                node.MethodOwner is MethodInvocation ||
                node.MethodOwner is InstanceCreationExpression ||
                node.MethodOwner is ArrayIndexingExpression)
            {
                if (expressionType is MiniJavaArrayType)
                {
                    if (MiniJavaArrayType.IsPredefinedArrayAction(node.MethodName))
                    { // TODO: ask type from array
                        _operandTypes.Push(_symbolTable.ResolveType(MiniJavaInfo.IntType));
                        return;
                    }
                    throw new ReferenceError(String.Format("Cannot call method '{0}' for an array near row {1}, col {2}.",
                        node.MethodName, node.Row, node.Col));
                }
                else if (expressionType is BuiltinTypeSymbol)
                {
                    throw new ReferenceError(String.Format("Cannot call method '{0}' on builtin type {1} near row {2}, col {3}.",
                        node.MethodName, expressionType.Name, node.Row, node.Col));
                }
                else // expression evaluates to an object of a user defined type
                {
                    var classDefinition = _symbolTable.Definitions[(Symbol) expressionType];
                    method = (MethodSymbol)_symbolTable.Scopes[classDefinition].Resolve<MethodSymbol>(node.MethodName);
                }
            }
            else
            {
                throw new ReferenceError(String.Format("Cannot call a method on expression near row {0}, col {1}.",
                    node.Row, node.Col));
            }
            ValidateMethodCall(method, node);
            _operandTypes.Push(method.Type); // return type, can be void
        }

        private void ValidateMethodCall(MethodSymbol method, MethodInvocation node)
        {
            if (method == null)
            {
                throw new ReferenceError(String.Format("Cannot resolve symbol '{0}' near row {1}, col {2}.",
                    node.MethodName, node.Row, node.Col));
            }
            var methodDecl = (MethodDeclaration)_symbolTable.Definitions[method];
            var numCallParams = node.CallParameters.Count;
            var numFormalParams = methodDecl.Formals.Count;
            if (numCallParams != numFormalParams)
            {
                throw new ReferenceError(String.Format("Wrong number of arguments to method '{0}' near row {1}, col {2}.",
                    node.MethodName, node.Row, node.Col));
            }

            var callParams = new Stack<IType>();
            for (int i = 0; i < numCallParams; i++)
            {
                callParams.Push(_operandTypes.Pop());
            }
            foreach (var formalParameter in methodDecl.Formals)
            {
                var callParamType = callParams.Pop();
                var formalParamType = _symbolTable.ResolveType(formalParameter.Type);
                if (formalParamType != callParamType)
                {
                    throw new TypeError(String.Format(
                        "Wrong type of argument to method '{0}' near row {1}, col {2}. Expected {3} but got {4}.",
                        node.MethodName, node.Row, node.Col, formalParamType.Name, callParamType.Name));
                }
            }
        }

        public void Visit(InstanceCreationExpression node)
        { // Check that type reference is valid.
          // If creating an array, check that size evaluates to int.
          // Take note of created type.
            var createdType = (ISimpleType) _symbolTable.ResolveType(node.Type);
            if (createdType == null)
            {
                throw new ReferenceError(String.Format("Cannot resolve symbol {0} near row {1}, col {2}.",
                    node.Type, node.Row, node.Col));
            }
            var arraySizeType = _operandTypes.Pop();
            if (node.IsArrayCreation)
            {
                if (arraySizeType != _symbolTable.ResolveType(MiniJavaInfo.IntType))
                {
                    throw new ReferenceError(String.Format("Array size must be numeric near row {0}, col {1}.",
                                                           node.Row, node.Col));
                }
                _operandTypes.Push(new MiniJavaArrayType(createdType));
            }
            else
            {
                _operandTypes.Push(createdType);
            }
        }

        public void Visit(UnaryOperatorExpression node)
        {
            var op = MiniJavaInfo.Operators[node.Operator];
            var expectedArgType = _symbolTable.ResolveType(op.OperandType);
            var actualArgType = _operandTypes.Pop();
            if (expectedArgType != actualArgType)
            {
                throw new TypeError(String.Format("Cannot use operator {0} on operand of type {1} on row {2}, col {3}.",
                    node.Operator, actualArgType.Name, node.Row, node.Col));
            }
            _operandTypes.Push(_symbolTable.ResolveType(op.ResultType));
        }

        public void Visit(BinaryOpExpression node)
        { // both arguments (lhs and rhs) must match operator type
            var leftOperandType = _operandTypes.Pop();
            var rightOperandType = _operandTypes.Pop();
            var op = MiniJavaInfo.Operators[node.Operator];
            if (op.OperandType == MiniJavaInfo.AnyBuiltin)
            {
                if (!(leftOperandType is BuiltinTypeSymbol && rightOperandType is BuiltinTypeSymbol))
                {
                    throw new TypeError(String.Format("Cannot apply operator {0} on arguments of type {1} and {2} near row {3}, col {4}.",
                        node.Operator, leftOperandType.Name, rightOperandType.Name, node.Row, node.Col));
                }
            }
            else
            {
                var expectedOpType = _symbolTable.ResolveType(op.OperandType);
                if (leftOperandType != expectedOpType || rightOperandType != expectedOpType)
                {
                    throw new TypeError(String.Format("Cannot apply operator {0} on arguments of type {1} and {2} near row {3}, col {4}.",
                        node.Operator, leftOperandType.Name, rightOperandType.Name, node.Row, node.Col));
                }
            }
            _operandTypes.Push(_symbolTable.ResolveType(op.ResultType));
        }

        public void Visit(BooleanLiteralExpression node)
        {
            _operandTypes.Push(_symbolTable.ResolveType(MiniJavaInfo.BoolType));
        }

        public void Visit(ThisExpression node) { }

        public void Visit(ArrayIndexingExpression node)
        { // check that the reference being indexed is actually an array
          // check that the indexing expression is valid
          // and take note of the return type
            var arrayType = _operandTypes.Pop();
            if (!(arrayType is MiniJavaArrayType))
            {
                throw new TypeError(String.Format("Cannot index into expression of type {0} near row {1}, col {2}.",
                    arrayType.Name, node.Row, node.Col));
            }
            var indexType = _operandTypes.Pop();
            if (indexType != _symbolTable.ResolveType(MiniJavaInfo.IntType))
            {
                throw new TypeError(String.Format("Invalid array index near row {0}, col {1}.",
                    node.Row, node.Col));
            }
            _operandTypes.Push((arrayType as MiniJavaArrayType).ElementType);
        }

        public void Visit(VariableReferenceExpression node)
        { // check that the reference is valid and take note of the type
            var scope = _symbolTable.Scopes[node];
            var symbol = scope.Resolve<VariableSymbol>(node.Name);
            if (symbol == null)
            {
                throw new ReferenceError(String.Format("Could not resolve symbol {0} near row {1}, col {2}.",
                    node.Name, node.Row, node.Col));
            }
            _operandTypes.Push(symbol.Type);
        }

        public void Visit(IntegerLiteralExpression node)
        { // take note of the type
            _operandTypes.Push(_symbolTable.ResolveType(MiniJavaInfo.IntType));
        }

        public void Exit(ClassDeclaration node) { }

        public void Exit(MainClassDeclaration node) { }

        public void Exit(MethodDeclaration node)
        {
            var method = (MethodSymbol)_symbolTable.Scopes[node].Resolve<MethodSymbol>(node.Name);
            int numReturnStatements = _returnTypes.Count;
            if (method.Type == VoidType.GetInstance())
            {
                if (numReturnStatements > 0)
                {
                    throw new TypeError(String.Format("Method of type {0} cannot have return statements near row {1}, col {2}.",
                        method.Type.Name, node.Row, node.Col));
                }
            }
            else
            {
                for (int i = 0; i < numReturnStatements; i++)
                {
                    var returnType = _returnTypes.Pop();
                    if (returnType != method.Type)
                    {
                        throw new TypeError(String.Format("Cannot convert expression of type {0} to {1} near row {2}, col {3}.",
                            returnType, method.Type.Name, node.Row, node.Col));
                    }
                }
            }
        }

        public void Exit(BlockStatement node) { }
    }
}