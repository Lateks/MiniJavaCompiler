using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler.SemanticAnalysis
{
    public interface INodeVisitor
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
        void Visit(UnaryOperatorExpression node);
        void Visit(BinaryOpExpression node);
        void Visit(BooleanLiteralExpression node);
        void Visit(ThisExpression node);
        void Visit(ArrayIndexingExpression node);
        void Visit(VariableReferenceExpression node);
        void Visit(IntegerLiteralExpression node);

        // These are for handling e.g. scope exits when needed.
        void Exit(ClassDeclaration node);
        void Exit(MainClassDeclaration node);
        void Exit(MethodDeclaration node);
        void Exit(BlockStatement node);
    }
}