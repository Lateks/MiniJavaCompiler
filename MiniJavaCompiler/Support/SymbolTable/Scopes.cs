using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public interface IScope
    {
        Symbol Resolve<TSymbolType>(string name) where TSymbolType : Symbol;
        IScope EnclosingScope { get; }
    }

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

        protected bool Define(Symbol sym)
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

    public class GlobalScope : ScopeBase, ITypeScope
    {
        public bool Define(SimpleTypeSymbol sym)
        {
            return base.Define((Symbol) sym);
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
