using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable.Scopes;
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
        // Checks references and annotates the tree with type information
        // for the type checking phase.
        private class ReferenceChecker : NodeVisitorBase
        {
            private readonly SemanticsChecker _parent;
            private bool _checkOK;

            public ReferenceChecker(SemanticsChecker parent)
            {
                _parent = parent;
                _checkOK = true;
            }

            public bool RunCheck()
            {
                _parent._programRoot.Accept(this);
                return _checkOK;
            }

            public override void Visit(VariableDeclaration node)
            {
                if (node.Type is ErrorType)
                {
                    _checkOK = false;
                }
            }

            public override void Visit(MethodDeclaration node)
            {
                node.DeclaringType = GetSurroundingClass(node);
                Debug.Assert(node.DeclaringType != null);
                if (node.ReturnType is ErrorType)
                {
                    _checkOK = false;
                }
            }

            public override void Visit(AssignmentStatement node)
            {
                if (node.LeftHandSide is ILValueExpression)
                {   // Whether an lvalue expression is being used "as an address"
                    // or just as a regular reference matters in both reference checks
                    // (reference to uninitialized variable) and code generation.
                    ((ILValueExpression)node.LeftHandSide).UsedAsAddress = true;
                }
            }

            public override void Visit(MethodInvocation node)
            {
                MethodSymbol method = ResolveMethod(node, node.MethodOwner.Type);
                if (method == null)
                {
                    node.Type = ErrorType.GetInstance();
                }
                else
                {
                    if (method.Declaration != null)
                    {   // There is no AST node declaration for built-in methods (namely, array length).
                        node.ReferencedMethod = (MethodDeclaration)method.Declaration;
                    }
                    node.Type = method.Type;
                }
            }

            public override void Visit(InstanceCreationExpression node)
            {
                node.Type = CheckCreatedType(node);
            }

            public override void Visit(UnaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                node.Type = _parent._symbolTable.ResolveType(op.ResultType).Type;
            }

            public override void Visit(BinaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                node.Type = _parent._symbolTable.ResolveType(op.ResultType).Type;
            }

            public override void Visit(BooleanLiteralExpression node)
            {
                node.Type = _parent._symbolTable.ResolveType(MiniJavaInfo.BoolType).Type;
            }

            public override void Visit(ThisExpression node)
            {
                node.Type = GetSurroundingClass(node).Type;
            }

            public override void Visit(ArrayIndexingExpression node)
            {
                var arrayType = node.ArrayExpr.Type;
                node.Type = arrayType is ArrayType ? (IType)
                    (arrayType as ArrayType).ElementType : ErrorType.GetInstance();
            }

            public override void Visit(VariableReferenceExpression node)
            {
                var symbol = node.Scope.ResolveVariable(node.Name);
                if (symbol == null || !VariableDeclaredBeforeReference(symbol, node))
                {
                    ReportError(ErrorTypes.LvalueReference,
                        String.Format("Cannot find symbol {0}.", node.Name), node);
                }
                else if (symbol != null && symbol.Type == ErrorType.GetInstance())
                {
                    _checkOK = false;
                }

                node.Type = symbol == null ? ErrorType.GetInstance() : symbol.Type;
            }

            public override void Visit(IntegerLiteralExpression node)
            {
                node.Type = _parent._symbolTable.ResolveType(MiniJavaInfo.IntType).Type;
            }

            private void ReportError(ErrorTypes type, string errorMsg, SyntaxElement node)
            {
                _parent._errors.ReportError(type, errorMsg, node);
                _checkOK = false;
            }

            private MethodSymbol ResolveMethod(MethodInvocation node, IType methodOwnerType)
            {
                MethodSymbol method = null;
                if (methodOwnerType == VoidType.GetInstance())
                {
                    ReportError(
                        ErrorTypes.LvalueReference,
                        String.Format("Void cannot be dereferenced."),
                        node);
                }
                else if (methodOwnerType is ScalarType || methodOwnerType is ArrayType)
                {
                    var typeSymbol = _parent._symbolTable.ResolveType(methodOwnerType.Name);
                    method = typeSymbol.Scope.ResolveMethod(node.MethodName);
                    if (method == null)
                    {
                        ReportError(ErrorTypes.MethodReference,
                            String.Format("Cannot find symbol {0}.", node.MethodName), node);
                    }
                }
                else // ErrorType
                {
                    _checkOK = false;
                }
                return method;
            }

            private IType CheckCreatedType(InstanceCreationExpression node)
            {
                IType createdType;
                if (node.CreatedTypeName == MiniJavaInfo.VoidType)
                {
                    Debug.Assert(node.IsArrayCreation); // ...otherwise this should not have passed the parser.
                    ReportError(ErrorTypes.TypeError, String.Format("Illegal type void for array elements."), node);
                    createdType = ErrorType.GetInstance();
                }
                else
                {
                    var createdTypeSymbol = node.IsArrayCreation ?
                        _parent._symbolTable.ResolveArrayType(node.CreatedTypeName) :
                        _parent._symbolTable.ResolveType(node.CreatedTypeName);
                    if (createdTypeSymbol == null)
                    {
                        if (!_parent._errors.HasErrorReportForNode(ErrorTypes.TypeReference, node))
                        {
                            ReportError(ErrorTypes.TypeReference,
                                String.Format("Unknown type {0}.", node.CreatedTypeName), node);
                        }
                        else
                        {
                            _checkOK = false;
                        }
                        createdType = ErrorType.GetInstance();
                    }
                    else
                    {
                        createdType = createdTypeSymbol.Type;
                    }
                }
                return createdType;
            }

            private bool VariableDeclaredBeforeReference(VariableSymbol varSymbol,
                VariableReferenceExpression reference)
            {
                var declaration = (VariableDeclaration)varSymbol.Declaration;
                if (declaration.VariableKind == VariableDeclaration.Kind.Local)
                {
                    return declaration.Row < reference.Row ||
                        (declaration.Row == reference.Row && declaration.Col < reference.Col);
                }
                return true;
            }

            private TypeSymbol GetSurroundingClass(ISyntaxTreeNode node)
            {
                var scope = node.Scope;
                while (!(scope == null) && !(scope is ClassScope))
                {
                    scope = scope.EnclosingScope;
                }
                return scope != null ? (scope as ClassScope).Symbol : null;
            }
        }
    }
}
