﻿using System;
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
            void Visit(Program node);
            void Visit(ClassDeclaration node);
            void Visit(MainClassDeclaration node);
            void Visit(VariableDeclaration node);
            void Visit(MethodDeclaration node);
            void Visit(PrintStatement node);
            void Visit(ReturnStatement node);
            void Visit(BlockStatement node);
            void Visit(AssertStatement node);
            void Visit(AssignmentStatement node);
            void Visit(IfStatement node);
            void Visit(WhileStatement node);
            void Visit(MethodInvocation node);
            void Visit(InstanceCreationExpression node);
            void Visit(UnaryNotExpression node);
            void Visit(ArithmeticOpExpression node);
            void Visit(LogicalOpExpression node);
            void Visit(BooleanLiteralExpression node);
            void Visit(ThisExpression node);
            void Visit(ArrayIndexingExpression node);
            void Visit(VariableReferenceExpression node);
            void Visit(IntegerLiteralExpression node);
        }
    }
}