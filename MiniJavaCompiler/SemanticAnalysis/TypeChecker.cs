using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Visit(MethodDeclaration node)
        { // Check that the method does not overload a method in a superclass.
          // Only overriding is allowed.
            var classScope = _symbolTable.ResolveSurroundingClass(node);
            var superClassMethod = classScope.SuperClass == null ? null : classScope.SuperClass.Resolve<MethodSymbol>(node.Name);
            if (superClassMethod == null)
            {
                return;
            }
            var superClassMethodDeclaration = (MethodDeclaration) _symbolTable.Definitions[superClassMethod];
            if (OverloadsSuperClassMethod(node, superClassMethodDeclaration))
            {
                throw new TypeError(String.Format(
                    "Method {0} in class {1} on row {2}, col {3} overloads method {4} in class {5}. Overloading is not allowed.",
                    node.Name, classScope.Name, node.Row, node.Col, superClassMethod.Name, classScope.SuperClass.Name));
            }
            if (SuperClassMethodHasADifferentTypeSignature(node, superClassMethodDeclaration))
            {
                throw new TypeError(String.Format(
                    "Method {0} in class {1} on row {2}, col {3} has a different type signature from overridden method {4} in class {5}.",
                    node.Name, classScope.Name, node.Row, node.Col, superClassMethod.Name, classScope.SuperClass.Name));
            }
        }

        private bool SuperClassMethodHasADifferentTypeSignature(MethodDeclaration method, MethodDeclaration superClassMethod)
        {
            return !_symbolTable.ResolveType(method.Type, method.IsArray).Equals(
                _symbolTable.ResolveType(superClassMethod.Type, superClassMethod.IsArray));
        }

        private bool OverloadsSuperClassMethod(MethodDeclaration method, MethodDeclaration superClassMethod)
        {
            if (method.Formals.Count != superClassMethod.Formals.Count)
            {
                return true;
            }
            var paramsEqual = method.Formals.Zip(superClassMethod.Formals,
                (a, b) => _symbolTable.ResolveType(a.Type, a.IsArray).Equals(_symbolTable.ResolveType(b.Type, b.IsArray)));
            return paramsEqual.Contains(false);
        }

        public void Visit(PrintStatement node)
        { // argument must be a basic type (int or boolean)
            var type = _operandTypes.Pop();
            if (!(type is BuiltinTypeSymbol))
            {
                throw new TypeError(String.Format("Cannot print expression of type {0} near row {1}, col {2}.",
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
            RequireSingleBooleanArgument(node);
        }

        public void Visit(AssignmentStatement node)
        { // the type of right hand side must match the left hand side
          // and left hand side needs to be an lvalue (in MiniJava
          // that practically means an identifier that references a
          // variable)
            var lhsType = _operandTypes.Pop();
            var rhsType = _operandTypes.Pop();
            if (node.LeftHandSide is VariableReferenceExpression || node.LeftHandSide is ArrayIndexingExpression)
            {
                if (!(rhsType.IsAssignableTo(lhsType)))
                {
                    throw new TypeError(String.Format("Cannot assign expression of type {0} to variable of type {1} " +
                        "near row {2}, col {3}.", rhsType.Name, lhsType.Name, node.Row, node.Col));
                }
            }
            else
            {
                throw new TypeError(String.Format("Assignment receiver expression is not assignable (requires an lvalue) near row {0}, col {1}.",
                    node.Row, node.Col));
            }
        }

        public void Visit(IfStatement node)
        {
            RequireSingleBooleanArgument(node);
        }

        public void Visit(WhileStatement node)
        {
            // TODO: if the single boolean argument is a literal false, this is unreachable code
            RequireSingleBooleanArgument(node);
        }

        public void Visit(MethodInvocation node)
        {
            var expressionType = _operandTypes.Pop();
            MethodSymbol method;
            if (node.MethodOwner is ThisExpression) // method called is defined by the enclosing class or its superclasses
            {
                method = (MethodSymbol)_symbolTable.Scopes[node].Resolve<MethodSymbol>(node.MethodName);
            }
            else
            {
                if (expressionType is MiniJavaArrayType) // method is called on an array (can only be a built in array method)
                {
                    if (MiniJavaArrayType.IsPredefinedArrayMethod(node.MethodName))
                    {
                        _operandTypes.Push(_symbolTable.ResolveType(MiniJavaInfo.IntType));
                        return;
                    }
                    throw new ReferenceError(String.Format("Cannot call method {0} for an array near row {1}, col {2}.",
                        node.MethodName, node.Row, node.Col));
                }
                else if (expressionType is BuiltinTypeSymbol) // no methods are defined for builtin simple types
                {
                    throw new ReferenceError(String.Format("Cannot call method {0} on builtin type {1} near row {2}, col {3}.",
                        node.MethodName, expressionType.Name, node.Row, node.Col));
                }
                else // expression evaluates to an object of a user defined type and method must be resolved in the defining class
                {
                    var enclosingClass = (UserDefinedTypeSymbol) expressionType;
                    method = (MethodSymbol)enclosingClass.Resolve<MethodSymbol>(node.MethodName);
                }
            }
            ValidateMethodCall(method, node);
            _operandTypes.Push(method.Type); // return type, can be void
        }

        public void Visit(InstanceCreationExpression node)
        {
            var createdType = _symbolTable.ResolveType(node.Type, node.IsArrayCreation);
            if (createdType == null)
            {
                throw new ReferenceError(String.Format("Cannot resolve symbol {0} near row {1}, col {2}.",
                    node.Type, node.Row, node.Col));
            }
            if (node.IsArrayCreation)
            { // check that the array size expression is valid
                var arraySizeType = _operandTypes.Pop();
                if (!arraySizeType.IsAssignableTo(_symbolTable.ResolveType(MiniJavaInfo.IntType)))
                {
                    throw new ReferenceError(String.Format("Array size must be numeric near row {0}, col {1}.",
                                                           node.Row, node.Col));
                }
            }
            _operandTypes.Push(createdType);
        }

        public void Visit(UnaryOperatorExpression node)
        {
            var op = MiniJavaInfo.Operators[node.Operator];
            var expectedArgType = _symbolTable.ResolveType(op.OperandType);
            var actualArgType = _operandTypes.Pop();
            if (!actualArgType.IsAssignableTo(expectedArgType))
            {
                throw new TypeError(String.Format("Cannot apply operator {0} on operand of type {1} on row {2}, col {3}.",
                    node.Operator, actualArgType.Name, node.Row, node.Col));
            }
            _operandTypes.Push(_symbolTable.ResolveType(op.ResultType));
        }

        public void Visit(BinaryOpExpression node)
        {
            var leftOperandType = _operandTypes.Pop();
            var rightOperandType = _operandTypes.Pop();
            var op = MiniJavaInfo.Operators[node.Operator];
            if (op.OperandType != MiniJavaInfo.AnyType)
            { // types are not checked if operator can be applied to any type of object (like ==)
                var expectedOpType = _symbolTable.ResolveType(op.OperandType);
                if (!leftOperandType.IsAssignableTo(expectedOpType) || !rightOperandType.IsAssignableTo(expectedOpType))
                { // both arguments (lhs and rhs) must match operator's expected operand type
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

        public void Visit(ThisExpression node)
        {
            var thisType = _symbolTable.ResolveSurroundingClass(node);
            _operandTypes.Push(thisType);
        }

        public void Visit(ArrayIndexingExpression node)
        {
            var arrayType = _operandTypes.Pop();
            if (!(arrayType is MiniJavaArrayType)) // only arrays can be indexed
            {
                throw new TypeError(String.Format("Cannot index into expression of type {0} near row {1}, col {2}.",
                    arrayType.Name, node.Row, node.Col));
            }
            var indexType = _operandTypes.Pop();
            if (!indexType.IsAssignableTo(_symbolTable.ResolveType(MiniJavaInfo.IntType)))
            { // array must be indexed with an expression that evaluates to an int value
                throw new TypeError(String.Format("Invalid array index near row {0}, col {1}.",
                    node.Row, node.Col));
            }
            _operandTypes.Push((arrayType as MiniJavaArrayType).ElementType);
        }

        public void Visit(VariableReferenceExpression node)
        { // check that the reference is valid and take note of the type
            var scope = _symbolTable.Scopes[node];
            var symbol = (VariableSymbol) scope.Resolve<VariableSymbol>(node.Name);
            if (symbol == null || !VariableDeclaredBeforeReference(symbol, node))
            {
                throw new ReferenceError(String.Format("Could not resolve symbol {0} near row {1}, col {2}.",
                    node.Name, node.Row, node.Col));
            }
            _operandTypes.Push(symbol.Type);
        }

        public void Visit(IntegerLiteralExpression node)
        {
            try
            {
                Int32.Parse(node.Value);
            }
            catch (OverflowException)
            {
                throw new TypeError(String.Format("Cannot fit integer literal {0} into a 32 bit integer variable near row {1}, col {2}.",
                    node.Value, node.Row, node.Col));
            }
            _operandTypes.Push(_symbolTable.ResolveType(MiniJavaInfo.IntType));
        }

        public void Exit(ClassDeclaration node) { }

        public void Exit(MainClassDeclaration node) { }

        public void Exit(MethodDeclaration node)
        { // Note: in this implementation the main method cannot be called from inside the program
          // because there would be no sensible use for such a method call - and there are no other
          // static methods - so implementing it would have been pointless.
            var method = (MethodSymbol)_symbolTable.Scopes[node].Resolve<MethodSymbol>(node.Name);
            int numReturnStatements = _returnTypes.Count;
            if (method.Type.Equals(VoidType.GetInstance()))
            { // void methods cannot have return statements (because Mini-Java does not define an empty return statement)
                if (numReturnStatements > 0)
                {
                    throw new TypeError(String.Format("Method of type {0} cannot have return statements near row {1}, col {2}.",
                        method.Type.Name, node.Row, node.Col));
                }
            }
            else if (numReturnStatements == 0)
            {
                throw new TypeError(String.Format("Missing return statement in method {0} near row {1}, col {2}.",
                    method.Name, node.Row, node.Col));
            }
            else
            {
                if (!AllBranchesReturnAValue(node))
                {
                    throw new TypeError(String.Format("Missing return statement in method {0} near row {1}, col {2}.",
                        method.Name, node.Row, node.Col));
                }
                for (int i = 0; i < numReturnStatements; i++)
                {
                    var returnType = _returnTypes.Pop();
                    if (!returnType.IsAssignableTo(method.Type))
                    { // the type of object returned by the return statement must match the method's declared return type
                        throw new TypeError(String.Format("Cannot convert expression of type {0} to {1} near row {2}, col {3}.",
                            returnType.Name, method.Type.Name, node.Row, node.Col));
                    }
                }
            }
        }

        public void Exit(BlockStatement node) { }

        private bool AllBranchesReturnAValue(MethodDeclaration node)
        {
            return BlockAlwaysReturnsAValue(node.MethodBody);
        }

        private bool BlockAlwaysReturnsAValue(List<IStatement> statementsInBlock)
        {
            var flattenedStatementsInBlock = FlattenStatementList(statementsInBlock);
            var returnIdx = flattenedStatementsInBlock.FindIndex((statement) => statement is ReturnStatement); // TODO: if this is not the last index in the block, there is unreachable code which should cause an error
            if (returnIdx >= 0)
            {
                return true;
            }
            var conditionalStatements = new List<IfStatement>(flattenedStatementsInBlock.OfType<IfStatement>());
            if (!conditionalStatements.Any())
            {
                return false;
            }
            bool allConditionalsReturnAValue = true;
            foreach (var conditional in conditionalStatements)
            {
                allConditionalsReturnAValue &= BlockAlwaysReturnsAValue(conditional.ThenBranch.Statements);
                if (conditional.ElseBranch == null)
                {
                    allConditionalsReturnAValue = false;
                }
                else
                {
                    allConditionalsReturnAValue &= BlockAlwaysReturnsAValue(conditional.ElseBranch.Statements);
                }
            }
            return allConditionalsReturnAValue;
        }

        private List<IStatement> FlattenStatementList(List<IStatement> statementList)
        {
            if (!statementList.Any(elem => elem is BlockStatement))
            {
                return statementList;
            }
            var wrappedStatements = statementList.Select(elem => elem is BlockStatement ?
                new List<IStatement>(FlattenStatementList((elem as BlockStatement).Statements)) :
                new List<IStatement>() {elem});
            return wrappedStatements.SelectMany(elem => elem).ToList();
        }

        private void RequireSingleBooleanArgument(SyntaxElement node)
        {
            var argType = _operandTypes.Pop();
            if (!(argType is BuiltinTypeSymbol && argType.Name == MiniJavaInfo.BoolType))
            {
                throw new TypeError(String.Format("Cannot convert expression of type {0} to boolean near row {1}, col {2}.",
                                                  argType.Name, node.Row, node.Col));
            }
        }

        private void ValidateMethodCall(MethodSymbol method, MethodInvocation node)
        {
            if (method == null) // method does not exist
            {
                throw new ReferenceError(String.Format("Cannot resolve symbol {0} near row {1}, col {2}.",
                    node.MethodName, node.Row, node.Col));
            }
            if (method.IsStatic)
            {
                throw new ReferenceError(String.Format("Cannot call static method {0} on an instance near row {1}, col {2}.",
                    node.MethodName, node.Row, node.Col));
            }
            var methodDecl = (MethodDeclaration)_symbolTable.Definitions[method];
            if (node.CallParameters.Count != methodDecl.Formals.Count)
            {
                throw new TypeError(String.Format("Wrong number of arguments to method {0} near row {1}, col {2}.",
                    node.MethodName, node.Row, node.Col));
            }

            ValidateCallParameterTypes(node, methodDecl);
        }

        private void ValidateCallParameterTypes(MethodInvocation node, MethodDeclaration methodDecl)
        {
            var callParams = new Stack<IType>();
            foreach (var arg in node.CallParameters)
            {
                callParams.Push(_operandTypes.Pop());
            }
            foreach (var formalParameter in methodDecl.Formals)
            {
                var callParamType = callParams.Pop();
                var formalParamType = _symbolTable.ResolveType(formalParameter.Type, formalParameter.IsArray);
                if (!callParamType.IsAssignableTo(formalParamType))
                {
                    throw new TypeError(String.Format(
                        "Wrong type of argument to method {0} near row {1}, col {2}. Expected {3} but got {4}.",
                        node.MethodName, node.Row, node.Col, formalParamType.Name, callParamType.Name));
                }
            }
        }

        private bool VariableDeclaredBeforeReference(VariableSymbol varSymbol, VariableReferenceExpression reference)
        {
            if (varSymbol.EnclosingScope is UserDefinedTypeSymbol)
            { // Variables defined on the class level are visible in all internal scopes.
                return true;
            }
            var declaration = (VariableDeclaration)_symbolTable.Definitions[varSymbol];
            return declaration.Row < reference.Row || (declaration.Row == reference.Row && declaration.Col < reference.Col);
        }
    }
}