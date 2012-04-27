using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler
{
    namespace SemanticAnalysis
    {
        public interface NodeVisitor
        {
            void visit(Program node);
            void visit(ClassDeclaration node);
            void visit(MainClassDeclaration node);
            void visit(VariableDeclaration node);
            void visit(MethodDeclaration node);
            void visit(PrintStatement node);
            void visit(ReturnStatement node);
            void visit(BlockStatement node);
            void visit(AssertStatement node);
            void visit(AssignmentStatement node);
            void visit(IfStatement node);
            void visit(WhileStatement node);
            void visit(MethodInvocation node);
            void visit(InstanceCreationExpression node);
            void visit(UnaryNotExpression node);
            void visit(ArithmeticOpExpression node);
            void visit(LogicalOpExpression node);
            void visit(BooleanLiteralExpression node);
            void visit(ThisExpression node);
            void visit(ArrayIndexingExpression node);
            void visit(VariableReferenceExpression node);
            void visit(IntegerLiteralExpression node);
        }
    }
}
