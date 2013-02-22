using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Scopes;

namespace MiniJavaCompiler.Support.SymbolTable.Symbols
{
    // Represents "simple" types (not collections, ie. arrays).
    public abstract class SimpleTypeSymbol : Symbol, IType
    {
        protected SimpleTypeSymbol(string name, IType type, IScope enclosingScope) : base(name, type, enclosingScope) { }
        public abstract bool IsAssignableTo(IType other);
    }

    public class BuiltInTypeSymbol : SimpleTypeSymbol
    {
        public BuiltInTypeSymbol(string name, IScope enclosingScope)
            : base(name, null, enclosingScope) { }

        public override bool IsAssignableTo(IType other)
        {
            if (other == ErrorType.GetInstance())
            {
                return true;
            }
            return Equals(other);
        }
    }

    // Used for user defined types (classes).
    public class UserDefinedTypeSymbol : SimpleTypeSymbol
    {
        private UserDefinedTypeSymbol _superClass;

        public UserDefinedTypeSymbol SuperClass
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

        public UserDefinedTypeSymbol(string name, ITypeScope enclosingScope)
            : base(name, null, new ClassScope(enclosingScope))
        {
            SuperClass = null;
            ((ClassScope)Scope).Symbol = this;
        }

        public override bool IsAssignableTo(IType other)
        {
            if (other == ErrorType.GetInstance())
            {
                return true;
            }
            if (other is BuiltInTypeSymbol)
            {
                return false;
            }
            return IsDerivedFrom(other as UserDefinedTypeSymbol);
        }

        private bool IsDerivedFrom(UserDefinedTypeSymbol other)
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
