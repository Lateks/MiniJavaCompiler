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
        private TypeSymbol _superClass;

        public TypeSymbol SuperClass
        {
            get { return _superClass; }
            set
            {
                _superClass = value;
                Debug.Assert(Kind == _superClass.Kind);
                if (_superClass != null)
                {
                    ((ClassScope)Scope).SuperClassScope = (ClassScope)_superClass.Scope;
                    switch (Kind)
                    {
                        case TypeSymbolKind.Array:
                            ((ArrayType)Type).SuperType = (ArrayType)_superClass.Type;
                            break;
                        case TypeSymbolKind.Scalar:
                            ((ScalarType)Type).SuperType = (ScalarType)_superClass.Type;
                            break;
                    }
                }
            }
        }

        private TypeSymbol(string name, ITypeScope enclosingScope, IType type, TypeSymbolKind kind)
            : base(name, type, new ClassScope(enclosingScope))
        {
            SuperClass = null;
            Kind = kind;
            ((ClassScope)Scope).Symbol = this;
        }

        public static TypeSymbol MakeArrayTypeSymbol(ScalarType elementType, ITypeScope enclosingScope)
        {
            var arrayTypeName = String.Format("{0}[]", elementType.Name);
            var type = new ArrayType(elementType);
            return new TypeSymbol(arrayTypeName, enclosingScope, type, TypeSymbolKind.Array);
        }

        public static TypeSymbol MakeScalarTypeSymbol(string typeName, ITypeScope enclosingScope)
        {
            var type = new ScalarType(typeName);
            var symbol = new TypeSymbol(typeName, enclosingScope, type, TypeSymbolKind.Scalar);
            return symbol;
        }
    }
}
