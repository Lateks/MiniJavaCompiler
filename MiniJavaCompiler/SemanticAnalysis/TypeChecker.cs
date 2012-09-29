using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler.SemanticAnalysis
{
    public class TypeChecker : INodeVisitor
    {
        public void Visit(Program node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ClassDeclaration node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MainClassDeclaration node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableDeclaration node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MethodDeclaration node)
        {
            throw new NotImplementedException();
        }

        public void Visit(PrintStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ReturnStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(BlockStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(AssertStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(AssignmentStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(IfStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(WhileStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MethodInvocation node)
        {
            throw new NotImplementedException();
        }

        public void Visit(InstanceCreationExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(UnaryNotExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(BinaryOpExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(BooleanLiteralExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ThisExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ArrayIndexingExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableReferenceExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(IntegerLiteralExpression node)
        {
            throw new NotImplementedException();
        }
    }
}