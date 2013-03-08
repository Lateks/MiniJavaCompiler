using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class IfStatement : SyntaxElement, IStatement
    {
        public IExpression Condition { get; private set; }
        public BlockStatement ThenBranch { get; private set; }
        public BlockStatement ElseBranch { get; private set; }

        public IfStatement(IExpression booleanExp, IStatement thenBranch,
            IStatement elseBranch, int row, int col)
            : base(row, col)
        {
            Condition = booleanExp;
            ThenBranch = WrapInBlock(thenBranch);
            ElseBranch = WrapInBlock(elseBranch);
        }

        private BlockStatement WrapInBlock(IStatement statement)
        {
            if (statement == null) // Can be null if errors are encountered in the parsing phase.
            {
                return null;
            }
            if (statement is BlockStatement)
            {
                return statement as BlockStatement;
            }
            var statementNode = (SyntaxElement)statement;
            return new BlockStatement(new List<IStatement>() { statement },
                statementNode.Row, statementNode.Col);
        }

        public override void Accept(INodeVisitor visitor)
        {
            Condition.Accept(visitor);
            ThenBranch.Accept(visitor);
            if (ElseBranch != null)
            {
                ElseBranch.Accept(visitor);
            }
            visitor.Visit(this);
        }
    }
}