using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Scopes;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MiniJavaCompiler.BackEnd
{
    public partial class CodeGenerator
    {
        private class CodeGenAnalyser : NodeVisitorBase
        {
            private short _pass;
            private ISyntaxTreeNode _astRoot;
            private short _currentLocalIdx;
            private VariableDeclaration.Kind _previousVarKind;

            public CodeGenAnalyser(ISyntaxTreeNode astRoot)
            {
                _astRoot = astRoot;
                _currentLocalIdx = 0;
                _previousVarKind = VariableDeclaration.Kind.Class;
            }

            public void Analyse()
            {
                _pass = 0; // The first pass performs analysis: it detects which variables are used or not used,
                           // which variable references or array indexing expressions appear as the left-hand side
                           // of assignments and which method invocation return values are just discarded.
                _astRoot.Accept(this);
                _pass = 1; // The second pass numbers local variables and formal parameters, taking unused locals
                           // into account.
                _astRoot.Accept(this);
            }

            public override void Visit(MethodDeclaration node)
            {
                if (_pass == 0)
                {
                    CheckStatements(node.MethodBody);
                }
                else if (_pass == 1)
                {
                    _currentLocalIdx = 0;
                }
            }

            public override void Visit(VariableDeclaration node)
            {
                if (_pass != 1) return;

                if (_previousVarKind != node.VariableKind)
                {
                    _currentLocalIdx = 0;
                }
                if (node.VariableKind == VariableDeclaration.Kind.Formal ||
                    (node.VariableKind == VariableDeclaration.Kind.Local && node.Used))
                {
                    node.LocalIndex = _currentLocalIdx++;
                }
                _previousVarKind = node.VariableKind;
            }

            public override void Visit(AssignmentStatement node)
            {
                if (_pass == 0)
                {
                    (node.LeftHandSide as ILValueExpression).UsedAsAddress = true;
                }
            }

            public override void Visit(BlockStatement node)
            {
                if (_pass == 0)
                {
                    CheckStatements(node.Statements);
                }
            }

            public override void Visit(VariableReferenceExpression node)
            {
                if (_pass == 0)
                {
                    var decl = (VariableDeclaration)node.Scope.ResolveVariable(node.Name).Declaration;
                    decl.Used = true;
                }
            }

            private void CheckStatements(List<IStatement> list)
            {
                foreach (var methodInvoc in list.Where((elem) => elem is MethodInvocation))
                {   // A method invocation is used as a stand-alone statement.
                    (methodInvoc as MethodInvocation).UsedAsStatement = true;
                }
            }
        }
    }
}
