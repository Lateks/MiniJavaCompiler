using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public interface IScope
    {
        MethodSymbol ResolveMethod(string name);
        VariableSymbol ResolveVariable(string name);
        SimpleTypeSymbol ResolveType(string name);
        IScope EnclosingScope { get; }
    }

    /* Note: All Define methods in different scope interfaces return a boolean
     * value indicating whether the attempt to define the symbol succeeded.
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

    public class GlobalScope : ScopeBase, ITypeScope
    {
        public new bool Define(SimpleTypeSymbol sym)
        {
            return base.Define(sym);
        }
    }

    public class LocalScope : ScopeBase, IVariableScope
    {
        public LocalScope(IScope enclosingScope) : base(enclosingScope) { }

        public new bool Define(VariableSymbol sym)
        {
            return base.Define(sym);
        }
    }

    public class MethodBodyScope : ScopeBase, IVariableScope
    {
        public MethodBodyScope(IMethodScope enclosingScope) : base(enclosingScope) { }

        public new bool Define(VariableSymbol sym)
        {
            return base.Define(sym);
        }
    }

    public class ClassScope : ScopeBase, IVariableScope, IMethodScope
    {
        public ClassScope SuperClassScope { get; set; }
        public UserDefinedTypeSymbol Symbol { get; set; }

        public ClassScope(ITypeScope enclosingScope) : base(enclosingScope) { }

        public new bool Define(VariableSymbol sym)
        {
            return base.Define(sym);
        }

        public new bool Define(MethodSymbol sym)
        {
            return base.Define(sym);
        }

        public override MethodSymbol ResolveMethod(string name)
        {
            return ResolveMethodWithinSuperClasses(name);
        }

        private MethodSymbol ResolveMethodWithinSuperClasses(string name)
        {
            if (_methodTable.ContainsKey(name))
            {
                return _methodTable[name];
            }
            else
            {
                return SuperClassScope == null ? null : SuperClassScope.ResolveMethodWithinSuperClasses(name);
            }
        }
    }
}
