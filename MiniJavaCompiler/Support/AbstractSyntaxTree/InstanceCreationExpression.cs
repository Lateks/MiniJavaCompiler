using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class InstanceCreationExpression : SyntaxElement, IExpression
    {
        public string CreatedTypeName { get; private set; }
        public bool IsArrayCreation { get; private set; }
        public IExpression ArraySize { get; private set; }
        public IType Type { get; set; }

        public InstanceCreationExpression(string type, int row, int col, IExpression arraySize = null)
            : base(row, col)
        {
            CreatedTypeName = type;
            ArraySize = arraySize;
            IsArrayCreation = arraySize != null;
        }

        public override void Accept(INodeVisitor visitor)
        {
            if (ArraySize != null)
            {
                ArraySize.Accept(visitor);
            }
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "instance creation expression";
        }
    }
}
