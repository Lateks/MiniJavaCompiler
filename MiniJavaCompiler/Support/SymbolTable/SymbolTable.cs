using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public class SymbolTable
    {
        public readonly GlobalScope GlobalScope;
        public readonly Dictionary<ISyntaxTreeNode, IScope> Scopes;
        public readonly Dictionary<Symbol, ISyntaxTreeNode> Definitions;

        public SymbolTable()
        {
            GlobalScope = new GlobalScope();
            Scopes = new Dictionary<ISyntaxTreeNode, IScope>();
            Definitions = new Dictionary<Symbol, ISyntaxTreeNode>();
        }

        public IType ResolveType(string typeName)
        {
            return (IType) GlobalScope.Resolve<TypeSymbol>(typeName);
        }
    }
}
