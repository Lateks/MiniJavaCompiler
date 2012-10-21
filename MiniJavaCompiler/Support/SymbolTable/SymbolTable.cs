using System.Collections.Generic;
using MiniJavaCompiler.AbstractSyntaxTree;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public class SymbolTable
    {
        public readonly GlobalScope GlobalScope;
        public readonly Dictionary<ISyntaxTreeNode, IScope> Scopes;
        public readonly Dictionary<Symbol, ISyntaxTreeNode> Definitions;

        public SymbolTable()
        {
            GlobalScope = new GlobalScope();
            Scopes = new Dictionary<ISyntaxTreeNode, IScope>();
            Definitions = new Dictionary<Symbol, ISyntaxTreeNode>();
        }

        public IType ResolveType(string typeName, bool array = false)
        { // In Mini-Java types are always defined in the global scope.
            var simpleType = (SimpleTypeSymbol) GlobalScope.ResolveType(typeName);
            if (simpleType == null)
            {
                return null;
            }
            return array ? MiniJavaArrayType.OfType(simpleType) : (IType) simpleType;
        }

        public UserDefinedTypeSymbol ResolveSurroundingClass(ISyntaxTreeNode node)
        {
            var scope = Scopes[node];
            while (!(scope is UserDefinedTypeSymbol) && scope != null)
            {
                scope = scope.EnclosingScope;
            }
            return (UserDefinedTypeSymbol) scope;
        }
    }
}
