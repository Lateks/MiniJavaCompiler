using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler
{
    namespace SemanticAnalysis
    {
        public class SymbolTableBuilder : NodeVisitor
        {
            public void visit(Program node)
            {
                throw new NotImplementedException();
            }

            public void visit(ClassDeclaration node)
            {
                throw new NotImplementedException();
            }

            public void visit(MainClassDeclaration node)
            {
                throw new NotImplementedException();
            }

            public void visit(VariableDeclaration node)
            {
                throw new NotImplementedException();
            }

            public void visit(MethodDeclaration node)
            {
                throw new NotImplementedException();
            }

            public void visit(PrintStatement node)
            {
                throw new NotImplementedException();
            }

            public void visit(ReturnStatement node)
            {
                throw new NotImplementedException();
            }

            public void visit(BlockStatement node)
            {
                throw new NotImplementedException();
            }

            public void visit(AssertStatement node)
            {
                throw new NotImplementedException();
            }

            public void visit(AssignmentStatement node)
            {
                throw new NotImplementedException();
            }

            public void visit(IfStatement node)
            {
                throw new NotImplementedException();
            }

            public void visit(WhileStatement node)
            {
                throw new NotImplementedException();
            }

            public void visit(MethodInvocation node)
            {
                throw new NotImplementedException();
            }

            public void visit(InstanceCreationExpression node)
            {
                throw new NotImplementedException();
            }

            public void visit(UnaryNotExpression node)
            {
                throw new NotImplementedException();
            }

            public void visit(ArithmeticOpExpression node)
            {
                throw new NotImplementedException();
            }

            public void visit(LogicalOpExpression node)
            {
                throw new NotImplementedException();
            }

            public void visit(BooleanLiteralExpression node)
            {
                throw new NotImplementedException();
            }

            public void visit(ThisExpression node)
            {
                throw new NotImplementedException();
            }

            public void visit(ArrayIndexingExpression node)
            {
                throw new NotImplementedException();
            }

            public void visit(VariableReferenceExpression node)
            {
                throw new NotImplementedException();
            }

            public void visit(IntegerLiteralExpression node)
            {
                throw new NotImplementedException();
            }
        }
    }
}
