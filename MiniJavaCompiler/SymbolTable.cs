using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler
{
    namespace Support
    {
        public interface Scope
        {
            Symbol Resolve(string name);
            void Define(Symbol sym);
        }

        public interface Type { }

        public class BaseScope : Scope
        {
            private Dictionary<string, Symbol> symbolTable;
            public BaseScope EnclosingScope
            {
                get;
                private set;
            }

            public BaseScope()
            {
                new BaseScope(null);
            }

            public BaseScope(BaseScope enclosingScope)
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
                    if (EnclosingScope == null)
                        return null;
                    else
                        return EnclosingScope.Resolve(name);
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
            public Type Type
            {
                get;
                private set;
            }
            public Scope EnclosingScope
            {
                get;
                private set;
            }
            public SyntaxTreeNode Definition
            {
                get;
                set;
            }

            public Symbol(string name, Scope enclosingScope)
            {
                Name = name;
            }

            public Symbol(string name, Type type, Scope enclosingScope)
            {
                Name = name;
                Type = type;
                EnclosingScope = enclosingScope;
            }
        }

        public class VariableSymbol : Symbol
        {

            public VariableSymbol(string name, Type type, Scope enclosingScope)
                : base(name, type, enclosingScope) { }
        }

        public class BuiltinTypeSymbol : Symbol, Type
        {
            public BuiltinTypeSymbol(string name, Scope enclosingScope)
                : base(name, enclosingScope) { }
        }

        public class ScopedSymbol : Symbol, Scope, Type
        {
            protected Dictionary<string, Symbol> symbolTable;

            public ScopedSymbol(string name, Scope enclosingScope)
                : base(name, enclosingScope) { }

            public ScopedSymbol(string name, Type type, Scope enclosingScope)
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

            public virtual Scope GetParentScope()
            {
                return EnclosingScope;
            }

            public void Define(Symbol sym)
            {
                symbolTable.Add(sym.Name, sym);
            }
        }

        public class MethodSymbol : ScopedSymbol
        {
            public MethodSymbol(string name, Type returnType, ClassSymbol enclosingScope)
                : base(name, returnType, enclosingScope) { }
        }

        public class ClassSymbol : ScopedSymbol, Type
        {
            private ClassSymbol superClass;

            public ClassSymbol(string name, Scope enclosingScope, ClassSymbol superClass = null)
                : base(name, enclosingScope)
            {
                this.superClass = superClass;
            }

            public override Scope GetParentScope()
            {
                if (superClass == null)
                    return EnclosingScope;
                return superClass;
            }

            public override Symbol Resolve(string name)
            {
                try
                {
                    return symbolTable[name];
                }
                catch (KeyNotFoundException)
                {
                    return GetParentScope().Resolve(name);
                }
            }
        }
    }
}
