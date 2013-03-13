using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.Support.SymbolTable.Scopes
{
    public class GlobalScope : ScopeBase, ITypeScope
    {
        public new bool Define(TypeSymbol sym)
        {
            return base.Define(sym);
        }
    }

    // Used for block scopes.
    public class LocalScope : VariableScopeBase
    {
        // Note: A local scope can only be defined inside a variable scope
        // (another block or a method body).
        public LocalScope(IVariableScope enclosingScope)
            : base(enclosingScope, true) { }

        public override bool Define(VariableSymbol sym)
        {
            if (ResolveLocalVariable(sym.Name) != null)
            {
                return false;
            }
            return base.Define(sym);
        }
    }

    // Used for error recovery when the correct type of scope cannot be constructed.
    public class ErrorScope : VariableScopeBase
    {
        public ErrorScope(IScope enclosingScope) : base(enclosingScope) { }
    }
}
