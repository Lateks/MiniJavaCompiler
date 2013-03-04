using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Scopes;
using System.Diagnostics;

namespace MiniJavaCompiler.Support.SymbolTable.Symbols
{
    public enum TypeSymbolKind
    {
        Scalar,
        Array
    }

    public class TypeSymbol : Symbol
    {
        public TypeSymbolKind Kind { get; private set; }
        public System.Reflection.Emit.TypeBuilder Builder { get; set; }
        private TypeSymbol _superClass;

        public static TypeSymbol MakeArrayTypeSymbol(ScalarType elementType, ITypeScope enclosingScope)
        {
            var type = new ArrayType(elementType);
            return new TypeSymbol(type.Name, enclosingScope, type, TypeSymbolKind.Array);
        }

        public static TypeSymbol MakeScalarTypeSymbol(string typeName, ITypeScope enclosingScope)
        {
            var type = new ScalarType(typeName);
            return new TypeSymbol(typeName, enclosingScope, type, TypeSymbolKind.Scalar);
        }

        private TypeSymbol(string name, ITypeScope enclosingScope, IType type, TypeSymbolKind kind)
            : base(name, type, new ClassScope(enclosingScope))
        {
            _superClass = null;
            Kind = kind;
            ((ClassScope)Scope).Symbol = this;
        }

        public TypeSymbol SuperClass
        {
            get { return _superClass; }
            set
            {
                _superClass = value;
                Debug.Assert(_superClass == null || Kind == _superClass.Kind);
                SetSuperClassScope();
                SetSuperType();
            }
        }

        private void SetSuperClassScope()
        {
            ((ClassScope)Scope).SuperClassScope = _superClass == null ? null : (ClassScope)_superClass.Scope;

        }

        private void SetSuperType()
        {
            if (Kind == TypeSymbolKind.Scalar)
            {
                ((ScalarType)Type).SuperType = _superClass == null ? null : (ScalarType)_superClass.Type;
            }
        }
    }
}
