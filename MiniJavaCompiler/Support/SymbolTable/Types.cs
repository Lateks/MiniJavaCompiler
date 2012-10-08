namespace MiniJavaCompiler.Support.SymbolTable
{
    public interface IType
    {
        string Name { get; }
    }

    public interface IProgrammableType : IType
    {
        bool IsAssignableTo(IType other);
    }

    public interface ISimpleType : IProgrammableType { }

    public class BuiltinType : IType
    {
        private static readonly BuiltinType ClassInstance = new BuiltinType();
        public string Name { get; protected set; }

        private BuiltinType()
        {
            Name = "$builtin";
        }

        public static BuiltinType GetInstance()
        {
            return ClassInstance;
        }
    }

    public class MiniJavaArrayType : IProgrammableType
    {
        public ISimpleType ElementType { get; private set; }
        public string Name { get; protected set; }

        public MiniJavaArrayType(ISimpleType elementType)
        {
            Name = "array[" + elementType.Name + "]";
            ElementType = elementType;
        }

        public bool IsAssignableTo(IType other)
        {
            if (!(other is MiniJavaArrayType))
            {
                return false;
            }
            return ElementType.IsAssignableTo((other as MiniJavaArrayType).ElementType);
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

    public class MiniJavaClass : IType
    {
        private static readonly MiniJavaClass ClassInstance = new MiniJavaClass();
        public string Name { get; private set; }

        private MiniJavaClass()
        {
            Name = "class";
        }

        public static MiniJavaClass GetInstance()
        {
            return ClassInstance;
        }
    }

    public class VoidType : IProgrammableType
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
