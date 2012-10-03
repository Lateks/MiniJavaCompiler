using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.SemanticAnalysis
{
    public class TypeSetBuilder : INodeVisitor
    {
        private readonly Program _syntaxTree;
        private readonly HashSet<string> _types;
        private readonly IErrorReporter _errorReporter;

        public TypeSetBuilder(Program node, IErrorReporter errorReporter)
        {
            _syntaxTree = node;
            _errorReporter = errorReporter;
            _types = new HashSet<string>();
        }

        public IEnumerable<string> BuildTypeSet()
        {
            _syntaxTree.Accept(this);
            return _types;
        }

        private void ReportConflict(string typeName, int row, int col)
        {
            _errorReporter.ReportError("Conflicting definitions for " +
                    typeName + ".", row, col);
        }

        private bool NameAlreadyDefined(string name)
        {
            return _types.Contains(name) || MiniJavaInfo.IsBuiltinType(name);
        }

        public void Visit(Program node) { }

        public void Visit(ClassDeclaration node)
        {
            if (NameAlreadyDefined(node.Name))
            {
                ReportConflict(node.Name, node.Row, node.Col);
            }
            else
            {
                _types.Add(node.Name);
            }
        }

        public void Visit(MainClassDeclaration node)
        {
            if (NameAlreadyDefined(node.Name))
            {
                ReportConflict(node.Name, node.Row, node.Col);
            }
            else
            {
                _types.Add(node.Name);
            }
        }

        public void Visit(VariableDeclaration node) { }

        public void Visit(MethodDeclaration node) { }

        public void Visit(PrintStatement node) { }

        public void Visit(ReturnStatement node) { }

        public void Visit(BlockStatement node) { }

        public void Visit(AssertStatement node) { }

        public void Visit(AssignmentStatement node) { }

        public void Visit(IfStatement node) { }

        public void Visit(WhileStatement node) { }

        public void Visit(MethodInvocation node) { }

        public void Visit(InstanceCreationExpression node) { }

        public void Visit(UnaryOperatorExpression node) { }

        public void Visit(BinaryOpExpression node) { }

        public void Visit(BooleanLiteralExpression node) { }

        public void Visit(ThisExpression node) { }

        public void Visit(ArrayIndexingExpression node) { }

        public void Visit(VariableReferenceExpression node) { }

        public void Visit(IntegerLiteralExpression node) { }

        public void Exit(ClassDeclaration node) { }

        public void Exit(MainClassDeclaration node) { }

        public void Exit(MethodDeclaration node) { }

        public void Exit(BlockStatement node) { }
    }
}