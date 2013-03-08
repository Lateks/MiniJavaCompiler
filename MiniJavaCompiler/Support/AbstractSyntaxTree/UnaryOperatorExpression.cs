using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class UnaryOperatorExpression : SyntaxElement, IExpression
    {
        public IExpression Operand { get; private set; }
        public MiniJavaInfo.Operator Operator { get; private set; }
        public IType Type { get; set; }

        public UnaryOperatorExpression(MiniJavaInfo.Operator op, IExpression operand, int row, int col)
            : base(row, col)
        {
            Operator = op;
            Operand = operand;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Operand.Accept(visitor);
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "unary operator expression";
        }
    }
}