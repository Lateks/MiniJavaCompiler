using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler.Support
{
    public interface IScope
    {
        Symbol Resolve(string name);
        void Define(Symbol sym);
        IScope ParentScope { get; }
    }

    public interface IType
    {
        string Name { get; }
    }

    public class BuiltinType : IType
    {
        private static BuiltinType classInstance = new BuiltinType();
        public string Name { get; private set; }

        private BuiltinType()
        {
            Name = "$builtin";
        }

        public static BuiltinType GetInstance()
        {
            return classInstance;
        }
    }

    public class MiniJavaClass : IType
    {
        private static MiniJavaClass classInstance = new MiniJavaClass();
        public string Name { get; private set; }

        private MiniJavaClass()
        {
            Name = "class";
        }

        public static MiniJavaClass GetInstance()
        {
            return classInstance;
        }
    }

    public abstract class ScopeBase : IScope
    {
        private readonly Dictionary<string, Symbol> symbolTable;
        protected IScope EnclosingScope
        {
            get;
            private set;
        }
        public IScope ParentScope
        {
            get { return EnclosingScope; }
        }

        protected ScopeBase() : this(null) { }

        protected ScopeBase(IScope enclosingScope)
        {
            symbolTable = new Dictionary<string, Symbol>();
            EnclosingScope = enclosingScope;
        }

        public void Define(Symbol sym)
        {
            symbolTable.Add(sym.Name, sym);
        }

        public Symbol Resolve(string name)
        {
            try
            {
                return symbolTable[name];
            }
            catch (KeyNotFoundException)
            {
                return EnclosingScope == null ? null : EnclosingScope.Resolve(name);
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
            var sym = (Symbol)Activator.CreateInstance(typeof(TSymbolType), constructorParams);
            enclosingScope.Define(sym);
            return sym;
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

    public class BuiltinTypeSymbol : Symbol, IType
    {
        public BuiltinTypeSymbol(string name, IScope enclosingScope)
            : base(name, BuiltinType.GetInstance(), enclosingScope) { }
    }

    public abstract class ScopedSymbol : Symbol, IScope
    {
        protected Dictionary<string, Symbol> symbolTable;
        public IScope ParentScope
        {
            get { return EnclosingScope; }
        }

        protected ScopedSymbol(string name, IType type, IScope enclosingScope)
            : base(name, type, enclosingScope) { }

        public virtual Symbol Resolve(string name)
        {
            try
            {
                return symbolTable[name];
            }
            catch (KeyNotFoundException)
            {
                return EnclosingScope.Resolve(name);
            }
        }

        public void Define(Symbol sym)
        {
            symbolTable.Add(sym.Name, sym);
        }
    }

    public class MethodSymbol : ScopedSymbol
    {
        public MethodSymbol(string name, IType returnType, UserDefinedTypeSymbol enclosingScope)
            : base(name, returnType, enclosingScope) { }
    }

    public class UserDefinedTypeSymbol : ScopedSymbol, IType
    {
        internal UserDefinedTypeSymbol SuperClass { get; set; }
        public new IScope ParentScope
        {
            get { return SuperClass ?? base.ParentScope; }
        }

        public UserDefinedTypeSymbol(string name, IScope enclosingScope)
            : base(name, MiniJavaClass.GetInstance(), enclosingScope)
        {
            this.SuperClass = SuperClass;
        }

        public override Symbol Resolve(string name)
        {
            try
            {
                return symbolTable[name];
            }
            catch (KeyNotFoundException)
            {
                return ParentScope.Resolve(name);
            }
        }
    }
}