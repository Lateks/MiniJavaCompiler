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
        public readonly GlobalScope GlobalScope;
        public readonly Dictionary<ISyntaxTreeNode, IScope> Scopes; // Maps AST nodes to their enclosing scopes (or the scopes they define in the case of methods and classes).
        public readonly Dictionary<Symbol, ISyntaxTreeNode> Definitions; // Maps method, variable and user defined type symbols to their definitions in the AST.

        public SymbolTable()
        {
            GlobalScope = new GlobalScope();
            Scopes = new Dictionary<ISyntaxTreeNode, IScope>();
            Definitions = new Dictionary<Symbol, ISyntaxTreeNode>();
        }

        public IType ResolveType(string typeName, bool array = false)
        {   // In Mini-Java types are always defined in the global scope.
            var scalarTypeSymbol = GlobalScope.ResolveType(typeName);
            if (scalarTypeSymbol == null)
            {
                return null;
            }
            var type = scalarTypeSymbol.Type;
            return array ? new ArrayType((ScalarType) type) : type;
        }

        // TODO: fix this so this method is no longer needed
        // (or the symbol property in the scope, probably)
        public TypeSymbol ResolveSurroundingClass(ISyntaxTreeNode node)
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
