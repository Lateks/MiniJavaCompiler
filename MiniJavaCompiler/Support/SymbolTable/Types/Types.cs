using System;
using System.Collections.Generic;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.Support.SymbolTable.Types
{
    public interface IType
    {
        string Name { get; }
        bool IsAssignableTo(IType other);
    }

    // This is a placeholder type for type stacks when a type cannot be
    // resolved or is faulty. ErrorTypes _must_ be compatible with
    // (that is: assignable to and from) every other type to avoid a
    // cascade of uninformative error messages during type checking.
    public class ErrorType : IType
    {
        private static readonly ErrorType Instance = new ErrorType();
        public string Name { get { return "RESOLVE_ERROR"; } }

        private ErrorType() { }

        public bool IsAssignableTo(IType other)
        {
            return true;
        }

        public static ErrorType GetInstance()
        {
            return Instance;
        }
    }

    public class ScalarType : IType
    {
        public string Name { get; private set; }
        public ScalarType SuperType { get; set; }

        public ScalarType(string name)
        {
            Name = name;
        }

        public bool IsAssignableTo(IType other)
        {
            if (other == ErrorType.GetInstance())
            {
                return true;
            }
            if (other is ArrayType || other is VoidType)
            {
                return false;
            }
            return IsDerivedFrom(other as ScalarType);
        }

        private bool IsDerivedFrom(ScalarType other)
        {
            if (other == null)
            {
                return false;
            }
            if (Equals(other))
            {
                return true;
            }
            return SuperType != null && SuperType.IsDerivedFrom(other);
        }
    }

    public class ArrayType : IType
    {
        public ScalarType ElementType { get; private set; }
        public string Name { get; private set; }

        public ArrayType(ScalarType elementType)
        {
            Name = String.Format("{0}[]", elementType.Name);
            ElementType = elementType;
        }

        public bool IsAssignableTo(IType other)
        {
            if (other == ErrorType.GetInstance())
            {
                return true;
            }
            return this.Equals(other);
        }

        // TODO: use scopes for this (like with other methods).
        public static bool IsPredefinedArrayMethod(string name)
        {
            return name == "length";
        }
    }

    public class VoidType : IType
    {
        private static readonly VoidType ClassInstance = new VoidType();
        public string Name { get; private set; }

        private VoidType()
        {
            Name = MiniJavaInfo.VoidType;
        }

        public bool IsAssignableTo(IType other)
        {
            return other == ErrorType.GetInstance();
        }

        public static VoidType GetInstance()
        {
            return ClassInstance;
        }
    }
}
