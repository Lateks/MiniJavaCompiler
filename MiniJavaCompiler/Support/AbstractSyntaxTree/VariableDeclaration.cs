using MiniJavaCompiler.Support.SymbolTable.Types;
using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class VariableDeclaration : Declaration, IStatement
    {
        public enum Kind
        {
            Formal,
            Local,
            Class
        }
        public Kind VariableKind { get; private set; }
        public short LocalIndex { get; set; }

        public VariableDeclaration(string name, string type, bool isArray,
            Kind kind, int row, int col)
            : base(name, type, isArray, row, col)
        {
            VariableKind = kind;
            LocalIndex = 0;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}