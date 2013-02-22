using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Scopes;

namespace MiniJavaCompiler.Support.SymbolTable.Symbols
{
    public class VariableSymbol : Symbol
    {
        public VariableSymbol(string name, IType type, IScope enclosingScope)
            : base(name, type, enclosingScope) { }
    }

    public class MethodSymbol : Symbol
    {
        public bool IsStatic { get; private set; }

        public MethodSymbol(string name, IType returnType, IMethodScope enclosingScope, bool isStatic = false)
            : base(name, returnType, new MethodBodyScope(enclosingScope))
        {
            IsStatic = isStatic;
        }
    }
}
