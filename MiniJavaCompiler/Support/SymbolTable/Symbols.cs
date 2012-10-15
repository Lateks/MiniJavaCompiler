using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public abstract class Symbol
    {   // This class provides the base information for all symbol classes.
        public string Name { get; private set; }
        public IType Type { get; private set; }
        public IScope EnclosingScope { get; private set; }

        protected Symbol(string name, IType type, IScope enclosingScope)
        {
            Name = name;
            Type = type;
            EnclosingScope = enclosingScope;
        }
    }

    public class VariableSymbol : Symbol
    {
        public VariableSymbol(string name, IType type, IScope enclosingScope)
            : base(name, type, enclosingScope) { }
    }

    public abstract class SimpleTypeSymbol : Symbol, IType
    {
        protected SimpleTypeSymbol(string name, IType type, IScope enclosingScope) : base(name, type, enclosingScope) { }
        public abstract bool IsAssignableTo(IType other);
    }

    public class BuiltinTypeSymbol : SimpleTypeSymbol
    {
        public BuiltinTypeSymbol(string name, IScope enclosingScope)
            : base(name, null, enclosingScope) { }

        public override bool IsAssignableTo(IType other)
        {
            return Equals(other);
        }
    }

    public class MethodSymbol : Symbol, IVariableScope
    {
        private readonly Dictionary<string, Symbol> _variableTable;
        internal bool IsStatic { get; set; }

        public MethodSymbol(string name, IType returnType, IMethodScope enclosingScope, bool isStatic = false)
            : base(name, returnType, enclosingScope)
        {
            IsStatic = isStatic;
            _variableTable = new Dictionary<string, Symbol>();
        }

        public Symbol Resolve<TSymbolType>(string name)
            where TSymbolType : Symbol
        {
            if (typeof(TSymbolType) != typeof(VariableSymbol))
            {
                return EnclosingScope.Resolve<TSymbolType>(name);
            }
            try
            {
                return _variableTable[name];
            }
            catch (KeyNotFoundException)
            {
                return EnclosingScope.Resolve<TSymbolType>(name);
            }
        }

        public bool Define(VariableSymbol sym)
        {
            try
            {
                _variableTable.Add(sym.Name, sym);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    public class UserDefinedTypeSymbol : SimpleTypeSymbol, IMethodScope, IVariableScope
    {
        internal UserDefinedTypeSymbol SuperClass { get; set; }
        private readonly Dictionary<string, Symbol> _methods;
        private readonly Dictionary<string, Symbol> _fields;

        public UserDefinedTypeSymbol(string name, IScope enclosingScope)
            : base(name, null, enclosingScope)
        {
            _methods = new Dictionary<string, Symbol>();
            _fields = new Dictionary<string, Symbol>();
            SuperClass = null;
        }

        public override bool IsAssignableTo(IType other)
        {
            return IsDerivedFrom(other as UserDefinedTypeSymbol);
        }

        protected bool IsDerivedFrom(UserDefinedTypeSymbol other)
        {
            if (other == null)
            {
                return false;
            }
            if (Equals(other))
            {
                return true;
            }
            return SuperClass != null && SuperClass.IsDerivedFrom(other);
        }

        public Symbol Resolve<TSymbolType>(string name)
            where TSymbolType : Symbol
        {
            if (typeof(TSymbolType) == typeof(MethodSymbol))
                return ResolveMethodInSuperClasses(name);
            if (typeof(TSymbolType) == typeof(UserDefinedTypeSymbol))
                return EnclosingScope.Resolve<TSymbolType>(name);

            try
            {
                return _fields[name];
            }
            catch (KeyNotFoundException)
            { // Because fields are private, they are not resolved from superclasses.
              // In Mini-Java the enclosing scope of a class is the global scope which
              // cannot contain variable declarations, so resolving stops here.
                return null;
            }
        }

        private Symbol ResolveMethodInSuperClasses(string name)
        {
            try
            {
                return _methods[name];
            }
            catch (KeyNotFoundException)
            {
                return SuperClass == null ? null : SuperClass.ResolveMethodInSuperClasses(name);
            }
        }

        public bool Define(VariableSymbol sym)
        {
            return DefineSymbolIn(sym, _fields);
        }

        public bool Define(MethodSymbol sym)
        {
            return DefineSymbolIn(sym, _methods);
        }

        private bool DefineSymbolIn(Symbol sym, IDictionary<string, Symbol> lookupTable)
        {
            try
            {
                lookupTable.Add(sym.Name, sym);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}