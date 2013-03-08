using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class ArrayIndexingExpression : SyntaxElement, IExpression
    {
        public IExpression ArrayExpr { get; private set; }
        public IExpression IndexExpr { get; private set; }
        public IType Type { get; set; }

        public ArrayIndexingExpression(IExpression arrayReference,
            IExpression arrayIndex, int row, int col)
            : base(row, col)
        {
            ArrayExpr = arrayReference;
            IndexExpr = arrayIndex;
        }

        public override void Accept(INodeVisitor visitor)
        {
            IndexExpr.Accept(visitor);
            ArrayExpr.Accept(visitor);
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "array indexing expression";
        }
    }
}