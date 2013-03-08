using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class PrintStatement : SyntaxElement, IStatement
    {
        public IExpression Argument { get; private set; }

        public PrintStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            Argument = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Argument.Accept(visitor);
            visitor.Visit(this);
        }
    }
}
