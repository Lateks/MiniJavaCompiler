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
        // that references to variables and methods are valid (but does
        // not check variable initialization).
        //
        // It also annotates the tree with type information and other
        // relevant data. (TODO: should this be done as a separate pass
        // to clean up the TypeChecker?)
        private partial class TypeChecker : INodeVisitor
        {
            private SemanticsChecker _parent;
            private readonly Stack<IType> _returnTypes; /* When return statements are encountered, the types of
                                                         * the expressions they return will be stored here and
                                                         * checked when exiting the method declaration.
                                                         */
            private bool _checkFailed;

            public TypeChecker(SemanticsChecker parent)
            {
                _parent = parent;
                _returnTypes = new Stack<IType>();
                _checkFailed = false;
            }

            public bool RunCheck()
            {
                _parent._programRoot.Accept(this);
                return !_checkFailed;
            }

            public void Visit(Program node) { }

            public void Visit(ClassDeclaration node) { }

            public void Visit(VariableDeclaration node) { }

            public void Visit(MethodDeclaration node)
            {  // Check that the method does not overload a method in a superclass.
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

            public void Visit(PrintStatement node)
            {   // Argument must be an integer.
                var type = node.Argument.Type;
                if (type is ErrorType) return; // Type errors are never checked in recovery.
                if (type.Name != MiniJavaInfo.IntType)
                {
                    ReportError(String.Format("Cannot print expression of type {0}.", type.Name), node);
                }
            }

            public void Visit(ReturnStatement node)
            {
                var argType = node.ReturnValue.Type;
                _returnTypes.Push(argType);
            }

            public void Visit(BlockStatement node) { }

            public void Visit(AssertStatement node)
            {
                ArgumentShouldBeBoolean(node, node.Condition);
            }

            public void Visit(AssignmentStatement node)
            {   // The type of right hand side must match the left hand side
                // and left hand side needs to be an lvalue.
                var lhsType = node.LeftHandSide.Type;
                var rhsType = node.RightHandSide.Type;
                if (node.LeftHandSide is ILValueExpression || node.LeftHandSide is ErrorType)
                {
                    if (node.LeftHandSide is ILValueExpression)
                    {
                        ((ILValueExpression)node.LeftHandSide).UsedAsAddress = true;
                    }
                    if (!(rhsType.IsAssignableTo(lhsType)))
                    {
                        // ErrorType should be assignable both ways.
                        Debug.Assert(!(lhsType is ErrorType || rhsType is ErrorType));
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

            public void VisitAfterCondition(IfStatement node) { }

            public void VisitAfterThenBranch(IfStatement node) { }

            public void Exit(IfStatement node)
            {
                ArgumentShouldBeBoolean(node, node.Condition);
            }

            public void Visit(WhileStatement node) { }

            public void VisitAfterBody(WhileStatement node) { }

            public void Exit(WhileStatement node)
            {
                ArgumentShouldBeBoolean(node, node.LoopCondition);
            }

            public void Visit(MethodInvocation node)
            {   // Note: in this implementation the main method cannot be called
                // from inside the program because there would be no sensible use
                // for such a method call - and there are no other static methods
                // - so implementing it would have been pointless.
                var methodOwnerType = node.MethodOwner.Type;
                MethodSymbol method = ResolveMethod(node, methodOwnerType);

                ValidateMethodCall(method, node, methodOwnerType);

                // Expected return type, may be void.
                node.Type = method == null ? ErrorType.GetInstance() : method.Type;
            }

            public void Visit(InstanceCreationExpression node)
            {
                IType createdType = CheckCreatedType(node);
                CheckArraySizeType(node);
                node.Type = createdType ?? ErrorType.GetInstance();
            }

            public void Visit(UnaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                var expectedOperandType = _parent._symbolTable.ResolveTypeName(op.OperandType).Type;
                var actualOperandType = node.Operand.Type;
                if (!actualOperandType.IsAssignableTo(expectedOperandType))
                {
                    ReportError(String.Format("Cannot apply operator {0} on operand of type {1}.",
                        node.Operator, actualOperandType.Name), node);
                }
                node.Type = _parent._symbolTable.ResolveTypeName(op.ResultType).Type;
            }

            public void Visit(BinaryOperatorExpression node)
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
                node.Type = _parent._symbolTable.ResolveTypeName(op.ResultType).Type;
            }

            public void Visit(BooleanLiteralExpression node)
            {
                node.Type = _parent._symbolTable.ResolveTypeName(MiniJavaInfo.BoolType).Type;
            }

            public void Visit(ThisExpression node)
            {
                node.Type = _parent._symbolTable.ResolveClass(node).Type;
            }

            public void Visit(ArrayIndexingExpression node)
            {
                var arrayType = node.ArrayExpr.Type;
                if (!(arrayType is ErrorType) && !(arrayType is ArrayType))
                {   // Only arrays can be indexed. Resolving errors are ignored.
                    ReportError(String.Format("Cannot index into expression of type {0}.", arrayType.Name), node);
                }
                var indexType = node.IndexExpr.Type;
                if (!indexType.IsAssignableTo(_parent._symbolTable.ResolveTypeName(MiniJavaInfo.IntType).Type))
                {   // Array must be indexed with an expression that evaluates into an int value.
                    ReportError("Invalid array index.", node);
                }
                node.Type = arrayType is ArrayType ?
                    (IType)(arrayType as ArrayType).ElementType : ErrorType.GetInstance();
            }

            public void Visit(VariableReferenceExpression node)
            {
                var scope = _parent._symbolTable.Scopes[node];
                var symbol = scope.ResolveVariable(node.Name);
                if (symbol == null || !VariableDeclaredBeforeReference(symbol, node))
                {
                    ReportError(String.Format("Cannot resolve symbol {0}.", node.Name), node);
                }
                else if (symbol != null && symbol.Type is ErrorType)
                {
                    _checkFailed = true;
                }

                node.Type = symbol == null ? ErrorType.GetInstance() : symbol.Type;
            }

            public void Visit(IntegerLiteralExpression node)
            {
                int value;
                if (!Int32.TryParse(node.Value, out value))
                {
                    ReportError(String.Format("Cannot fit integer literal {0} into a 32-bit integer variable.",
                        node.Value), node);
                }
                node.IntValue = value;
                node.Type = _parent._symbolTable.ResolveTypeName(MiniJavaInfo.IntType).Type;
            }

            public void Exit(ClassDeclaration node) { }

            public void Exit(MethodDeclaration node)
            {
                var method = _parent._symbolTable.Scopes[node].ResolveMethod(node.Name);
                int numReturnStatements = _returnTypes.Count;
                if (method.Type == VoidType.GetInstance())
                {   // Void methods cannot have return statements
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
                    // Return types can be checked even if some branches were missing
                    // a return statement.
                    CheckReturnTypes(node, method);
                }
            }

            public void Exit(BlockStatement node) { }
        }
    }
}
