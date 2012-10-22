using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniJavaCompiler.Support
{
    // Collects static information on the syntax and semantics of Mini-Java.
    public static class MiniJavaInfo
    {
        // Constants for type names.
        public const string IntType = "int";
        public const string BoolType = "boolean";
        public const string AnyType = "$any";
        public const string VoidType = "void";

        private static readonly char[]
            Punctuation = new[] { ';', '(', ')', '[', ']', '.', '{', '}', ',' },
            SingleCharOperatorSymbols = new[] { '/', '+', '-', '*', '<', '>', '%', '!' },
            MultiCharOperatorStartSymbols = new[] { '&', '=', '|' }; // Only one symbol needed because all two-character operators have the same symbol twice.

        // Note: all of these operators are left-associative.
        // A lower index in the array indicates a lower precedence.
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
            Types = new[] { IntType, BoolType, VoidType }; // Built-in types are also reserved words.

        private static readonly string[] BuiltIns = new [] { "int", "boolean" }; // Built-in types that can be used as variable and method return types.
        private static readonly string[] UnaryOperators = new [] { "!" };

        // Defines the operand and result types of operators for purposes of semantic analysis.
        private static readonly Dictionary<string, BuiltInOperator> Operators =
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

        public static int MaxPrecedenceLevel()
        {
            return OperatorsByPrecedenceLevel.Count() - 1;
        }

        public static BuiltInOperator GetOperator(string operatorSymbol)
        {
            if (!Operators.ContainsKey(operatorSymbol))
            {
                throw new ArgumentException("Invalid operator symbol.");
            }
            return Operators[operatorSymbol];
        }

        // A helper method to return copies of arrays to prevent editing.
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
