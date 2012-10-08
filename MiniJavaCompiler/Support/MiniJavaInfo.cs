using System.Collections.Generic;
using System.Linq;

namespace MiniJavaCompiler.Support
{
    // Collects information on the syntax and semantics of Mini-Java.
    internal static class MiniJavaInfo
    {
        public static readonly string IntType = "int";
        public static readonly string BoolType = "boolean";
        public static readonly string AnyType = "$any";
        public static readonly string VoidType = "void";

        internal static readonly char[]
            Punctuation = new[] { ';', '(', ')', '[', ']', '.', '{', '}', ',' },
            SingleCharOperatorSymbols = new[] { '/', '+', '-', '*', '<', '>', '%', '!' },
            MultiCharOperatorSymbols = new[] { '&', '=', '|' };

        internal static readonly string[]
            Keywords = new[] { "this", "true", "false", "new", "length", "System", "out",
                               "println", "if", "else", "while", "return", "assert",
                               "public", "static", "main", "class", "extends" },
            Types = new[] { IntType, BoolType, VoidType };

        internal static readonly string[] BuiltIns = new [] { "int", "boolean" };
        internal static readonly string[] UnaryOperators = new [] { "!" };

        internal static Dictionary<string, BuiltinOperator> Operators =
            new Dictionary<string, BuiltinOperator>()
                {
                    { "+", new BuiltinOperator { OperandType = IntType, ResultType = IntType } },
                    { "-", new BuiltinOperator { OperandType = IntType, ResultType = IntType } },
                    { "*", new BuiltinOperator { OperandType = IntType, ResultType = IntType } },
                    { "/", new BuiltinOperator { OperandType = IntType, ResultType = IntType } },
                    { "<", new BuiltinOperator { OperandType = IntType, ResultType = BoolType } },
                    { ">", new BuiltinOperator { OperandType = IntType, ResultType = BoolType } },
                    { "%", new BuiltinOperator { OperandType = IntType, ResultType = IntType } },
                    { "!", new BuiltinOperator { OperandType = BoolType, ResultType = BoolType } },
                    { "&&", new BuiltinOperator { OperandType = BoolType, ResultType = BoolType } },
                    { "||", new BuiltinOperator { OperandType = BoolType, ResultType = BoolType } },
                    { "==", new BuiltinOperator { OperandType = AnyType, ResultType = BoolType } } // is defined for ints and booleans but not user defined types
                };

        internal static bool IsBuiltinType(string typeName)
        {
            return BuiltIns.Contains(typeName);
        }
    }

    internal struct BuiltinOperator
    {
        public string OperandType;
        public string ResultType;
    }
}
