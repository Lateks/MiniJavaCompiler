using MiniJavaCompiler.Support.SymbolTable.Scopes;
using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    // This is the base interface that represents a node in the syntax tree.
    public interface ISyntaxTreeNode
    {
        IScope Scope { get; set; }
        void Accept(INodeVisitor visitor);
    }

    public interface IStatement : ISyntaxTreeNode { }

    public interface IExpression : ISyntaxTreeNode
    {
        string Describe();
        IType Type { get; set; }
    }

    public interface ILValueExpression : IExpression
    {
        bool UsedAsAddress { get; set; }
    }

    // This class defined the basic information and operations common
    // to all syntax elements.
    public abstract class SyntaxElement : ISyntaxTreeNode
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public IScope Scope { get; set; }

        protected SyntaxElement(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public abstract void Accept(INodeVisitor visitor);
    }
}
