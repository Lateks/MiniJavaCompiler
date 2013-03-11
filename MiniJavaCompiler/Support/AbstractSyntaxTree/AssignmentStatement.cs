using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class AssignmentStatement : SyntaxElement, IStatement
    {
        public IExpression LeftHandSide { get; private set; }
        public IExpression RightHandSide { get; private set; }

        public AssignmentStatement(IExpression lhs, IExpression rhs, int row, int col)
            : base(row, col)
        {
            LeftHandSide = lhs;
            RightHandSide = rhs;
        }

        public override void Accept(INodeVisitor visitor)
        {
            LeftHandSide.Accept(visitor);
            RightHandSide.Accept(visitor);
            visitor.Visit(this);
        }
    }
}