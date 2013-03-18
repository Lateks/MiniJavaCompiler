using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using MiniJavaCompiler.Support.SymbolTable.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
            private SemanticsChecker _parent;
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

            public override void Visit(AssignmentStatement node)
            {
                if (node.LeftHandSide is ILValueExpression)
                {
                    ((ILValueExpression)node.LeftHandSide).UsedAsAddress = true;
                }
            }

            public override void Visit(MethodInvocation node)
            {
                var methodOwnerType = node.MethodOwner.Type;
                if (methodOwnerType == VoidType.GetInstance())
                {
                    ReportError(
                        ErrorTypes.LvalueReference,
                        String.Format("{0} cannot be dereferenced.",
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(methodOwnerType.Name)),
                        node);
                    node.Type = ErrorType.GetInstance();
                    return;
                }
                
                MethodSymbol method = ResolveMethod(node, methodOwnerType);
                if (method != null)
                {
                    if (_parent._symbolTable.Declarations.ContainsKey(method))
                    { // there is no AST node declaration for built-in methods (namely, array length).
                        node.ReferencedMethod = (MethodDeclaration)_parent._symbolTable.Declarations[method];
                    }
                    node.Type = method.Type;
                }
                else
                {
                    ReportError(ErrorTypes.MethodReference,
                        String.Format("Cannot find symbol {0}.", node.MethodName), node);
                    node.Type = ErrorType.GetInstance();
                }
            }

            public override void Visit(InstanceCreationExpression node)
            {
                node.Type = CheckCreatedType(node) ?? ErrorType.GetInstance();
            }

            public override void Visit(UnaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                node.Type = _parent._symbolTable.ResolveTypeName(op.ResultType).Type;
            }

            public override void Visit(BinaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                node.Type = _parent._symbolTable.ResolveTypeName(op.ResultType).Type;
            }

            public override void Visit(BooleanLiteralExpression node)
            {
                node.Type = _parent._symbolTable.ResolveTypeName(MiniJavaInfo.BoolType).Type;
            }

            public override void Visit(ThisExpression node)
            {
                node.Type = _parent._symbolTable.ResolveClass(node).Type;
            }

            public override void Visit(ArrayIndexingExpression node)
            {
                var arrayType = node.ArrayExpr.Type;
                node.Type = arrayType is ArrayType ? (IType)
                    (arrayType as ArrayType).ElementType : ErrorType.GetInstance();
            }

            public override void Visit(VariableReferenceExpression node)
            {
                var scope = _parent._symbolTable.Scopes[node];
                var symbol = scope.ResolveVariable(node.Name);
                if (symbol == null || !VariableDeclaredBeforeReference(symbol, node))
                {
                    ReportError(ErrorTypes.LvalueReference,
                        String.Format("Cannot find symbol {0}.", node.Name), node);
                }
                else if (symbol != null && symbol.Type is ErrorType)
                {
                    _checkOK = false;
                }

                node.Type = symbol == null ? ErrorType.GetInstance() : symbol.Type;
            }

            public override void Visit(IntegerLiteralExpression node)
            {
                node.Type = _parent._symbolTable.ResolveTypeName(MiniJavaInfo.IntType).Type;
            }

            public override void Visit(MethodDeclaration node)
            {
                if (node.ReturnType is ErrorType)
                {
                    _checkOK = false;
                }
            }

            private void ReportError(ErrorTypes type, string errorMsg, SyntaxElement node)
            {
                _parent._errors.ReportError(type, errorMsg, node);
                _checkOK = false;
            }

            private MethodSymbol ResolveMethod(MethodInvocation node, IType methodOwnerType)
            {
                MethodSymbol method = null;
                if (node.MethodOwner is ThisExpression)
                {   // Method called is defined by the enclosing class or its superclasses.
                    method = _parent._symbolTable.Scopes[node].ResolveMethod(node.MethodName);
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
                    if (!_parent._errors.HasErrorReportForNode(ErrorTypes.TypeReference, node))
                    {
                        ReportError(ErrorTypes.TypeReference,
                            String.Format("Cannot find symbol {0}.", node.CreatedTypeName), node);
                    }
                    else
                    {
                        _checkOK = false;
                    }
                    createdType = null;
                }
                else
                {
                    createdType = createdTypeSymbol.Type;
                }
                return createdType;
            }

            private bool VariableDeclaredBeforeReference(VariableSymbol varSymbol,
                VariableReferenceExpression reference)
            {
                var declaration = (VariableDeclaration)_parent._symbolTable.Declarations[varSymbol];
                if (declaration.VariableKind == VariableDeclaration.Kind.Local)
                {
                    return declaration.Row < reference.Row ||
                        (declaration.Row == reference.Row && declaration.Col < reference.Col);
                }
                return true;
            }
        }
    }
}
