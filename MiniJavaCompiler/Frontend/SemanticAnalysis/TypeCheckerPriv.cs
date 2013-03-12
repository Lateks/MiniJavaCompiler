using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using MiniJavaCompiler.Support.SymbolTable.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.FrontEnd.SemanticAnalysis
{
    public partial class SemanticsChecker
    {
        // These are the private helper methods used by TypeChecker.
        private partial class TypeChecker : INodeVisitor
        {
            private void CheckForOverloading(MethodDeclaration node, TypeSymbol classSymbol, MethodSymbol superClassMethod)
            {
                var superClassMethodDeclaration = (MethodDeclaration)_parent._symbolTable.Definitions[superClassMethod];
                if (OverloadsSuperClassMethod(node, superClassMethodDeclaration))
                {
                    ReportError(String.Format("Method {0} in class {1} overloads a method in class {2}. Overloading is not allowed.",
                        node.Name, classSymbol.Name, classSymbol.SuperClass.Name), node);
                }

                // Subclass methods can have covariant return types with respect to overridden
                // superclass methods. (Note: arrays are still non-covariant.)
                if (SuperClassMethodHasADifferentReturnType(node, superClassMethodDeclaration))
                {
                    ReportError(String.Format(
                        "Method {0} in class {1} has a different return type from overridden method in class {2}.",
                        node.Name, classSymbol.Name, classSymbol.SuperClass.Name), node);
                }
            }

            private MethodSymbol ResolveMethod(MethodInvocation node, IType methodOwnerType)
            {
                MethodSymbol method = null;

                if (node.MethodOwner is ThisExpression)
                {   // Method called is defined by the enclosing class or its superclasses.
                    method = _parent._symbolTable.Scopes[node].ResolveMethod(node.MethodName);
                }
                else if (methodOwnerType == VoidType.GetInstance())
                {
                    ReportError(String.Format("Cannot call a method on type {0}.", methodOwnerType.Name), node);
                }
                else if (methodOwnerType is ScalarType || methodOwnerType is ArrayType)
                {
                    var typeSymbol = _parent._symbolTable.ResolveTypeName(methodOwnerType.Name);
                    method = typeSymbol.Scope.ResolveMethod(node.MethodName);
                }
                return method;
            }

            private IType CheckCreatedType(InstanceCreationExpression node)
            {
                var createdTypeSymbol = _parent._symbolTable.ResolveTypeName(node.CreatedTypeName, node.IsArrayCreation);
                IType createdType;
                if (createdTypeSymbol == null)
                {
                    ReportError(String.Format("Cannot resolve symbol {0}.",
                        node.CreatedTypeName), node);
                    createdType = null;
                }
                else
                {
                    createdType = createdTypeSymbol.Type;
                }
                return createdType;
            }

            private void CheckArraySizeType(InstanceCreationExpression node)
            {
                if (node.IsArrayCreation)
                {
                    var integerType = _parent._symbolTable.ResolveTypeName(MiniJavaInfo.IntType).Type;
                    if (!node.ArraySize.Type.IsAssignableTo(integerType))
                    {
                        ReportError("Array size must be numeric.", node);
                    }
                }
            }

            private bool AllBranchesReturnAValue(MethodDeclaration node)
            {
                return BlockAlwaysReturnsAValue(node.MethodBody);
            }

            private bool BlockAlwaysReturnsAValue(List<IStatement> statementsInBlock)
            {
                // Start by flattening block statements.
                var flattenedStatementsInBlock = FlattenStatementList(statementsInBlock);
                var returnIdx = flattenedStatementsInBlock.FindIndex((statement) => statement is ReturnStatement);
                if (returnIdx >= 0) // Return statement found.
                {
                    return true;
                }

                // There were no return statements on this level, so all
                // conditional branches must return a value.
                var conditionalStatements = new Stack<IfStatement>(flattenedStatementsInBlock.OfType<IfStatement>());
                if (!conditionalStatements.Any())
                {
                    return false; // No conditional branches, so the block never returns a value.
                }

                return AllConditionalsReturnAValue(conditionalStatements);
            }

            private bool AllConditionalsReturnAValue(Stack<IfStatement> conditionalStatements)
            {
                bool allConditionalsReturnAValue = true;
                IfStatement conditional;
                while (allConditionalsReturnAValue && conditionalStatements.Count > 0)
                {
                    conditional = conditionalStatements.Pop();
                    if (conditional.ElseBranch == null)
                    {
                        allConditionalsReturnAValue = false;
                        continue;
                    }

                    allConditionalsReturnAValue &= BlockAlwaysReturnsAValue(conditional.ThenBranch.Statements);
                    allConditionalsReturnAValue &= BlockAlwaysReturnsAValue(conditional.ElseBranch.Statements);
                }
                return allConditionalsReturnAValue;
            }

            // Flattens a list of statements recursively by replacing
            // block statements with lists of statements.
            private List<IStatement> FlattenStatementList(List<IStatement> statementList)
            {
                if (!statementList.Any(elem => elem is BlockStatement))
                {   // If there are no block statements, there is nothing to flatten.
                    return statementList;
                }
                return statementList.SelectMany(elem => FlattenStatement(elem)).ToList();
            }

            private List<IStatement> FlattenStatement(IStatement statement)
            {
                if (statement is BlockStatement)
                {
                    return FlattenStatementList((statement as BlockStatement).Statements);
                }
                return new List<IStatement>() { statement };
            }

            private void ArgumentShouldBeBoolean(SyntaxElement node, IExpression argument)
            {
                var argType = argument.Type;
                if (argType == ErrorType.GetInstance()) return;
                if (argType.Name != MiniJavaInfo.BoolType)
                {
                    ReportError(String.Format("Cannot convert expression of type {0} to boolean.",
                        argType.Name), node);
                }
            }

            private void ValidateMethodCall(MethodSymbol method, MethodInvocation node, IType methodOwnerType)
            {
                if (method == null) // method does not exist
                {
                    ReportError(String.Format("Cannot resolve symbol {0}.", node.MethodName), node);
                    return;
                }

                if (methodOwnerType is ArrayType) return; // no checks done, there can be no call params

                if (method.Type is ErrorType) _checkOK = false;

                if (method.IsStatic)
                {
                    ReportError(String.Format("Cannot call static method {0} on an instance.",
                        node.MethodName), node);
                    return;
                }

                var methodDecl = (MethodDeclaration)_parent._symbolTable.Definitions[method];
                if (node.CallParameters.Count != methodDecl.Formals.Count)
                {
                    ReportError(String.Format("Wrong number of arguments to method {0} ({1} for {2}).",
                        node.MethodName, node.CallParameters.Count, methodDecl.Formals.Count), node);
                    return;
                }

                ValidateCallParameterTypes(node, methodDecl);
            }

            private void ReportError(string errorMsg, SyntaxElement node)
            {
                _parent._errors.ReportError(errorMsg, node.Row, node.Col);
                _checkOK = false;
            }

            private void ValidateCallParameterTypes(MethodInvocation node, MethodDeclaration methodDecl)
            {
                var callParamTypes = node.CallParameters.Select<IExpression, IType>((expr) => expr.Type).ToList();
                for (int i = 0; i < methodDecl.Formals.Count; i++)
                {
                    var callParamType = callParamTypes[i];
                    var formalParamType = _parent._symbolTable.ResolveTypeName(methodDecl.Formals[i].Type,
                        methodDecl.Formals[i].IsArray).Type;
                    if (!callParamType.IsAssignableTo(formalParamType))
                    {
                        ReportError(String.Format(
                            "Wrong type of argument to method {0}. Expected {1} but got {2}.",
                            node.MethodName, formalParamType.Name, callParamType.Name), node);
                    }
                }
            }

            private bool VariableDeclaredBeforeReference(VariableSymbol varSymbol,
                VariableReferenceExpression reference)
            {
                if (varSymbol.Scope is TypeSymbol)
                {   // Variables defined on the class level are visible
                    // in all scopes internal to the class.
                    return true;
                }
                var declaration = (VariableDeclaration)_parent._symbolTable.Definitions[varSymbol];
                return declaration.Row < reference.Row ||
                    (declaration.Row == reference.Row && declaration.Col < reference.Col);
            }

            // Allows covariance.
            private bool SuperClassMethodHasADifferentReturnType(MethodDeclaration method,
                MethodDeclaration superClassMethod)
            {
                var returnType = _parent._symbolTable.ResolveTypeName(method.Type, method.IsArray).Type;
                var superClassMethodReturnType = _parent._symbolTable.ResolveTypeName(
                    superClassMethod.Type, superClassMethod.IsArray).Type;
                return !returnType.IsAssignableTo(superClassMethodReturnType);
            }

            private bool OverloadsSuperClassMethod(MethodDeclaration method,
                MethodDeclaration superClassMethod)
            {
                if (method.Formals.Count != superClassMethod.Formals.Count)
                {
                    return true;
                }
                bool formalsEqual = true;
                for (int i = 0; i < method.Formals.Count && formalsEqual; i++)
                {
                    var methodFormal = _parent._symbolTable.ResolveTypeName(
                        method.Formals[i].Type, method.Formals[i].IsArray);
                    var superFormal = _parent._symbolTable.ResolveTypeName(
                        superClassMethod.Formals[i].Type, superClassMethod.Formals[i].IsArray);
                    formalsEqual = methodFormal.Equals(superFormal);
                }
                return !formalsEqual;
            }

            private void CheckBinaryOperatorOperands(BinaryOperatorExpression node, IType left, IType right)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                var expected = _parent._symbolTable.ResolveTypeName(op.OperandType).Type;
                if (left.IsAssignableTo(expected) && right.IsAssignableTo(expected))
                { // Everything OK.
                    return;
                }
                Debug.Assert(!(left is ErrorType && right is ErrorType));

                ReportBinaryOperatorError(node, left, right);
            }

            private void ReportBinaryOperatorError(BinaryOperatorExpression node, IType left, IType right)
            {
                string errormsg;
                string opRepr = MiniJavaInfo.OperatorRepr(node.Operator);
                if (left is ErrorType)
                {
                    errormsg = String.Format("Invalid operand of type {0} for operator {1}.", right.Name, opRepr);
                }
                else if (right is ErrorType)
                {
                    errormsg = String.Format("Invalid operand of type {0} for operator {1}.", left.Name, opRepr);
                }
                else
                {
                    errormsg = String.Format("Cannot apply operator {0} on arguments of type {1} and {2}.",
                        opRepr, left.Name, right.Name);
                }
                ReportError(errormsg, node);
            }

            private void CheckOperandCompatibility(BinaryOperatorExpression node, IType left, IType right)
            {
                if (left.IsAssignableTo(right) || right.IsAssignableTo(left))
                    return;
                ReportError(String.Format("Cannot apply operator {0} on arguments of type {1} and {2}.",
                    node.Operator, left.Name, right.Name), node);
            }

            private void CheckReturnTypes(MethodDeclaration node, MethodSymbol method)
            {
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

    }
}
