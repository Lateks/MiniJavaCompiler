using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        {
            var simpleType = (ISimpleType) GlobalScope.Resolve<TypeSymbol>(typeName);
            if (simpleType == null)
            {
                return null;
            }
            return array ? new MiniJavaArrayType(simpleType) : (IType) simpleType;
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
