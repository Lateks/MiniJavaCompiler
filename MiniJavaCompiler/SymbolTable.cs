using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler
{
    namespace Support
    {
        public interface Scope
        {
            Symbol resolve(string name);
            Scope GetEnclosingScope();
            void define(Symbol sym);
        }

        public interface Type { }

        public class BaseScope : Scope
        {
            private Dictionary<string, Symbol> symbolTable;
            private BaseScope enclosingScope;

            public BaseScope()
            {
                new BaseScope(null);
            }

            public BaseScope(BaseScope enclosingScope)
            {
                symbolTable = new Dictionary<string, Symbol>();
                this.enclosingScope = enclosingScope;
            }

            public Scope GetEnclosingScope()
            {
                return enclosingScope;
            }

            public void define(Symbol sym)
            {
                symbolTable.Add(sym.Name, sym);
            }

            public Symbol resolve(string name)
            {
                try
                {
                    return symbolTable[name];
                }
                catch (KeyNotFoundException)
                {
                    if (enclosingScope == null)
                        return null;
                    else
                        return enclosingScope.resolve(name);
                }
            }
        }

        public class GlobalScope : BaseScope { }

        public class LocalScope : BaseScope { }

        public class Symbol
        {
            public string Name
            {
                get;
                private set;
            }

            public Symbol(string name)
            {
                Name = name;
            }
        }

        public class VariableSymbol : Symbol
        {
            public Type Type
            {
                get;
                private set;
            }

            public VariableSymbol(string name, Type type)
                : base(name)
            {
                Type = type;
            }
        }

        public class BuiltinTypeSymbol : Symbol, Type
        {
            public BuiltinTypeSymbol(string name) : base(name) { }
        }

        public class ScopedSymbol : Symbol, Scope, Type
        {
            protected Dictionary<string, Symbol> symbolTable;
            private Scope enclosingScope;

            public ScopedSymbol(string name, Scope enclosingScope)
                : base(name)
            {
                this.enclosingScope = enclosingScope;
            }

            public virtual Symbol resolve(string name)
            {
                try
                {
                    return symbolTable[name];
                }
                catch (KeyNotFoundException)
                {
                    return enclosingScope.resolve(name);
                }
            }

            public Scope GetEnclosingScope()
            {
                return enclosingScope;
            }

            public void define(Symbol sym)
            {
                symbolTable.Add(sym.Name, sym);
            }
        }

        public class MethodSymbol : ScopedSymbol
        {
            public Type Type
            {
                get;
                private set;
            }

            public MethodSymbol(string name, Type returnType, ClassSymbol enclosingScope)
                : base(name, enclosingScope)
            {
                Type = returnType;
            }
        }

        public class ClassSymbol : ScopedSymbol, Type
        {
            private ClassSymbol superClass;

            public ClassSymbol(string name, Scope enclosingScope, ClassSymbol superClass = null)
                : base(name, enclosingScope)
            {
                this.superClass = superClass;
            }

            public Scope getParentScope()
            {
                if (superClass != null)
                    return superClass;
                return GetEnclosingScope();
            }

            public override Symbol resolve(string name)
            {
                try
                {
                    return symbolTable[name];
                }
                catch (KeyNotFoundException)
                {
                    return getParentScope().resolve(name);
                }
            }
        }
    }
}
