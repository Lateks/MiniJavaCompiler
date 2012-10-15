using System;

namespace MiniJavaCompiler.Support.SymbolTable
{
    public interface IType
    {
        string Name { get; }
        bool IsAssignableTo(IType other);
    }

    public class MiniJavaArrayType : IType
    {
        public SimpleTypeSymbol ElementType { get; private set; }
        public string Name { get; protected set; }

        public MiniJavaArrayType(SimpleTypeSymbol elementType)
        {
            Name = String.Format("{0}[]", elementType.Name);
            ElementType = elementType;
        }

        public bool IsAssignableTo(IType other)
        {
            if (!(other is MiniJavaArrayType))
            {
                return false;
            }
            // Element types must be the same (not just derived from the same base).
            return ElementType.Equals((other as MiniJavaArrayType).ElementType);
        }

        public static bool IsPredefinedArrayMethod(string name)
        {
            return name == "length";
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }
            return other is MiniJavaArrayType && Equals(other as MiniJavaArrayType);
        }

        public bool Equals(MiniJavaArrayType other)
        {
            return ElementType.Equals(other.ElementType);
        }

        public override int GetHashCode()
        {
            return (ElementType != null ? ElementType.GetHashCode() : 0);
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
            return false;
        }

        public static VoidType GetInstance()
        {
            return ClassInstance;
        }
    }
}
