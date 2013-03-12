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
        public readonly Dictionary<ISyntaxTreeNode, IScope> Scopes; // Maps AST nodes to their enclosing scopes (or the scopes they define in the case of methods and classes).
        public readonly Dictionary<Symbol, ISyntaxTreeNode> Declarations; // Maps method, variable and user defined type symbols to their definitions in the AST.

        public SymbolTable()
        {
            GlobalScope = new GlobalScope();
            Scopes = new Dictionary<ISyntaxTreeNode, IScope>();
            Declarations = new Dictionary<Symbol, ISyntaxTreeNode>();
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

        public TypeSymbol ResolveClass(ISyntaxTreeNode node)
        {
            if (!Scopes.ContainsKey(node))
            {
                throw new ArgumentException("Scope map not built or invalid node given.");
            }
            var scope = Scopes[node];
            while (!(scope is ClassScope) && scope != null)
            {
                scope = scope.EnclosingScope;
            }
            return scope == null ? null : ((ClassScope)scope).Symbol;
        }
    }
}
