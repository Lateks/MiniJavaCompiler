using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class BinaryOperatorExpression : SyntaxElement, IExpression
    {
        public MiniJavaInfo.Operator Operator { get; private set; }
        public IExpression LeftOperand { get; private set; }
        public IExpression RightOperand { get; private set; }
        public IType Type { get; set; }
        public System.Reflection.Emit.Label? AfterLabel { get; set; }

        public BinaryOperatorExpression(MiniJavaInfo.Operator op, IExpression lhs, IExpression rhs,
            int row, int col)
            : base(row, col)
        {
            Operator = op;
            LeftOperand = lhs;
            RightOperand = rhs;
            AfterLabel = null;
        }

        public override void Accept(INodeVisitor visitor)
        {
            LeftOperand.Accept(visitor);
            visitor.VisitAfterLHS(this);
            RightOperand.Accept(visitor);
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "binary operator expression";
        }
    }
}
