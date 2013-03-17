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
        // The public methods of TypeChecker are defined in this file.
        // This class checks that operand and argument types are ok and
        // that local variables have been initialized before reference.
        private partial class TypeChecker : NodeVisitorBase
        {
            private SemanticsChecker _parent;
            private readonly Stack<IType> _returnTypes; /* When return statements are encountered, the types of
                                                         * the expressions they return will be stored here and
                                                         * checked when exiting the method declaration.
                                                         */
            private bool _checkOK;

            public TypeChecker(SemanticsChecker parent)
            {
                _parent = parent;
                _returnTypes = new Stack<IType>();
                _checkOK = true;
            }

            public bool RunCheck()
            {
                _parent._programRoot.Accept(this);
                return _checkOK;
            }

            public override void Visit(MethodDeclaration node)
            {   // Check that the method does not overload a method in a superclass.
                // Only overriding is allowed.
                var classSymbol = _parent._symbolTable.ResolveClass(node);
                var superClassMethod = classSymbol.SuperClass == null ? null :
                    classSymbol.SuperClass.Scope.ResolveMethod(node.Name);
                if (superClassMethod == null) // Did not override or overload another method.
                {
                    return;
                }

                CheckForOverloading(node, classSymbol, superClassMethod);
            }

            public override void Visit(PrintStatement node)
            {   // Argument must be an integer.
                var type = node.Argument.Type;
                if (type is ErrorType) return; // Type errors are never checked in recovery.
                if (type.Name != MiniJavaInfo.IntType)
                {
                    ReportError(
                        ErrorTypes.TypeError,
                        String.Format("Cannot print expression of type {0}.", type.Name), node);
                }
            }

            public override void Visit(ReturnStatement node)
            {
                _returnTypes.Push(node.ReturnValue.Type);
            }

            public override void Visit(AssertStatement node)
            {
                ArgumentShouldBeBoolean(node, node.Condition);
            }

            public override void Visit(AssignmentStatement node)
            {   // The type of right hand side must match the left hand side
                // and left hand side needs to be an lvalue.
                var lhsType = node.LeftHandSide.Type;
                var rhsType = node.RightHandSide.Type;
                if (node.LeftHandSide is ILValueExpression || node.LeftHandSide is ErrorType)
                {
                    if (!(rhsType.IsAssignableTo(lhsType)))
                    {
                        // ErrorType should be assignable both ways.
                        Debug.Assert(!(lhsType is ErrorType || rhsType is ErrorType));
                        ReportError(
                            ErrorTypes.TypeError,
                            String.Format("incompatible types (expected {0}, found {1}).",
                            lhsType.Name, rhsType.Name), node);
                    }
                }
                else
                {
                    ReportError(
                        ErrorTypes.LvalueReference,
                        "Assignment receiver expression is not assignable (an lvalue required).",
                        node);
                }
            }

            public override void Exit(IfStatement node)
            {
                ArgumentShouldBeBoolean(node, node.Condition);
            }

            public override void Exit(WhileStatement node)
            {
                ArgumentShouldBeBoolean(node, node.LoopCondition);
            }

            public override void Visit(MethodInvocation node)
            {
                ValidateMethodCall(node);
            }

            public override void Visit(InstanceCreationExpression node)
            {
                if (node.IsArrayCreation)
                {
                    CheckArraySizeType(node);
                }
            }

            public override void Visit(UnaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                var expectedOperandType = _parent._symbolTable.ResolveTypeName(op.OperandType).Type;
                var actualOperandType = node.Operand.Type;
                if (!actualOperandType.IsAssignableTo(expectedOperandType))
                {
                    ReportError(
                        ErrorTypes.TypeError,
                        String.Format("Cannot apply operator {0} on operand of type {1}.",
                        node.Operator, actualOperandType.Name), node);
                }
            }

            public override void Visit(BinaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                if (op.OperandType == MiniJavaInfo.AnyType)
                {   // Operands can be of any type but they must be compatible.
                    CheckOperandCompatibility(node, node.LeftOperand.Type, node.RightOperand.Type);
                }
                else
                {
                    CheckBinaryOperatorOperands(node, node.LeftOperand.Type, node.RightOperand.Type);
                }
            }

            public override void Visit(ArrayIndexingExpression node)
            {
                var arrayType = node.ArrayExpr.Type;
                if (!(arrayType is ErrorType) && !(arrayType is ArrayType))
                {   // Only arrays can be indexed. Resolving errors are ignored.
                    ReportError(
                        ErrorTypes.TypeError,
                        String.Format("Cannot index into expression of type {0}.", arrayType.Name), node);
                }
                var indexType = node.IndexExpr.Type;
                if (!indexType.IsAssignableTo(_parent._symbolTable.ResolveTypeName(MiniJavaInfo.IntType).Type))
                {   // Array must be indexed with an expression that evaluates into an int value.
                    ReportError(ErrorTypes.TypeError, "Invalid array index.", node);
                }
            }

            public override void Visit(VariableReferenceExpression node)
            {
                var scope = _parent._symbolTable.Scopes[node];
                var variableSymbol = scope.ResolveVariable(node.Name);
                if (variableSymbol == null) return; // resolving error has already been reported
                                                    // TODO: should symbol be stored on the reference expression?

                var declaration = (VariableDeclaration)_parent._symbolTable.Declarations[variableSymbol];
                if (declaration.VariableKind != VariableDeclaration.Kind.Local) return;

                if (node.UsedAsAddress)
                {
                    declaration.IsInitialized = true;
                }
                else if (!declaration.IsInitialized && !ErrorsAlreadyReported(node, declaration))
                {
                    ReportError(
                        ErrorTypes.UninitializedLocal,
                        String.Format("variable {0} might not have been initialized",
                        node.Name), node, declaration);
                }
            }

            private bool ErrorsAlreadyReported(VariableReferenceExpression reference, VariableDeclaration declaration)
            {
                return _parent._errors.HasErrorReportForReferenceTo(ErrorTypes.UninitializedLocal, declaration) ||
                    _parent._errors.HasErrorReportForNode(ErrorTypes.LvalueReference, reference);
            }

            public override void Visit(IntegerLiteralExpression node)
            {
                int value;
                if (!Int32.TryParse(node.Value, out value))
                {
                    ReportError(
                        ErrorTypes.TypeError,
                        String.Format("Cannot fit integer literal {0} into a 32-bit integer variable.",
                        node.Value), node);
                }
                node.IntValue = value;
            }

            public override void Exit(MethodDeclaration node)
            {
                var method = _parent._symbolTable.Scopes[node].ResolveMethod(node.Name);
                int numReturnStatements = _returnTypes.Count;
                if (method.Type == VoidType.GetInstance())
                {   // Void methods cannot have return statements
                    // (because Mini-Java does not allow empty return statements).
                    if (numReturnStatements > 0)
                    {
                        ReportError(
                            ErrorTypes.TypeError,
                            String.Format("cannot return a value from a method whose result type is {0}",
                            method.Type.Name), node);
                        _returnTypes.Clear();
                    }
                }
                else if (numReturnStatements == 0)
                {
                    ReportError(
                        ErrorTypes.TypeError,
                        String.Format("missing return statement in method {0}.",
                        method.Name), node);
                }
                else
                {
                    if (!AllBranchesReturnAValue(node))
                    {
                        ReportError(
                            ErrorTypes.TypeError,
                            String.Format("missing return statement in method {0}.",
                            method.Name), node);
                    }
                    // Return types can be checked even if some branches were missing
                    // a return statement.
                    CheckReturnTypes(node, method);
                }
            }
        }
    }
}
