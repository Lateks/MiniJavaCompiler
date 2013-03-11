using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class ArrayIndexingExpression : SyntaxElement, ILValueExpression
    {
        public IExpression ArrayExpr { get; private set; }
        public IExpression IndexExpr { get; private set; }
        public IType Type { get; set; }
        public bool UsedAsAddress { get; set; }

        public ArrayIndexingExpression(IExpression arrayReference,
            IExpression arrayIndex, int row, int col)
            : base(row, col)
        {
            ArrayExpr = arrayReference;
            IndexExpr = arrayIndex;
            UsedAsAddress = false;
        }

        public override void Accept(INodeVisitor visitor)
        {
            ArrayExpr.Accept(visitor);
            IndexExpr.Accept(visitor);
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "array indexing expression";
        }
    }
}