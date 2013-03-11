namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public interface INodeVisitor
    {
        void Visit(Program node);
        void Visit(VariableDeclaration node);
        void Visit(PrintStatement node);
        void Visit(ReturnStatement node);
        void Visit(AssertStatement node);
        void Visit(AssignmentStatement node);
        void Visit(IfStatement node);
        void Visit(MethodInvocation node);
        void Visit(InstanceCreationExpression node);
        void Visit(UnaryOperatorExpression node);
        void Visit(BinaryOperatorExpression node);
        void Visit(BooleanLiteralExpression node);
        void Visit(ThisExpression node);
        void Visit(ArrayIndexingExpression node);
        void Visit(VariableReferenceExpression node);
        void Visit(IntegerLiteralExpression node);

        // Visits upon "entering" the node.
        void Visit(ClassDeclaration node);
        void Visit(MethodDeclaration node);
        void Visit(BlockStatement node);
        void Visit(WhileStatement node);

        // Visits in the middle of handling the node's children.
        void VisitAfterBody(WhileStatement node);

        // These are for handling e.g. scope exits when needed.
        void Exit(ClassDeclaration node);
        void Exit(MethodDeclaration node);
        void Exit(BlockStatement node);
        void Exit(WhileStatement node);
    }
}