using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public struct SymbolTable
    {
        public GlobalScope GlobalScope;
        public Dictionary<ISyntaxTreeNode, IScope> Scopes;
        public Dictionary<Symbol, ISyntaxTreeNode> Definitions;
    }
}
