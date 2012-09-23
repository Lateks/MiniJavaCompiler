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
        private List<ErrorMessage> errorMessages;

        public TypeSetBuilder(Program node)
        {
            syntaxTree = node;
            errorMessages = new List<ErrorMessage>();
            types = new HashSet<string>(new string[] { "int", "boolean" });
        }

        public IEnumerable<string> BuildTypeSet()
        {
            syntaxTree.Accept(this);
            if (errorMessages.Count > 0)
                throw new ErrorReport(errorMessages);
            return types;
        }

        private void ReportConflict(string typeName, int row, int col)
        {
            errorMessages.Add(new ErrorMessage("Conflicting definitions for " +
                    typeName + ".", row, col));
        }

        public void Visit(Program node) { }

        public void Visit(ClassDeclaration node)
        {
            if (types.Contains(node.Name))
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
            if (types.Contains(node.Name))
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

        public void Visit(ArithmeticOpExpression node) { }

        public void Visit(LogicalOpExpression node) { }

        public void Visit(BooleanLiteralExpression node) { }

        public void Visit(ThisExpression node) { }

        public void Visit(ArrayIndexingExpression node) { }

        public void Visit(VariableReferenceExpression node) { }

        public void Visit(IntegerLiteralExpression node) { }
    }
}