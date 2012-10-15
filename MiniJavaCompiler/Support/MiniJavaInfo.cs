using System.Collections.Generic;
using System.Linq;

namespace MiniJavaCompiler.Support
{
    // Collects static information on the syntax and semantics of Mini-Java.
    internal static class MiniJavaInfo
    {
        internal const string IntType = "int";
        internal const string BoolType = "boolean";
        internal const string AnyType = "$any";
        internal const string VoidType = "void";

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

        // Defines the operand and result types of operators for purposes of semantic analysis.
        internal static readonly Dictionary<string, BuiltinOperator> Operators =
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
                    { "==", new BuiltinOperator { OperandType = AnyType, ResultType = BoolType } } // '==' is defined for any type, including user defined types
                                                                                                   // (in which case it tests reference equality).
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
