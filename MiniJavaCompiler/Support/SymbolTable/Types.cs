using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.SymbolTable
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
        public string Name { get { return "error"; } }

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

    public class MiniJavaArrayType : IType
    {
        public SimpleTypeSymbol ElementType { get; private set; }
        public string Name { get; protected set; }
        private static readonly Dictionary<SimpleTypeSymbol, MiniJavaArrayType> ArrayTypes =
            new Dictionary<SimpleTypeSymbol, MiniJavaArrayType>();

        private MiniJavaArrayType(SimpleTypeSymbol elementType)
        {
            Name = String.Format("{0}[]", elementType.Name);
            ElementType = elementType;
        }

        public static MiniJavaArrayType OfType(SimpleTypeSymbol elementType)
        {
            if (ArrayTypes.ContainsKey(elementType))
            {
                return ArrayTypes[elementType];
            }
            var arrayType = new MiniJavaArrayType(elementType);
            ArrayTypes[elementType] = arrayType;
            return arrayType;
        }

        public bool IsAssignableTo(IType other)
        {
            if (other == ErrorType.GetInstance())
            {
                return true;
            }
            return Equals(other);
        }

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
