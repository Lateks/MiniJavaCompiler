using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler.Support
{
    public interface IScope
    {
        Symbol Resolve<TSymbolType>(string name) where TSymbolType : Symbol;
        bool Define(Symbol sym);
    }

    public interface IType
    {
        string Name { get; }
    }

    public interface ISimpleType : IType { }

    public class BuiltinType : IType
    {
        private static readonly BuiltinType ClassInstance = new BuiltinType();
        public string Name { get; private set; }

        private BuiltinType()
        {
            Name = "$builtin";
        }

        public static BuiltinType GetInstance()
        {
            return ClassInstance;
        }
    }

    public class MiniJavaArrayType : IType
    {
        public string Name { get; private set; }
        public ISimpleType ElementType { get; private set; }

        public MiniJavaArrayType(ISimpleType elementType)
        {
            Name = "$builtin_array_" + elementType.Name;
            ElementType = elementType;
        }
    }

    public class MiniJavaClass : IType
    {
        private static readonly MiniJavaClass ClassInstance = new MiniJavaClass();
        public string Name { get; private set; }

        private MiniJavaClass()
        {
            Name = "class";
        }

        public static MiniJavaClass GetInstance()
        {
            return ClassInstance;
        }
    }

    public abstract class ScopeBase : IScope
    {
        private readonly Dictionary<string, Symbol> typeTable;
        private readonly Dictionary<string, Symbol> methodTable;
        private readonly Dictionary<string, Symbol> variableTable;
        protected IScope EnclosingScope
        {
            get;
            private set;
        }

        protected ScopeBase() : this(null) { }

        protected ScopeBase(IScope enclosingScope)
        {
            typeTable = new Dictionary<string, Symbol>();
            methodTable = new Dictionary<string, Symbol>();
            variableTable = new Dictionary<string, Symbol>();
            EnclosingScope = enclosingScope;
        }

        protected Dictionary<string, Symbol> LookupTableFor<TSymbolType>()
            where TSymbolType : Symbol
        {
            if (typeof(TSymbolType) == typeof(MethodSymbol))
            {
                return methodTable;
            }
            else if (typeof(TSymbolType) == typeof(VariableSymbol))
            {
                return variableTable;
            }
            else
            {
                return typeTable;
            }
        }

        protected Dictionary<string, Symbol> LookupTableFor(Symbol sym)
        {
            if (sym is MethodSymbol)
            {
                return methodTable;
            }
            else if (sym is VariableSymbol)
            {
                return variableTable;
            }
            else
            {
                return typeTable;
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

    public class LocalScope : ScopeBase { }

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
            if (enclosingScope.Define(sym))
            {
                return sym;
            }
            return null;
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
            : base(name, returnType, enclosingScope)
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
        private readonly Dictionary<string, Symbol> methodTable;
        private readonly Dictionary<string, Symbol> variableTable;

        public UserDefinedTypeSymbol(string name, IScope enclosingScope)
            : base(name, MiniJavaClass.GetInstance(), enclosingScope)
        {
            methodTable = new Dictionary<string, Symbol>();
            variableTable = new Dictionary<string, Symbol>();
            this.SuperClass = SuperClass;
        }

        private Dictionary<string, Symbol> LookupTableFor<TSymbolType>()
            where TSymbolType : Symbol
        {
            if (typeof(TSymbolType) == typeof(MethodSymbol))
            {
                return methodTable;
            }
            else if (typeof(TSymbolType) == typeof(VariableSymbol))
            {
                return variableTable;
            }
            return null; // No other symbols in this scope.
        }

        public Symbol Resolve<TSymbolType>(string name)
            where TSymbolType : Symbol
        {
            var lookupTable = LookupTableFor<TSymbolType>();

            Symbol resolvedSymbol = null;
            if (lookupTable != null && lookupTable.TryGetValue(name, out resolvedSymbol))
            {
                return resolvedSymbol;
            }
            else
            {
                if (SuperClass != null)
                {
                    resolvedSymbol = SuperClass.ResolveInSuperClasses<TSymbolType>(name);
                }
                return resolvedSymbol ?? EnclosingScope.Resolve<TSymbolType>(name);
            }
        }

        private Symbol ResolveInSuperClasses<TSymbolType>(string name)
            where TSymbolType : Symbol
        {
            var lookupTable = LookupTableFor<TSymbolType>();
            if (lookupTable == null)
            {
                return null;
            }

            try
            {
                return lookupTable[name];
            }
            catch (KeyNotFoundException)
            {
                return SuperClass == null ? null : SuperClass.ResolveInSuperClasses<TSymbolType>(name);
            }
        }

        public bool Define(Symbol sym)
        {
            if (sym is MethodSymbol)
            {
                return DefineSymbolIn(sym, methodTable);
            }
            else if (sym is VariableSymbol)
            {
                return DefineSymbolIn(sym, methodTable);
            }
            return false;
        }

        private bool DefineSymbolIn(Symbol sym, Dictionary<string, Symbol> lookupTable)
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