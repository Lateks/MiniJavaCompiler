using System;
using System.Collections.Generic;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable.Scopes;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using MiniJavaCompiler.Support.SymbolTable.Types;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public class SymbolTable
    {
        public IEnumerable<string> ScalarTypeNames { get; set; }
        public readonly GlobalScope GlobalScope;

        public SymbolTable()
        {
            GlobalScope = new GlobalScope();
        }

        public TypeSymbol ResolveTypeName(string typeName, bool makeArray = false)
        {   // In Mini-Java types are always defined in the global scope.
            if (makeArray)
            {
                typeName += "[]";
            }
            var typeSymbol = GlobalScope.ResolveType(typeName);
            if (typeSymbol == null)
            {
                return null;
            }
            return typeSymbol;
        }
    }
}
