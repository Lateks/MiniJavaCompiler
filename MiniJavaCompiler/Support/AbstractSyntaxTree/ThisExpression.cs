using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class ThisExpression : SyntaxElement, IExpression
    {
        public IType Type { get; set; }

        public ThisExpression(int row, int col)
            : base(row, col) { }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "self reference (this)";
        }
    }
}