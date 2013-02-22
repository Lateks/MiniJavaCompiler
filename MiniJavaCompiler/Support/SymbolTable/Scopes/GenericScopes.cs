using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.Support.SymbolTable.Scopes
{
    public class GlobalScope : ScopeBase, ITypeScope
    {
        public new bool Define(SimpleTypeSymbol sym)
        {
            return base.Define(sym);
        }
    }

    // Used for block scopes.
    public class LocalScope : ScopeBase, IVariableScope
    {
        public LocalScope(IVariableScope enclosingScope) : base(enclosingScope) { }

        public new bool Define(VariableSymbol sym)
        {
            return base.Define(sym);
        }
    }

    // Used for error recovery when the correct type of scope cannot be constructed.
    public class ErrorScope : ScopeBase, IVariableScope
    {
        public ErrorScope(IScope enclosingScope) : base(enclosingScope) { }

        public new bool Define(VariableSymbol sym)
        {
            return base.Define(sym);
        }
    }
}
