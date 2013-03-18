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
        private partial class TypeChecker : NodeVisitorBase
        {
            private void CheckForOverloading(MethodDeclaration node, TypeSymbol classSymbol, MethodSymbol superClassMethod)
            {
                var superClassMethodDeclaration = (MethodDeclaration)_parent._symbolTable.Declarations[superClassMethod];
                if (OverloadsSuperClassMethod(node, superClassMethodDeclaration))
                {
                    var msg = String.Format("Method {0} in class {1} overloads a method in class {2}. Overloading is not allowed.",
                        node.Name, classSymbol.Name, classSymbol.SuperClass.Name);
                    ReportError(ErrorTypes.InvalidOverride, msg, node);
                }

                // Subclass methods CANNOT have covariant return types with respect to overridden
                // superclass methods, though this is allowed in Java. This is because the .NET runtime
                // does not natively support them. (Reference link can be found in tests and docs.)
                if (node.ReturnType != superClassMethodDeclaration.ReturnType)
                {
                    var msg = String.Format(
                        "Method {0} in class {1} has a different return type from overridden method in class {2}.",
                        node.Name, classSymbol.Name, classSymbol.SuperClass.Name);
                    ReportError(ErrorTypes.InvalidOverride, msg, node);
                }
            }

            private void CheckArraySizeType(InstanceCreationExpression node)
            {
                if (node.IsArrayCreation)
                {
                    var integerType = _parent._symbolTable.ResolveTypeName(MiniJavaInfo.IntType).Type;
                    if (!node.ArraySize.Type.IsAssignableTo(integerType))
                    {
                        ReportError(ErrorTypes.TypeError, "Array size must be numeric.", node);
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
                if (conditionalStatements.Count == 0)
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
                    ReportError(
                        ErrorTypes.TypeError,
                        String.Format("Cannot convert expression of type {0} to boolean.",
                        argType.Name), node);
                }
            }

            private void ValidateMethodCall(MethodInvocation node)
            {
                if (node.MethodOwner.Type is ArrayType || node.ReferencedMethod == null)
                {
                    return;
                }

                if (node.ReferencedMethod.ReturnType is ErrorType)
                {
                    _checkOK = false;
                }

                if (node.ReferencedMethod.IsStatic)
                {
                    // Note: this is not an actual error in Java, where static methods can
                    // be referenced even through instances. However, since in MiniJava the
                    // only static method is the main method, I have prevented calls to static
                    // methods altogether. (In Java, of course, even the main method CAN be
                    // called from inside the program, but who would want to do that?)
                    ReportError(ErrorTypes.MethodReference,
                        String.Format("Cannot call static method {0}.",
                        node.MethodName), node);
                    return;
                }

                if (node.CallParameters.Count != node.ReferencedMethod.Formals.Count)
                {
                    ReportError(
                        ErrorTypes.TypeError,
                        String.Format("Wrong number of arguments to method {0} ({1} for {2}).",
                        node.MethodName, node.CallParameters.Count, node.ReferencedMethod.Formals.Count), node);
                    return;
                }

                ValidateCallParameterTypes(node, node.ReferencedMethod);
            }

            private void ReportError(ErrorTypes type, string errorMsg, SyntaxElement node,
                SyntaxElement referencedNode = null)
            {
                if (referencedNode == null)
                {
                    _parent._errors.ReportError(type, errorMsg, node);
                }
                else
                {
                    _parent._errors.ReportError(type, errorMsg, node, referencedNode);
                }
                _checkOK = false;
            }

            private void ValidateCallParameterTypes(MethodInvocation node, MethodDeclaration methodDecl)
            {
                var callParamTypes = node.CallParameters.Select<IExpression, IType>((expr) => expr.Type).ToList();
                for (int i = 0; i < methodDecl.Formals.Count; i++)
                {
                    var callParamType = callParamTypes[i];
                    var formalParamType = methodDecl.Formals[i].Type;
                    if (!callParamType.IsAssignableTo(formalParamType))
                    {
                        var msg = String.Format(
                            "Wrong type of argument to method {0}. Expected {1} but found {2}.",
                            node.MethodName, formalParamType.Name, callParamType.Name);
                        ReportError(ErrorTypes.TypeError, msg, node);
                    }
                }
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
                    var methodFormal = method.Formals[i].Type;
                    var superFormal = superClassMethod.Formals[i].Type;
                    // The method's formal parameter cannot be a subtype of the
                    // superclass method's formal parameter type:
                    // http://docs.oracle.com/javase/specs/jls/se7/html/jls-8.html#jls-8.4.2
                    // (This would be overloading.)
                    if (methodFormal != ErrorType.GetInstance() && superFormal != ErrorType.GetInstance())
                    {
                        formalsEqual = methodFormal.Equals(superFormal);
                    }
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
                ReportError(ErrorTypes.TypeError, errormsg, node);
            }

            private void CheckOperandCompatibility(BinaryOperatorExpression node, IType left, IType right)
            {
                if (left.IsAssignableTo(right) || right.IsAssignableTo(left))
                    return;
                ReportError(ErrorTypes.TypeError,
                    String.Format("Cannot apply operator {0} on arguments of type {1} and {2}.",
                    node.Operator, left.Name, right.Name), node);
            }

            private void CheckReturnTypes(MethodDeclaration node, MethodSymbol method)
            {
                while (_returnTypes.Count > 0)
                {
                    var returnType = _returnTypes.Pop();
                    if (!returnType.IsAssignableTo(method.Type))
                    {   // The type of object returned by the return statement
                        // must match the method's declared return type.
                        ReportError(ErrorTypes.TypeError,
                            String.Format("Cannot convert expression of type {0} to {1}.",
                            returnType.Name, method.Type.Name), node);
                    }
                }
            }

        }

    }
}
