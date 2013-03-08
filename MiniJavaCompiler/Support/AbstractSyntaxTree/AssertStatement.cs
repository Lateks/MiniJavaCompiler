using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class AssertStatement : SyntaxElement, IStatement
    {
        public IExpression Condition { get; private set; }

        public AssertStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            Condition = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Condition.Accept(visitor);
            visitor.Visit(this);
        }
    }
}