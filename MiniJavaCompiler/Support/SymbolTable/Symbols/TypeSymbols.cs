using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Scopes;

namespace MiniJavaCompiler.Support.SymbolTable.Symbols
{
    public enum TypeSymbolKind
    {
        Scalar,
        Array
    }

    public class TypeSymbol : Symbol, IType
    {
        public TypeSymbolKind Kind { get; private set; }
        private TypeSymbol _superClass;

        public TypeSymbol SuperClass
        {
            get { return _superClass; }
            set
            {
                _superClass = value;
                if (_superClass != null)
                {
                    ((ClassScope)Scope).SuperClassScope = (ClassScope)_superClass.Scope;
                }
            }
        }

        public TypeSymbol(string name, ITypeScope enclosingScope, TypeSymbolKind kind)
            : base(name, null, new ClassScope(enclosingScope))
        {
            SuperClass = null;
            Kind = kind;
            ((ClassScope)Scope).Symbol = this;
        }

        public bool IsAssignableTo(IType other)
        {
            if (other == ErrorType.GetInstance() || this == other)
            {
                return true;
            }
            return IsDerivedFrom(other as TypeSymbol);
        }

        private bool IsDerivedFrom(TypeSymbol other)
        {
            if (other == null)
            {
                return false;
            }
            if (Equals(other))
            {
                return true;
            }
            return SuperClass != null && SuperClass.IsDerivedFrom(other);
        }
    }
}
