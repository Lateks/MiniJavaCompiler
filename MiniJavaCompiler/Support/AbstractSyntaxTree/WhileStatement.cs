using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class WhileStatement : SyntaxElement, IStatement
    {
        public IExpression LoopCondition { get; private set; }
        public BlockStatement LoopBody { get; private set; }
        public System.Reflection.Emit.Label ConditionLabel { get; set; }

        public WhileStatement(IExpression booleanExp, IStatement loopBody,
            int row, int col)
            : base(row, col)
        {
            LoopCondition = booleanExp;
            if (loopBody == null)
            {
                LoopBody = null;
            }
            else
            {
                LoopBody = loopBody is BlockStatement
                               ? loopBody as BlockStatement
                               : new BlockStatement(new List<IStatement>() { loopBody }, row, col);
            }
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
            LoopBody.Accept(visitor);
            visitor.VisitAfterBody(this);
            LoopCondition.Accept(visitor);
            visitor.Exit(this);
        }
    }
}