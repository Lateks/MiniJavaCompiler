﻿using System.IO;
using MiniJavaCompiler.FrontEnd.LexicalAnalysis;
using MiniJavaCompiler.FrontEnd.SemanticAnalysis;
using MiniJavaCompiler.FrontEnd.SyntaxAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using NUnit.Framework;

namespace MiniJavaCompilerTest.FrontEndTest.SemanticAnalysis
{
    public partial class TypeCheckerTest
    {
        [TestFixture]
        public class LogicalBinaryOperatorTest
        {
            [Datapoints]
            public string[] LogicalOperators = new[] { "&&", "||" };

            [Theory]
            public void InvalidLeftOperandForALogicalOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = new A() " + op + " false;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").
                    And.StringContaining("A"));
            }

            [Theory]
            public void InvalidRightOperandForALogicalOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = true " + op + " 1;\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").
                    And.StringContaining("int"));
            }

            [Theory]
            public void InvalidBothOperandsForALogicalOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = new A() " + op + " 1;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").
                    And.StringContaining("int").And.StringContaining("A"));
            }

            [Theory]
            public void InvalidLeftOperandAndErrorTypeAsOperandsTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = new A() " + op + " bar;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.False(checker.RunCheck());
                Assert.AreEqual(2, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find symbol bar"));
                Assert.That(errors.Errors[1].ToString(), Is.StringContaining("Invalid operand of type A"));
            }

            [Theory]
            public void InvalidRightOperandAndErrorTypeAsOperandsTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = bar " + op + " new A();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.False(checker.RunCheck());
                Assert.AreEqual(2, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find symbol bar"));
                Assert.That(errors.Errors[1].ToString(), Is.StringContaining("Invalid operand of type A"));
            }

            [Theory]
            public void ValidOperandsForALogicalOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = true " + op + " foo;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.True(checker.RunCheck());
            }
        }
    }
}
