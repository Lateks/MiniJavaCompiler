using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.SymbolTable
{
    // This class provides the base information for all symbol classes.
    public abstract class Symbol
    {
        public string Name { get; private set; }
        public IType Type { get; private set; }
        public IScope Scope { get; private set; }

        protected Symbol(string name, IType type, IScope scope)
        {
            Name = name;
            Type = type;
            Scope = scope;
        }
    }

    public class VariableSymbol : Symbol
    {
        public VariableSymbol(string name, IType type, IScope enclosingScope)
            : base(name, type, enclosingScope) { }
    }

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

    public class MethodSymbol : Symbol
    {
        public bool IsStatic { get; private set; }

        public MethodSymbol(string name, IType returnType, IMethodScope enclosingScope, bool isStatic = false)
            : base(name, returnType, new MethodBodyScope(enclosingScope))
        {
            IsStatic = isStatic;
        }
    }

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