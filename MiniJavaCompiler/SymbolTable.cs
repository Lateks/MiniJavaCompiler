using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler
{
    namespace Support
    {
        public interface IScope
        {
            Symbol Resolve(string name);
            void Define(Symbol sym);
        }

        public interface IType { }

        public abstract class BaseScope : IScope
        {
            private readonly Dictionary<string, Symbol> symbolTable;
            public BaseScope EnclosingScope
            {
                get;
                private set;
            }

            // TODO: Replace null with a valid BaseScope (constant?)
            protected BaseScope() : this(null) { }

            protected BaseScope(BaseScope enclosingScope)
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

        public class GlobalScope : BaseScope { }

        public class LocalScope : BaseScope { }

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

            protected Symbol(string name, IScope enclosingScope)
            {
                Name = name;
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
                : base(name, enclosingScope) { }
        }

        public abstract class ScopedSymbol : Symbol, IScope
        {
            protected Dictionary<string, Symbol> symbolTable;

            protected ScopedSymbol(string name, IScope enclosingScope)
                : base(name, enclosingScope) { }

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

            public virtual IScope GetParentScope()
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
            public MethodSymbol(string name, IType returnType, ClassSymbol enclosingScope)
                : base(name, returnType, enclosingScope) { }
        }

        public class ClassSymbol : ScopedSymbol, IType
        {
            private readonly ClassSymbol superClass;

            public ClassSymbol(string name, IScope enclosingScope, ClassSymbol superClass = null)
                : base(name, enclosingScope)
            {
                this.superClass = superClass;
            }

            public override IScope GetParentScope()
            {
                return superClass ?? EnclosingScope;
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
