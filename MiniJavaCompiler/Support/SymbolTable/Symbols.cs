using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.SymbolTable
{
    // This class provides the base information for all symbol classes.
    public abstract class Symbol
    {
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

    public class BuiltInTypeSymbol : SimpleTypeSymbol
    {
        public BuiltInTypeSymbol(string name, IScope enclosingScope)
            : base(name, null, enclosingScope) { }

        public override bool IsAssignableTo(IType other)
        {
            if (other == ErrorType.GetInstance())
            {
                return true;
            }
            return Equals(other);
        }
    }

    public class MethodSymbol : Symbol, IVariableScope
    {
        private readonly Dictionary<string, Symbol> _variableTable;
        public bool IsStatic { get; private set; }

        public MethodSymbol(string name, IType returnType, IMethodScope enclosingScope, bool isStatic = false)
            : base(name, returnType, enclosingScope)
        {
            IsStatic = isStatic;
            _variableTable = new Dictionary<string, Symbol>();
        }

        public bool Define(VariableSymbol sym)
        {
            if (_variableTable.ContainsKey(sym.Name))
            {
                return false;
            }
            _variableTable.Add(sym.Name, sym);
            return true;
        }

        public Symbol ResolveMethod(string name)
        {
            return EnclosingScope.ResolveMethod(name);
        }

        public Symbol ResolveVariable(string name)
        {
            if (_variableTable.ContainsKey(name))
            {
                return _variableTable[name];
            }
            else
            {
                return EnclosingScope.ResolveVariable(name);
            }
        }

        public Symbol ResolveType(string name)
        {
            return EnclosingScope.ResolveType(name);
        }
    }

    public class UserDefinedTypeSymbol : SimpleTypeSymbol, IMethodScope, IVariableScope
    {
        public UserDefinedTypeSymbol SuperClass { get; set; }
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
            if (other == ErrorType.GetInstance())
            {
                return true;
            }
            if (other is BuiltInTypeSymbol)
            {
                return false;
            }
            return IsDerivedFrom(other as UserDefinedTypeSymbol);
        }

        private bool IsDerivedFrom(UserDefinedTypeSymbol other)
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

        public bool Define(VariableSymbol sym)
        {
            return DefineSymbolIn(sym, _fields);
        }

        public bool Define(MethodSymbol sym)
        {
            return DefineSymbolIn(sym, _methods);
        }

        public Symbol ResolveMethod(string name)
        {
            return ResolveMethodInSuperClasses(name);
        }

        public Symbol ResolveVariable(string name)
        {
            if (_fields.ContainsKey(name))
            {
                return _fields[name];
            }
            else
            {   // Because fields are private, they are not resolved from superclasses.
                // In Mini-Java the enclosing scope of a class is the global scope which
                // cannot contain variable declarations, so resolving stops here.
                return null;
            }
        }

        public Symbol ResolveType(string name)
        {
            return EnclosingScope.ResolveType(name);
        }

        private Symbol ResolveMethodInSuperClasses(string name)
        {
            if (_methods.ContainsKey(name))
            {
                return _methods[name];
            }
            else
            {
                return SuperClass == null ? null : SuperClass.ResolveMethodInSuperClasses(name);
            }
        }

        private bool DefineSymbolIn(Symbol sym, IDictionary<string, Symbol> lookupTable)
        {
            if (lookupTable.ContainsKey(sym.Name))
            {
                return false;
            }
            lookupTable.Add(sym.Name, sym);
            return true;
        }
    }
}