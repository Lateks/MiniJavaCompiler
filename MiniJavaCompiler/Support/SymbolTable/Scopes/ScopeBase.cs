using System;
using System.Collections.Generic;
using System.Diagnostics;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.Support.SymbolTable.Scopes
{
    // This class defines the base functionality for all scopes.
    public abstract class ScopeBase : IScope
    {
        protected readonly Dictionary<string, SimpleTypeSymbol> _typeTable;
        protected readonly Dictionary<string, MethodSymbol> _methodTable;
        protected readonly Dictionary<string, VariableSymbol> _variableTable;

        public IScope EnclosingScope
        {
            get;
            private set;
        }

        protected ScopeBase() : this(null) { }

        protected ScopeBase(IScope enclosingScope)
        {
            _typeTable = new Dictionary<string, SimpleTypeSymbol>();
            _methodTable = new Dictionary<string, MethodSymbol>();
            _variableTable = new Dictionary<string, VariableSymbol>();
            EnclosingScope = enclosingScope;
        }

        protected bool Define(VariableSymbol sym)
        {
            return DefineSymbolIn<VariableSymbol>(sym, _variableTable);
        }

        protected bool Define(MethodSymbol sym)
        {
            return DefineSymbolIn<MethodSymbol>(sym, _methodTable);
        }

        protected bool Define(SimpleTypeSymbol sym)
        {
            return DefineSymbolIn<SimpleTypeSymbol>(sym, _typeTable);
        }

        private bool DefineSymbolIn<T>(T sym, Dictionary<string, T> lookupTable)
            where T : Symbol
        {
            if (lookupTable.ContainsKey(sym.Name))
            {
                return false;
            }
            lookupTable.Add(sym.Name, sym);
            return true;
        }

        public virtual MethodSymbol ResolveMethod(string name)
        {
            if (_methodTable.ContainsKey(name))
            {
                return _methodTable[name];
            }
            return EnclosingScope == null ? null : EnclosingScope.ResolveMethod(name);
        }

        public virtual VariableSymbol ResolveVariable(string name)
        {
            if (_variableTable.ContainsKey(name))
            {
                return _variableTable[name];
            }
            return EnclosingScope == null ? null : EnclosingScope.ResolveVariable(name);
        }

        public virtual SimpleTypeSymbol ResolveType(string name)
        {
            if (_typeTable.ContainsKey(name))
            {
                return _typeTable[name];
            }
            return EnclosingScope == null ? null : EnclosingScope.ResolveType(name);
        }
    }
}
