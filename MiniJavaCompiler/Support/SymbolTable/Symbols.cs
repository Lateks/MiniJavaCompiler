using System;
using System.Collections.Generic;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public abstract class Symbol
    {
        public string Name
        {
            get;
            private set;
        }
        public IType Type
        {
            get;
            private set;
        }
        public IScope EnclosingScope
        {
            get;
            private set;
        }
        public SyntaxElement Definition
        {
            get;
            set;
        }

        // Returns the created symbol if defining succeeds. Otherwise returns null.
        public static Symbol CreateAndDefine<TSymbolType>(string name, IScope enclosingScope)
            where TSymbolType : Symbol, IType
        {
            return CreateAndDefine<TSymbolType>(enclosingScope, name, enclosingScope);
        }

        public static Symbol CreateAndDefine<TSymbolType>(string name, IType type, IScope enclosingScope)
            where TSymbolType : Symbol
        {
            if (typeof(TSymbolType) == typeof(UserDefinedTypeSymbol))
            {
                throw new NotSupportedException("This type of constructor not supported for the given type.");
            }
            return CreateAndDefine<TSymbolType>(enclosingScope, name, type, enclosingScope);
        }

        private static Symbol CreateAndDefine<TSymbolType>(IScope enclosingScope, params Object[] constructorParams)
            where TSymbolType : Symbol
        {
            var sym = (Symbol) Activator.CreateInstance(typeof (TSymbolType), constructorParams);
            return enclosingScope.Define(sym) ? sym : null;
        }

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

    public abstract class TypeSymbol : Symbol
    {
        protected TypeSymbol(string name, IType type, IScope enclosingScope) : base(name, type, enclosingScope) { }
    }

    public class BuiltinTypeSymbol : TypeSymbol, ISimpleType
    {
        public BuiltinTypeSymbol(string name, IScope enclosingScope)
            : base(name, BuiltinType.GetInstance(), enclosingScope) { }
    }

    public class MethodSymbol : Symbol, IScope
    {
        private Dictionary<string, Symbol> variableTable;

        public MethodSymbol(string name, IType returnType, UserDefinedTypeSymbol enclosingScope)
            : base(name, returnType ?? VoidType.GetInstance(), enclosingScope)
        {
            variableTable = new Dictionary<string, Symbol>();
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
                return variableTable[name];
            }
            catch (KeyNotFoundException)
            {
                return EnclosingScope.Resolve<TSymbolType>(name);
            }
        }

        public bool Define(Symbol sym)
        {
            if (!(sym is VariableSymbol))
            {
                throw new NotSupportedException("Only variable symbols can be defined in this scope.");
            }
            try
            {
                variableTable.Add(sym.Name, sym);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    public class UserDefinedTypeSymbol : TypeSymbol, IScope, ISimpleType
    {
        internal UserDefinedTypeSymbol SuperClass { get; set; }
        private readonly Dictionary<string, Symbol> methods;
        private readonly Dictionary<string, Symbol> fields;

        public UserDefinedTypeSymbol(string name, IScope enclosingScope)
            : base(name, MiniJavaClass.GetInstance(), enclosingScope)
        {
            methods = new Dictionary<string, Symbol>();
            fields = new Dictionary<string, Symbol>();
            SuperClass = null;
        }

        public Symbol Resolve<TSymbolType>(string name)
            where TSymbolType : Symbol
        {
            if (typeof(TSymbolType) == typeof(MethodSymbol))
                return ResolveMethodInSuperClasses(name);
            if (typeof(TSymbolType) != typeof(VariableSymbol))
                return EnclosingScope.Resolve<TSymbolType>(name);

            try
            {
                return fields[name];
            }
            catch (KeyNotFoundException)
            { // Because fields are private, they are not resolved from superclasses.
                return EnclosingScope.Resolve<TSymbolType>(name);
            }
        }

        private Symbol ResolveMethodInSuperClasses(string name)
        {
            try
            {
                return methods[name];
            }
            catch (KeyNotFoundException)
            {
                return SuperClass == null ? null : SuperClass.ResolveMethodInSuperClasses(name);
            }
        }

        public bool Define(Symbol sym)
        {
            if (sym is MethodSymbol)
            {
                return DefineSymbolIn(sym, methods);
            }
            else if (sym is VariableSymbol)
            {
                return DefineSymbolIn(sym, fields);
            }
            throw new NotSupportedException("Only variable and method symbols can be defined in this scope.");
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