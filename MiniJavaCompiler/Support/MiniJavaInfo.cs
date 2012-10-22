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
            MultiCharOperatorStartSymbols = new[] { '&', '=', '|' }; // Only one symbol needed because all two-character operators have the same symbol twice.

        // Note: all of these operators are left-associative.
        // A lower index in the array indicates a lower precedence.
        internal static readonly string[][] OperatorsByPrecedenceLevel = new[]
            {
                new [] { "||" },
                new [] { "&&" },
                new [] { "==" },
                new [] { "<", ">" },
                new [] { "+", "-" },
                new [] { "*", "/", "%" },
            };

        internal static readonly string[]
            Keywords = new[] { "this", "true", "false", "new", "length", "System", "out",
                               "println", "if", "else", "while", "return", "assert",
                               "public", "static", "main", "class", "extends" },
            Types = new[] { IntType, BoolType, VoidType }; // Built-in types are also reserved words.

        internal static readonly string[] BuiltIns = new [] { "int", "boolean" };
        internal static readonly string[] UnaryOperators = new [] { "!" };

        // Defines the operand and result types of operators for purposes of semantic analysis.
        internal static readonly Dictionary<string, BuiltInOperator> Operators =
            new Dictionary<string, BuiltInOperator>()
                {
                    { "+", new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { "-", new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { "*", new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { "/", new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { "<", new BuiltInOperator { OperandType = IntType, ResultType = BoolType } },
                    { ">", new BuiltInOperator { OperandType = IntType, ResultType = BoolType } },
                    { "%", new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { "!", new BuiltInOperator { OperandType = BoolType, ResultType = BoolType } },
                    { "&&", new BuiltInOperator { OperandType = BoolType, ResultType = BoolType } },
                    { "||", new BuiltInOperator { OperandType = BoolType, ResultType = BoolType } },
                    { "==", new BuiltInOperator { OperandType = AnyType, ResultType = BoolType } } // '==' is defined for any type, including user defined types
                                                                                                   // (in which case it tests reference equality).
                };

        internal static bool IsBuiltInType(string typeName)
        {
            return BuiltIns.Contains(typeName);
        }
    }

    internal struct BuiltInOperator
    {
        public string OperandType;
        public string ResultType;
    }
}
