using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class ReturnStatement : SyntaxElement, IStatement
    {
        public IExpression ReturnValue { get; private set; }

        public ReturnStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            ReturnValue = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            ReturnValue.Accept(visitor);
            visitor.Visit(this);
        }
    }
}
