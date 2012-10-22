using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public interface IScope
    {
        Symbol ResolveMethod(string name);
        Symbol ResolveVariable(string name);
        Symbol ResolveType(string name);
        IScope EnclosingScope { get; }
    }

    /* Note: All Define methods in different scope interfaces return a boolean
     * value indicating whether or not the attempt to define the symbol succeeded.
     * The same kind of symbol with the same name cannot be defined twice in the
     * same scope.
     */
    public interface IVariableScope : IScope
    {
        bool Define(VariableSymbol sym);
    }

    public interface IMethodScope : IScope
    {
        bool Define(MethodSymbol sym);
    }

    public interface ITypeScope : IScope
    {
        bool Define(SimpleTypeSymbol sym);
    }

    public abstract class ScopeBase : IScope
    {
        private readonly Dictionary<string, Symbol> _typeTable;
        private readonly Dictionary<string, Symbol> _methodTable;
        private readonly Dictionary<string, Symbol> _variableTable;

        public IScope EnclosingScope
        {
            get;
            private set;
        }

        protected ScopeBase() : this(null) { }

        protected ScopeBase(IScope enclosingScope)
        {
            _typeTable = new Dictionary<string, Symbol>();
            _methodTable = new Dictionary<string, Symbol>();
            _variableTable = new Dictionary<string, Symbol>();
            EnclosingScope = enclosingScope;
        }

        protected Dictionary<string, Symbol> LookupTableFor(Symbol sym)
        {
            if (sym is MethodSymbol)
            {
                return _methodTable;
            }
            else if (sym is VariableSymbol)
            {
                return _variableTable;
            }
            else if (sym is UserDefinedTypeSymbol || sym is BuiltInTypeSymbol)
            {
                return _typeTable;
            }
            else
            {
                throw new ArgumentException(String.Format("Cannot define symbol of type {0}.",
                    sym.GetType().Name));
            }
        }

        protected bool Define(Symbol sym)
        {
            var lookupTable = LookupTableFor(sym);
            if (lookupTable.ContainsKey(sym.Name))
            {
                return false;
            }
            lookupTable.Add(sym.Name, sym);
            return true;
        }

        public Symbol ResolveMethod(string name)
        {
            if (_methodTable.ContainsKey(name))
            {
                return _methodTable[name];
            }
            return EnclosingScope == null ? null : EnclosingScope.ResolveMethod(name);
        }

        public Symbol ResolveVariable(string name)
        {
            if (_variableTable.ContainsKey(name))
            {
                return _variableTable[name];
            }
            return EnclosingScope == null ? null : EnclosingScope.ResolveVariable(name);
        }

        public Symbol ResolveType(string name)
        {
            if (_typeTable.ContainsKey(name))
            {
                return _typeTable[name];
            }
            return EnclosingScope == null ? null : EnclosingScope.ResolveType(name);
        }
    }

    public class GlobalScope : ScopeBase, ITypeScope
    {
        public bool Define(SimpleTypeSymbol sym)
        {
            return base.Define(sym);
        }
    }

    public class LocalScope : ScopeBase, IVariableScope
    {
        public LocalScope(IScope enclosingScope) : base(enclosingScope) { }

        public bool Define(VariableSymbol sym)
        {
            return base.Define(sym);
        }
    }

}
