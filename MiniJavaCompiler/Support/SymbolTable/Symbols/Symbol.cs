using System;
using System.Collections.Generic;
using MiniJavaCompiler.Support.SymbolTable.Scopes;
using MiniJavaCompiler.Support.SymbolTable.Types;

namespace MiniJavaCompiler.Support.SymbolTable.Symbols
{
    // This class provides the base information for all symbol classes.
    public abstract class Symbol
    {
        public string Name { get; private set; }
        public IType Type { get; private set; }
        public IScope Scope { get; private set; }

        protected Symbol(string name, IType type, IScope scope)
        {
            Name = name;
            Type = type;
            Scope = scope;
        }
    }
}