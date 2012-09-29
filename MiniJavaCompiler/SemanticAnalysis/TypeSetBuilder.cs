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
        private Program syntaxTree;
        private HashSet<string> types;
        private HashSet<string> builtIns = new HashSet<string>(new [] { "int", "boolean" });
        private IErrorReporter errorReporter;

        public TypeSetBuilder(Program node, IErrorReporter errorReporter)
        {
            syntaxTree = node;
            this.errorReporter = errorReporter;
            types = new HashSet<string>();
        }

        public IEnumerable<string> BuildTypeSet()
        {
            syntaxTree.Accept(this);
            return types;
        }

        private void ReportConflict(string typeName, int row, int col)
        {
            errorReporter.ReportError("Conflicting definitions for " +
                    typeName + ".", row, col);
        }

        private bool NameAlreadyDefined(string name)
        {
            return types.Contains(name) || builtIns.Contains(name);
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
                types.Add(node.Name);
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
                types.Add(node.Name);
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

        public void Visit(UnaryNotExpression node) { }

        public void Visit(BinaryOpExpression node) { }

        public void Visit(BooleanLiteralExpression node) { }

        public void Visit(ThisExpression node) { }

        public void Visit(ArrayIndexingExpression node) { }

        public void Visit(VariableReferenceExpression node) { }

        public void Visit(IntegerLiteralExpression node) { }
    }
}