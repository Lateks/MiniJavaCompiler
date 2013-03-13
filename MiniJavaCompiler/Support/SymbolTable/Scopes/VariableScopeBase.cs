using System;
using System.Collections.Generic;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.Support.SymbolTable.Scopes
{
    public abstract class VariableScopeBase : ScopeBase, IVariableScope
    {
        public bool IsLocalScope { get; private set; }

        protected VariableScopeBase(IScope enclosingScope, bool isLocalScope = false)
            : base(enclosingScope)
        {
            IsLocalScope = isLocalScope;
        }

        public virtual VariableSymbol ResolveLocalVariable(string name)
        {
            if (_variableTable.ContainsKey(name))
            {
                return _variableTable[name];
            }
            if (EnclosingScope == null ||
                !(EnclosingScope is IVariableScope) ||
                !((IVariableScope)EnclosingScope).IsLocalScope)
            {
                return null;
            }
            return ((IVariableScope)EnclosingScope).ResolveLocalVariable(name);
        }

        public new virtual bool Define(VariableSymbol sym)
        {
            return base.Define(sym);
        }
    }
}
