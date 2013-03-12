using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.FrontEnd.SemanticAnalysis
{
    public partial class SemanticsChecker
    {
        private class UninitializedLocalDetector : INodeVisitor
        {
            private SemanticsChecker _parent;
            private bool _checkOK;

            public UninitializedLocalDetector(SemanticsChecker parent)
            {
                _parent = parent;
                _checkOK = true;
            }

            public bool RunCheck()
            {
                _parent._programRoot.Accept(this);
                return _checkOK;
            }

            public void Visit(VariableReferenceExpression node)
            {
                var scope = _parent._symbolTable.Scopes[node];
                var variableSymbol = scope.ResolveVariable(node.Name);
                if (variableSymbol == null) return;

                var declaration = (VariableDeclaration) _parent._symbolTable.Definitions[variableSymbol];
                if (declaration.VariableKind != VariableDeclaration.Kind.Local) return;

                if (node.UsedAsAddress)
                {
                    declaration.IsInitialized = true;
                }
                else if (!declaration.IsInitialized &&
                    !_parent._errors.HasErrorReportForReferenceTo(ErrorTypes.UninitializedLocal, declaration))
                {
                    _checkOK = false;
                    _parent._errors.ReportError(
                        ErrorTypes.UninitializedLocal,
                        String.Format("variable {0} might not have been initialized",
                        node.Name), node, declaration);
                }
            }

            public void Visit(Program node) { }

            public void Visit(VariableDeclaration node) { }

            public void Visit(PrintStatement node) { }

            public void Visit(ReturnStatement node) { }

            public void Visit(AssertStatement node) { }

            public void Visit(AssignmentStatement node) { }

            public void Visit(MethodInvocation node) { }

            public void Visit(InstanceCreationExpression node) { }

            public void Visit(UnaryOperatorExpression node) { }

            public void Visit(BinaryOperatorExpression node) { }

            public void Visit(BooleanLiteralExpression node) { }

            public void Visit(ThisExpression node) { }

            public void Visit(ArrayIndexingExpression node) { }

            public void Visit(IntegerLiteralExpression node) { }

            public void Visit(ClassDeclaration node) { }

            public void Visit(MethodDeclaration node) { }

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
        }
    }
}
