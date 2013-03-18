using System;
using System.Collections.Generic;
using System.Diagnostics;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.Support.SymbolTable.Scopes
{
    public interface IScope
    {
        MethodSymbol ResolveMethod(string name);
        VariableSymbol ResolveVariable(string name);
        TypeSymbol ResolveType(string name);
        TypeSymbol ResolveArrayType(string name);
        IScope EnclosingScope { get; }
    }

    /* Note: All Define methods in different scope interfaces return a boolean
     * value indicating whether the attempt to define the symbol succeeded.
     * The same kind of symbol with the same name cannot be defined twice in the
     * same scope.
     */
    public interface IVariableScope : IScope
    {
        bool Define(VariableSymbol sym);
        VariableSymbol ResolveLocalVariable(string name);
        bool IsLocalScope { get; }
    }

    public interface IMethodScope : IScope
    {
        bool Define(MethodSymbol sym);
    }

    public interface ITypeScope : IScope
    {
        bool Define(TypeSymbol sym);
    }
}
