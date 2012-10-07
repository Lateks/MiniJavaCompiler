namespace MiniJavaCompiler.Support.SymbolTable
{
    public interface IType
    {
        string Name { get; }
    }

    public interface ISimpleType : IType { }

    public class Type : IType
    {
        public string Name { get; protected set; }
    }

    public class BuiltinType : Type
    {
        private static readonly BuiltinType ClassInstance = new BuiltinType();

        private BuiltinType()
        {
            Name = "$builtin";
        }

        public static BuiltinType GetInstance()
        {
            return ClassInstance;
        }
    }

    public class MiniJavaArrayType : Type
    {
        public ISimpleType ElementType { get; private set; }

        public MiniJavaArrayType(ISimpleType elementType)
        {
            Name = "array[" + elementType.Name + "]";
            ElementType = elementType;
        }

        public static bool IsPredefinedArrayMethod(string name)
        {
            return name == "length";
        }

        public bool Equals(IType other)
        {
            if (other == null)
            {
                return false;
            }
            return other is MiniJavaArrayType && Equals(other as MiniJavaArrayType);
        }

        public bool Equals(MiniJavaArrayType other)
        {
            return ElementType == other.ElementType;
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

    public class VoidType : IType
    {
        private static readonly VoidType ClassInstance = new VoidType();
        public string Name { get; private set; }

        private VoidType()
        {
            Name = MiniJavaInfo.VoidType;
        }

        public static VoidType GetInstance()
        {
            return ClassInstance;
        }
    }
}
