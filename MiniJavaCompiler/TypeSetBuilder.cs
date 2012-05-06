using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler
{
    namespace SemanticAnalysis
    {
        public class TypeSetBuilder : NodeVisitor
        {
            private Program syntaxTree;
            private HashSet<string> types;
            private List<ErrorMessage> errorMessages;

            public TypeSetBuilder(Program node)
            {
                syntaxTree = node;
                errorMessages = new List<ErrorMessage>();
                types = new HashSet<string>(new string[] {"int", "boolean"});
            }

            public HashSet<string> BuildTypeSet()
            {
                syntaxTree.accept(this);
                if (errorMessages.Count > 0)
                    throw new ErrorReport(errorMessages);
                return types;
            }

            private void ReportConflict(string typeName, int row, int col)
            {
                errorMessages.Add(new ErrorMessage("Conflicting definitions for " +
                        typeName + ".", row, col));
            }

            public void visit(Program node) { }

            public void visit(ClassDeclaration node)
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

            public void visit(MainClassDeclaration node)
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

            public void visit(VariableDeclaration node) { }

            public void visit(MethodDeclaration node) { }

            public void visit(PrintStatement node) { }

            public void visit(ReturnStatement node) { }

            public void visit(BlockStatement node) { }

            public void visit(AssertStatement node) { }

            public void visit(AssignmentStatement node) { }

            public void visit(IfStatement node) { }

            public void visit(WhileStatement node) { }

            public void visit(MethodInvocation node) { }

            public void visit(InstanceCreationExpression node) { }

            public void visit(UnaryNotExpression node) { }

            public void visit(ArithmeticOpExpression node) { }

            public void visit(LogicalOpExpression node) { }

            public void visit(BooleanLiteralExpression node) { }

            public void visit(ThisExpression node) { }

            public void visit(ArrayIndexingExpression node) { }

            public void visit(VariableReferenceExpression node) { }

            public void visit(IntegerLiteralExpression node) { }
        }
    }
}