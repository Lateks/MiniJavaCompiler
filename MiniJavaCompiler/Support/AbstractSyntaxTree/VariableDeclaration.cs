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
        public bool IsInitialized { get; set; }

        public VariableDeclaration(string name, string type, bool isArray,
            Kind kind, short localIndex, int row, int col)
            : base(name, type, isArray, row, col)
        {
            VariableKind = kind;
            LocalIndex = localIndex;
            IsInitialized = VariableKind != Kind.Local; // Locals are not initialized automatically.
                                                        // Whether they are initialized or not cannot be
                                                        // known before tree traversal in the type and
                                                        // reference checking phase.
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}