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
        void Visit(MethodInvocation node);
        void Visit(InstanceCreationExpression node);
        void Visit(UnaryOperatorExpression node);
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
        void Visit(BinaryOperatorExpression node);

        // Visits in the middle of handling the node's children.
        void VisitAfterBody(WhileStatement node);
        void VisitAfterCondition(IfStatement node);
        void VisitAfterThenBranch(IfStatement node);
        // For binary logical operator short circuiting in code generation.
        void VisitAfterLHS(BinaryOperatorExpression node);

        // These are for handling e.g. scope exits when needed.
        void Exit(ClassDeclaration node);
        void Exit(MethodDeclaration node);
        void Exit(BlockStatement node);
        void Exit(WhileStatement node);
        void Exit(IfStatement node);
    }

    public abstract class NodeVisitorBase : INodeVisitor
    {
        public virtual void Visit(Program node) { }
        public virtual void Visit(VariableDeclaration node) { }
        public virtual void Visit(PrintStatement node) { }
        public virtual void Visit(ReturnStatement node) { }
        public virtual void Visit(AssertStatement node) { }
        public virtual void Visit(AssignmentStatement node) { }
        public virtual void Visit(MethodInvocation node) { }
        public virtual void Visit(InstanceCreationExpression node) { }
        public virtual void Visit(UnaryOperatorExpression node) { }
        public virtual void Visit(BinaryOperatorExpression node) { }
        public virtual void Visit(BooleanLiteralExpression node) { }
        public virtual void Visit(ThisExpression node) { }
        public virtual void Visit(ArrayIndexingExpression node) { }
        public virtual void Visit(VariableReferenceExpression node) { }
        public virtual void Visit(IntegerLiteralExpression node) { }

        public virtual void Visit(ClassDeclaration node) { }
        public virtual void Visit(MethodDeclaration node) { }
        public virtual void Visit(BlockStatement node) { }
        public virtual void Visit(WhileStatement node) { }

        public virtual void VisitAfterBody(WhileStatement node) { }
        public virtual void VisitAfterCondition(IfStatement node) { }
        public virtual void VisitAfterThenBranch(IfStatement node) { }
        public virtual void VisitAfterLHS(BinaryOperatorExpression node) { }

        public virtual void Exit(ClassDeclaration node) { }
        public virtual void Exit(MethodDeclaration node) { }
        public virtual void Exit(BlockStatement node) { }
        public virtual void Exit(WhileStatement node) { }
        public virtual void Exit(IfStatement node) { }
    }
}