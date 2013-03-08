using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class IntegerLiteralExpression : SyntaxElement, IExpression
    {
        public string Value { get; private set; }
        public int IntValue { get; set; }
        public IType Type { get; set; }

        public IntegerLiteralExpression(string value, int row, int col)
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
            return "integer literal";
        }
    }
}
