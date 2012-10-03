using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public interface IScope
    {
        Symbol Resolve<TSymbolType>(string name) where TSymbolType : Symbol;
        bool Define(Symbol sym);
    }

    public abstract class ScopeBase : IScope
    {
        private readonly Dictionary<string, Symbol> _typeTable;
        private readonly Dictionary<string, Symbol> _methodTable;
        private readonly Dictionary<string, Symbol> _variableTable;
        protected IScope EnclosingScope
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

        protected Dictionary<string, Symbol> LookupTableFor<TSymbolType>()
            where TSymbolType : Symbol
        {
            if (typeof(TSymbolType) == typeof(MethodSymbol))
            {
                return _methodTable;
            }
            else if (typeof(TSymbolType) == typeof(VariableSymbol))
            {
                return _variableTable;
            }
            else
            {
                return _typeTable;
            }
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
            else
            {
                return _typeTable;
            }
        }

        public bool Define(Symbol sym)
        {
            try
            {
                LookupTableFor(sym).Add(sym.Name, sym);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public Symbol Resolve<TSymbolType>(string name)
            where TSymbolType : Symbol
        {
            try
            {
                return LookupTableFor<TSymbolType>()[name];
            }
            catch (KeyNotFoundException)
            {
                return EnclosingScope == null ? null : EnclosingScope.Resolve<TSymbolType>(name);
            }
        }
    }

    public class GlobalScope : ScopeBase { }

    public class LocalScope : ScopeBase
    {
        public LocalScope(IScope enclosingScope) : base(enclosingScope) { }
    }

}
