using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;

namespace MiniJavaCompiler.Frontend.SemanticAnalysis
{
    /* This class checks that the types, references (variables, methods etc.),
     * return statements and method invocation arguments are acceptable.
     * 
     * Does not produce any output except for error message output to
     * the error reporter (if applicable).
     */ 
    public class TypeChecker : INodeVisitor
    {
        private readonly SymbolTable _symbolTable;
        private readonly Stack<IType> _operandTypes;
        private readonly Stack<IType> _returnTypes; // When return statements are encountered, the types of the expressions they return
                                                    // will be stored here and checked when exiting the method declaration.
        private readonly Program _programRoot;
        private readonly IErrorReporter _errors;
        private bool _checkFailed;

        public TypeChecker(Program program, SymbolTable symbolTable, IErrorReporter errorReporter)
        {
            _checkFailed = false;
            _programRoot = program;
            _symbolTable = symbolTable;
            _operandTypes = new Stack<IType>();
            _returnTypes = new Stack<IType>();
            _errors = errorReporter;
        }

        // Throws an exception at the end of analysis if there is a problem
        // with types or references.
        public void CheckTypesAndReferences()
        {
            _programRoot.Accept(this);
            if (_checkFailed)
            {
                throw new SemanticAnalysisFailed();
            }
        }

        public void Visit(Program node) { }

        public void Visit(ClassDeclaration node) { }

        public void Visit(MainClassDeclaration node) { }

        public void Visit(VariableDeclaration node) { }

        public void Visit(MethodDeclaration node)
        {  // Check that the method does not overload a method in a superclass.
           // Only overriding is allowed.
            var classScope = _symbolTable.ResolveSurroundingClass(node);
            var superClassMethod = classScope.SuperClass == null ? null : classScope.SuperClass.ResolveMethod(node.Name);
            if (superClassMethod == null) // Did not override or overload another method.
            {
                return;
            }

            var superClassMethodDeclaration = (MethodDeclaration) _symbolTable.Definitions[superClassMethod];
            if (OverloadsSuperClassMethod(node, superClassMethodDeclaration))
            {
                ReportError(String.Format("Method {0} in class {1} overloads a method in class {2}. Overloading is not allowed.",
                    node.Name, classScope.Name, classScope.SuperClass.Name), node);
            }
            if (SuperClassMethodHasADifferentReturnType(node, superClassMethodDeclaration)) // Allows return type covariance. (Note: arrays are still non-covariant.)
            {
                ReportError(String.Format(
                    "Method {0} in class {1} has a different return type from overridden method in class {2}.",
                    node.Name, classScope.Name, classScope.SuperClass.Name), node);
            }
        }

        public void Visit(PrintStatement node)
        {   // Argument must be a basic type (int or boolean).
            var type = _operandTypes.Pop();
            if (type is ErrorType) return; // Type errors are never checked in recovery.
            if (!(type is BuiltInTypeSymbol))
            {
                ReportError(String.Format("Cannot print expression of type {0}.", type.Name), node);
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
        {   // The type of right hand side must match the left hand side
            // and left hand side needs to be an lvalue.
            var lhsType = _operandTypes.Pop();
            var rhsType = _operandTypes.Pop();
            if (node.LeftHandSide is VariableReferenceExpression || node.LeftHandSide is ArrayIndexingExpression) // The only lvalues in Mini-Java.
            {
                if (!(rhsType.IsAssignableTo(lhsType)))
                {
                    Debug.Assert(!(lhsType is ErrorType || rhsType is ErrorType)); // ErrorType should be assignable both ways.
                    ReportError(String.Format("Cannot assign expression of type {0} to variable of type {1}.",
                        rhsType.Name, lhsType.Name), node);
                }
            }
            else
            {
                ReportError("Assignment receiver expression is not assignable (an lvalue required).",
                    node);
            }
        }

        public void Visit(IfStatement node)
        {
            RequireSingleBooleanArgument(node);
        }

        public void Visit(WhileStatement node)
        {
            RequireSingleBooleanArgument(node);
        }

        public void Visit(MethodInvocation node)
        {   // Note: in this implementation the main method cannot be called from inside the program
            // because there would be no sensible use for such a method call - and there are no other
            // static methods - so implementing it would have been pointless.
            var methodOwnerType = _operandTypes.Pop();
            MethodSymbol method = null;
            if (node.MethodOwner is ThisExpression) // Method called is defined by the enclosing class or its superclasses.
            {
                method = (MethodSymbol)_symbolTable.Scopes[node].ResolveMethod(node.MethodName);
            }
            else if (methodOwnerType != ErrorType.GetInstance()) // Type error tokens are not checked.
            {
                if (methodOwnerType is MiniJavaArrayType) // Method is called on an array (can only be a built in array method).
                {
                    if (MiniJavaArrayType.IsPredefinedArrayMethod(node.MethodName))
                    {
                        _operandTypes.Push(_symbolTable.ResolveType(MiniJavaInfo.IntType)); // In this case there is nothing further to check, since length is
                        return;                                                             // actually a field and therefore the invocation cannot have parameters.
                    }
                    ReportError(String.Format("Cannot call method {0} for an array.",
                        node.MethodName), node);
                }
                else if (methodOwnerType is BuiltInTypeSymbol)
                {
                    ReportError(String.Format("Cannot call method {0} on built-in type {1}.",
                        node.MethodName, methodOwnerType.Name), node);
                }
                else
                {
                    var enclosingClass = (UserDefinedTypeSymbol) methodOwnerType;
                    method = (MethodSymbol)enclosingClass.ResolveMethod(node.MethodName);
                }
            }
            ValidateMethodCall(method, node); // Pops out possible parameters for the method invocation even if the method could not be resolved.
            _operandTypes.Push(method == null ? ErrorType.GetInstance() : method.Type); // Expected return type, can be void.
        }

        public void Visit(InstanceCreationExpression node)
        {
            var createdType = _symbolTable.ResolveType(node.Type, node.IsArrayCreation);
            if (createdType == null)
            {
                _errors.ReportError(String.Format("Cannot resolve symbol {0}.",
                    node.Type), node.Row, node.Col);
            }
            if (node.IsArrayCreation)
            {   // Check that the array size expression is valid.
                var arraySizeType = _operandTypes.Pop();
                if (!arraySizeType.IsAssignableTo(_symbolTable.ResolveType(MiniJavaInfo.IntType)))
                {
                    ReportError("Array size must be numeric.", node);
                }
            }
            _operandTypes.Push(createdType ?? ErrorType.GetInstance());
        }

        public void Visit(UnaryOperatorExpression node)
        {
            var op = MiniJavaInfo.GetOperator(node.Operator);
            var expectedArgType = _symbolTable.ResolveType(op.OperandType);
            var actualArgType = _operandTypes.Pop();
            if (!actualArgType.IsAssignableTo(expectedArgType))
            {
                ReportError(String.Format("Cannot apply operator {0} on operand of type {1}.",
                    node.Operator, actualArgType.Name), node);
            }
            _operandTypes.Push(_symbolTable.ResolveType(op.ResultType));
        }

        public void Visit(BinaryOpExpression node)
        {
            var leftOperandType = _operandTypes.Pop();
            var rightOperandType = _operandTypes.Pop();
            var op = MiniJavaInfo.GetOperator(node.Operator);
            if (op.OperandType != MiniJavaInfo.AnyType) // Types are not checked if operator can be applied to any type of object (like ==).
            {
                var expectedOpType = _symbolTable.ResolveType(op.OperandType);
                if (!leftOperandType.IsAssignableTo(expectedOpType) || !rightOperandType.IsAssignableTo(expectedOpType))
                {   // Both arguments (lhs and rhs) must match operator's expected operand type.
                    // Note: both arguments cannot be ErrorTypes because in that case they would
                    // both have passed the assignability test.
                    Debug.Assert(!(leftOperandType is ErrorType && rightOperandType is ErrorType));
                    string errormsg;
                    if (leftOperandType is ErrorType)
                    {
                        errormsg = String.Format("Invalid operand of type {0} for operator {1}.", rightOperandType.Name,
                                                 node.Operator);
                    }
                    else if (rightOperandType is ErrorType)
                    {
                        errormsg = String.Format("Invalid operand of type {0} for operator {1}.", leftOperandType.Name,
                                                 node.Operator);
                    }
                    else
                    {
                        errormsg = String.Format("Cannot apply operator {0} on arguments of type {1} and {2}.",
                                                 node.Operator, leftOperandType.Name, rightOperandType.Name);
                    }
                    ReportError(errormsg, node);
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
            if (!(arrayType is ErrorType) && !(arrayType is MiniJavaArrayType)) // Only arrays can be indexed. Resolving errors are ignored.
            {
                ReportError(String.Format("Cannot index into expression of type {0}.", arrayType.Name), node);
            }
            var indexType = _operandTypes.Pop();
            if (!indexType.IsAssignableTo(_symbolTable.ResolveType(MiniJavaInfo.IntType)))
            { // Array must be indexed with an expression that evaluates into an int value.
                ReportError("Invalid array index.", node);
            }
            _operandTypes.Push(arrayType is MiniJavaArrayType ?
                (IType) (arrayType as MiniJavaArrayType).ElementType : ErrorType.GetInstance());
        }

        public void Visit(VariableReferenceExpression node)
        {
            var scope = _symbolTable.Scopes[node];
            var symbol = (VariableSymbol) scope.ResolveVariable(node.Name);
            if (symbol == null || !VariableDeclaredBeforeReference(symbol, node))
            {
                ReportError(String.Format("Cannot resolve symbol {0}.", node.Name), node);
            }
            _operandTypes.Push(symbol == null ? ErrorType.GetInstance() : symbol.Type);
        }

        public void Visit(IntegerLiteralExpression node)
        {
            int value;
            if (!Int32.TryParse(node.Value, out value))
            {
                ReportError(String.Format("Cannot fit integer literal {0} into a 32-bit integer variable.",
                    node.Value), node);
            }
            _operandTypes.Push(_symbolTable.ResolveType(MiniJavaInfo.IntType));
        }

        public void Exit(ClassDeclaration node) { }

        public void Exit(MainClassDeclaration node) { }

        public void Exit(MethodDeclaration node)
        {
            var method = (MethodSymbol)_symbolTable.Scopes[node].ResolveMethod(node.Name);
            int numReturnStatements = _returnTypes.Count;
            if (method.Type == VoidType.GetInstance())
            {  // Void methods cannot have return statements
               // (because Mini-Java does not allow empty return statements).
                if (numReturnStatements > 0)
                {
                    ReportError(String.Format("Method of type {0} cannot have return statements.",
                        method.Type.Name), node);
                    _returnTypes.Clear();
                }
            }
            else if (numReturnStatements == 0)
            {
                ReportError(String.Format("Missing return statement in method {0}.",
                    method.Name), node);
            }
            else
            {
                if (!AllBranchesReturnAValue(node))
                {
                    ReportError(String.Format("Missing return statement in method {0}.",
                        method.Name), node);
                }

                // Return types can be checked even if some branches were missing a return statement.
                while (_returnTypes.Count > 0)
                {
                    var returnType = _returnTypes.Pop();
                    if (!returnType.IsAssignableTo(method.Type))
                    {   // The type of object returned by the return statement must match the method's
                        // declared return type.
                        ReportError(String.Format("Cannot convert expression of type {0} to {1}.",
                            returnType.Name, method.Type.Name), node);
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
            var flattenedStatementsInBlock = FlattenStatementList(statementsInBlock); // Flatten block statements.
            var returnIdx = flattenedStatementsInBlock.FindIndex((statement) => statement is ReturnStatement);
            if (returnIdx >= 0)
            {
                return true;
            }

            // There were no return statements on this level, so all conditional branches must return a value.
            var conditionalStatements = new Stack<IfStatement>(flattenedStatementsInBlock.OfType<IfStatement>());
            if (!conditionalStatements.Any())
            {
                return false; // No conditional branches, so the block never returns a value.
            }

            bool allConditionalsReturnAValue = true;
            IfStatement conditional;
            while (allConditionalsReturnAValue && conditionalStatements.Count > 0)
            {
                conditional = conditionalStatements.Pop();

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

        // Flattens a list of statements recursively by replacing block statements with
        // lists of statements.
        private List<IStatement> FlattenStatementList(List<IStatement> statementList)
        {
            if (!statementList.Any(elem => elem is BlockStatement)) // If there are no block statements, there is nothing to flatten.
            {
                return statementList;
            }
            // Convert each block statement into a list of statements and wrap individual
            // statements into lists to be able to concatenate them.
            var wrappedStatements = statementList.Select(elem => elem is BlockStatement
                ? FlattenStatementList((elem as BlockStatement).Statements)
                : new List<IStatement>() { elem });
            return wrappedStatements.SelectMany(elem => elem).ToList(); // Concatenate lists.
        }

        private void RequireSingleBooleanArgument(SyntaxElement node)
        {
            var argType = _operandTypes.Pop();
            if (argType == ErrorType.GetInstance()) return;
            if (!(argType is BuiltInTypeSymbol && argType.Name == MiniJavaInfo.BoolType))
            {
                ReportError(String.Format("Cannot convert expression of type {0} to boolean.",
                    argType.Name), node);
            }
        }

        private void ValidateMethodCall(MethodSymbol method, MethodInvocation node)
        {
            if (method == null) // method does not exist
            {
                ReportErrorAndDiscardCallParams(String.Format("Cannot resolve symbol {0}.", node.MethodName), node);
                return;
            }
            if (method.IsStatic)
            {
                ReportErrorAndDiscardCallParams(String.Format("Cannot call static method {0} on an instance.", node.MethodName), node);
                return;
            }
            var methodDecl = (MethodDeclaration)_symbolTable.Definitions[method];
            if (node.CallParameters.Count != methodDecl.Formals.Count)
            {
                ReportErrorAndDiscardCallParams(String.Format("Wrong number of arguments to method {0} ({1} for {2}).",
                    node.MethodName, node.CallParameters.Count, methodDecl.Formals.Count), node);
                return;
            }
            
            ValidateCallParameterTypes(node, methodDecl);
        }

        private void ReportError(string errorMsg, SyntaxElement node)
        {
            _errors.ReportError(errorMsg, node.Row, node.Col);
            _checkFailed = true;
        }

        private void ReportErrorAndDiscardCallParams(string errorMsg, MethodInvocation node)
        {
            ReportError(errorMsg, node);
            PopCallParams(node);
        }

        private void ValidateCallParameterTypes(MethodInvocation node, MethodDeclaration methodDecl)
        {
            var callParams = PopCallParams(node);
            foreach (var formalParameter in methodDecl.Formals)
            {
                var callParamType = callParams.Pop();
                var formalParamType = _symbolTable.ResolveType(formalParameter.Type, formalParameter.IsArray);
                if (!callParamType.IsAssignableTo(formalParamType))
                {
                    ReportError(String.Format("Wrong type of argument to method {0}. Expected {1} but got {2}.",
                        node.MethodName, formalParamType.Name, callParamType.Name), node);
                }
            }
        }

        private Stack<IType> PopCallParams(MethodInvocation node)
        {
            var callParams = new Stack<IType>();
            foreach (var arg in node.CallParameters)
            {
                callParams.Push(_operandTypes.Pop());
            }
            return callParams;
        }

        private bool VariableDeclaredBeforeReference(VariableSymbol varSymbol, VariableReferenceExpression reference)
        {
            if (varSymbol.EnclosingScope is UserDefinedTypeSymbol)
            { // Variables defined on the class level are visible in all scopes internal to the class.
                return true;
            }
            var declaration = (VariableDeclaration)_symbolTable.Definitions[varSymbol];
            return declaration.Row < reference.Row || (declaration.Row == reference.Row && declaration.Col < reference.Col);
        }

        // Allows covariance.
        private bool SuperClassMethodHasADifferentReturnType(MethodDeclaration method, MethodDeclaration superClassMethod)
        {
            return !_symbolTable.ResolveType(method.Type, method.IsArray).IsAssignableTo(
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
    }
}