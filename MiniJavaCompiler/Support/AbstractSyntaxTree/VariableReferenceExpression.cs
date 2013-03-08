using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class VariableReferenceExpression : SyntaxElement, IExpression
    {
        public string Name { get; private set; }
        public IType Type { get; set; }

        public VariableReferenceExpression(string name, int row, int col)
            : base(row, col)
        {
            Name = name;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "variable reference";
        }
    }
}