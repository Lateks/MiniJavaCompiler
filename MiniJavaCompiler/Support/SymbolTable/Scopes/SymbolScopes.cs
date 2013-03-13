using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.Support.SymbolTable.Scopes
{
    public class MethodBodyScope : VariableScopeBase
    {
        public MethodBodyScope(IMethodScope enclosingScope)
            : base(enclosingScope, true) { }
    }

    public class ClassScope : VariableScopeBase, IMethodScope
    {
        public ClassScope SuperClassScope { get; set; }
        public TypeSymbol Symbol { get; set; }

        public ClassScope(ITypeScope enclosingScope)
            : base(enclosingScope) { }

        public new bool Define(MethodSymbol sym)
        {
            return base.Define(sym);
        }

        public override MethodSymbol ResolveMethod(string name)
        {
            return ResolveMethodWithinSuperClasses(name);
        }

        private MethodSymbol ResolveMethodWithinSuperClasses(string name)
        {
            if (_methodTable.ContainsKey(name))
            {
                return _methodTable[name];
            }
            else
            {
                return SuperClassScope == null ? null : SuperClassScope.ResolveMethodWithinSuperClasses(name);
            }
        }
    }
}
