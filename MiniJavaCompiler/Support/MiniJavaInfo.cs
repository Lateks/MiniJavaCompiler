using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniJavaCompiler.Support
{
    // Collects static information on the syntax and semantics of Mini-Java.
    public static class MiniJavaInfo
    {
        public enum Operator
        {
            Add,
            Sub,
            Mul,
            Div,
            Eq,
            Gt,
            Lt,
            Mod,
            Not,
            And,
            Or
        }

        private static readonly Dictionary<string, Operator> OperatorSymbolToEnum
            = new Dictionary<string, Operator>()
            {
                { "+", Operator.Add },
                { "-", Operator.Sub },
                { "*", Operator.Mul },
                { "/", Operator.Div },
                { "%", Operator.Mod },
                { "==", Operator.Eq },
                { "||", Operator.Or },
                { "&&", Operator.And },
                { "<", Operator.Lt },
                { ">", Operator.Gt },
                { "!", Operator.Not }
            };

        // Constants for type names.
        public const string IntType = "int";
        public const string BoolType = "boolean";
        public const string AnyType = "$any";
        public const string VoidType = "void";

        public const string MainMethodIdent = "main";

        private static readonly char[]
            Punctuation = new[] { ';', '(', ')', '[', ']', '.', '{', '}', ',' },
            SingleCharOperatorSymbols = new[] { '/', '+', '-', '*', '<', '>', '%', '!' },
            MultiCharOperatorStartSymbols = new[] { '&', '=', '|' }; // Only one symbol needed because all two-character operators have the same symbol twice.

        // Note: all of these operators are left-associative.
        // A lower index in the array indicates a lower precedence.
        // These are stored as strings because they are used for peeking at input.
        // (Can be converted into Operators through the ConvertOperator method if needed.)
        private static readonly string[][] OperatorsByPrecedenceLevel = new[]
            {
                new [] { "||" },
                new [] { "&&" },
                new [] { "==" },
                new [] { "<", ">" },
                new [] { "+", "-" },
                new [] { "*", "/", "%" },
            };

        private static readonly string[]
            Keywords = new[] { "this", "true", "false", "new", "length", "System", "out",
                               "println", "if", "else", "while", "return", "assert",
                               "public", "static", "main", "class", "extends" },
            Types = new[] { IntType, BoolType, VoidType }, // Built-in types are also reserved words.
            ArrayMethods = new[] { "length" };

        private static readonly string[] BuiltIns = new [] { "int", "boolean" }; // Built-in types that can be used as variable and method return types.
        private static readonly string[] UnaryOperators = new [] { "!" };

        // Defines the operand and result types of operators for purposes of semantic analysis.
        private static readonly Dictionary<Operator, BuiltInOperator> Operators =
            new Dictionary<Operator, BuiltInOperator>()
                {
                    { Operator.Add, new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { Operator.Sub, new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { Operator.Mul, new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { Operator.Div, new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { Operator.Lt, new BuiltInOperator { OperandType = IntType, ResultType = BoolType } },
                    { Operator.Gt, new BuiltInOperator { OperandType = IntType, ResultType = BoolType } },
                    { Operator.Mod, new BuiltInOperator { OperandType = IntType, ResultType = IntType } },
                    { Operator.Not, new BuiltInOperator { OperandType = BoolType, ResultType = BoolType } },
                    { Operator.And, new BuiltInOperator { OperandType = BoolType, ResultType = BoolType } },
                    { Operator.Or, new BuiltInOperator { OperandType = BoolType, ResultType = BoolType } },
                    { Operator.Eq, new BuiltInOperator { OperandType = AnyType, ResultType = BoolType } } // '==' is defined for any type, including user defined types
                                                                                                          // and arrays (in which case it tests reference equality).
                };

        public static bool IsBuiltInType(string typeName)
        {
            return BuiltIns.Contains(typeName);
        }

        public static string[] BuiltInTypes()
        {
            return CopyArray(BuiltIns);
        }

        public static string[] UnaryOperatorSymbols()
        {
            return CopyArray(UnaryOperators);
        }

        public static string[] ArrayMethodNames()
        {
            return CopyArray(ArrayMethods);
        }

        public static bool IsUnaryOperator(string operatorSymbol)
        {
            return UnaryOperators.Contains(operatorSymbol);
        }

        public static bool IsKeyword(string word)
        {
            return Keywords.Contains(word);
        }

        public static bool IsTypeKeyword(string word)
        {
            return Types.Contains(word);
        }

        public static bool IsPunctuationCharacter(char character)
        {
            return Punctuation.Contains(character);
        }

        public static bool IsSingleCharOperatorSymbol(char character)
        {
            return SingleCharOperatorSymbols.Contains(character);
        }

        public static bool IsMultiCharOperatorSymbol(char character)
        {
            return MultiCharOperatorStartSymbols.Contains(character);
        }

        public static string[] GetOperatorsForPrecedenceLevel(int level)
        {
            if (level < 0 || level > MaxPrecedenceLevel())
            {
                throw new ArgumentOutOfRangeException("Unknown precedence level.");
            }
            return CopyArray(OperatorsByPrecedenceLevel[level]);
        }

        // Precedence levels start from 0 (which is the lowest precedence).
        public static int MaxPrecedenceLevel()
        {
            return OperatorsByPrecedenceLevel.Count() - 1;
        }

        public static Operator ConvertOperator(string op)
        {
            if (!OperatorSymbolToEnum.ContainsKey(op))
            {
                throw new ArgumentException("Invalid operator symbol.");
            }
            return OperatorSymbolToEnum[op];
        }

        public static string OperatorRepr(Operator op)
        {   // Inefficient, but this is only used in generating certain error messages...
            return OperatorSymbolToEnum.First((kvpair) => kvpair.Value == op).Key;
        }

        public static BuiltInOperator GetOperator(Operator op)
        {
            return Operators[op];
        }

        // A helper method to return copies of arrays to prevent editing their contents.
        private static string[] CopyArray(string[] array)
        {
            string[] returnArray = new string[array.Count()];
            array.CopyTo(returnArray, 0);
            return returnArray;
        }
    }

    public struct BuiltInOperator
    {
        public string OperandType;
        public string ResultType;
    }
}
