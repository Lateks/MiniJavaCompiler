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
                _astRoot.Accept(this);
            }

            public override void Visit(MethodDeclaration node)
            {
                _currentLocalIdx = 0;
                CheckStatements(node.MethodBody);
            }

            public override void Visit(VariableDeclaration node)
            {
                if (_previousVarKind != node.VariableKind)
                {
                    _currentLocalIdx = 0;
                }
                if (node.VariableKind == VariableDeclaration.Kind.Formal ||
                    node.VariableKind == VariableDeclaration.Kind.Local)
                {
                    node.LocalIndex = _currentLocalIdx++;
                }
                _previousVarKind = node.VariableKind;
            }

            public override void Visit(AssignmentStatement node)
            {
                (node.LeftHandSide as ILValueExpression).UsedAsAddress = true;
            }

            public override void Visit(BlockStatement node)
            {
                CheckStatements(node.Statements);
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
