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
        // Checks references and annotates the tree with type information
        // for the type checking phase.
        private class ReferenceChecker : INodeVisitor
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

            public void Visit(Program node) { }

            public void Visit(VariableDeclaration node)
            {
                var typeSym = _parent._symbolTable.ResolveTypeName(node.TypeName, node.IsArray);
                if (typeSym == null)
                {
                    node.Type = ErrorType.GetInstance();
                    _checkOK = false;
                }
                else
                {
                    node.Type = typeSym.Type;
                }
            }

            public void Visit(PrintStatement node) { }

            public void Visit(ReturnStatement node) { }

            public void Visit(AssertStatement node) { }

            public void Visit(AssignmentStatement node)
            {
                if (node.LeftHandSide is ILValueExpression)
                {
                    ((ILValueExpression)node.LeftHandSide).UsedAsAddress = true;
                }
            }

            public void Visit(MethodInvocation node)
            {
                var methodOwnerType = node.MethodOwner.Type;
                if (methodOwnerType == VoidType.GetInstance())
                {
                    ReportError(
                        ErrorTypes.LvalueReference,
                        String.Format("{0} cannot be dereferenced.", methodOwnerType.Name), node);
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
                        String.Format("Cannot resolve symbol {0}.", node.MethodName), node);
                    node.Type = ErrorType.GetInstance();
                }
            }

            public void Visit(InstanceCreationExpression node)
            {
                node.Type = CheckCreatedType(node) ?? ErrorType.GetInstance();
            }

            public void Visit(UnaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
                node.Type = _parent._symbolTable.ResolveTypeName(op.ResultType).Type;
            }

            public void Visit(BinaryOperatorExpression node)
            {
                var op = MiniJavaInfo.GetOperator(node.Operator);
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
                node.Type = arrayType is ArrayType ? (IType)
                    (arrayType as ArrayType).ElementType : ErrorType.GetInstance();
            }

            public void Visit(VariableReferenceExpression node)
            {
                var scope = _parent._symbolTable.Scopes[node];
                var symbol = scope.ResolveVariable(node.Name);
                if (symbol == null || !VariableDeclaredBeforeReference(symbol, node))
                {
                    ReportError(ErrorTypes.LvalueReference,
                        String.Format("Cannot resolve symbol {0}.", node.Name), node);
                }
                else if (symbol != null && symbol.Type is ErrorType)
                {
                    _checkOK = false;
                }

                node.Type = symbol == null ? ErrorType.GetInstance() : symbol.Type;
            }

            public void Visit(IntegerLiteralExpression node)
            {
                node.Type = _parent._symbolTable.ResolveTypeName(MiniJavaInfo.IntType).Type;
            }

            public void Visit(ClassDeclaration node) { }

            public void Visit(MethodDeclaration node)
            {
                if (node.TypeName == MiniJavaInfo.VoidType)
                {
                    node.ReturnType = VoidType.GetInstance();
                }
                else
                {
                    node.ReturnType = _parent._symbolTable.ResolveTypeName(node.TypeName, node.IsArray).Type;
                }
            }

            public void Visit(BlockStatement node) { }

            public void Visit(WhileStatement node) { }

            public void VisitAfterBody(WhileStatement node) { }

            public void VisitAfterCondition(IfStatement node) { }

            public void VisitAfterThenBranch(IfStatement node) { }

            public void Exit(ClassDeclaration node) { }

            public void Exit(MethodDeclaration node) { }

            public void Exit(BlockStatement node) { }

            public void Exit(WhileStatement node) { }

            public void Exit(IfStatement node) { }

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
                    ReportError(ErrorTypes.TypeReference,
                        String.Format("Cannot resolve symbol {0}.", node.CreatedTypeName), node);
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
                if (varSymbol.Scope is TypeSymbol)
                {   // Variables defined on the class level are visible
                    // in all scopes internal to the class.
                    return true;
                }
                var declaration = (VariableDeclaration)_parent._symbolTable.Declarations[varSymbol];
                return declaration.Row < reference.Row ||
                    (declaration.Row == reference.Row && declaration.Col < reference.Col);
            }
        }
    }
}
