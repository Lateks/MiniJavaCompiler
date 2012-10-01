namespace MiniJavaCompiler.Support.SymbolTable
{
    public interface IType
    {
        string Name { get; }
    }

    public interface ISimpleType : IType { }

    public class BuiltinType : IType
    {
        private static readonly BuiltinType ClassInstance = new BuiltinType();
        public string Name { get; private set; }

        private BuiltinType()
        {
            Name = "$builtin";
        }

        public static BuiltinType GetInstance()
        {
            return ClassInstance;
        }
    }

    public class MiniJavaArrayType : IType
    {
        public string Name { get; private set; }
        public ISimpleType ElementType { get; private set; }

        public MiniJavaArrayType(ISimpleType elementType)
        {
            Name = "$builtin_array_" + elementType.Name;
            ElementType = elementType;
        }

        public static bool IsPredefinedArrayAction(string name)
        {
            return name == "length";
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
}
