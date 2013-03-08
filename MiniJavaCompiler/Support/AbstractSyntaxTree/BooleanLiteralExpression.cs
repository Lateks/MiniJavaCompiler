using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class BooleanLiteralExpression : SyntaxElement, IExpression
    {
        public IType Type { get; set; }
        public bool Value { get; private set; }

        public BooleanLiteralExpression(bool value, int row, int col)
            : base(row, col)
        {
            Value = value;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "boolean literal";
        }
    }
}