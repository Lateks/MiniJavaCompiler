using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.SyntaxAnalysis;
using NUnit.Framework;
using MiniJavaCompiler.SemanticAnalysis;

namespace MiniJavaCompilerTest.Frontend
{
    internal class TypeCheckerTest
    {
        internal static TypeChecker SetUpTypeAndReferenceChecker(string program)
        {
            var scanner = new MiniJavaScanner(new StringReader(program));
            var errors = new ErrorLogger();
            var parserInputReader = new ParserInputReader(scanner, errors);
            var parser = new Parser(parserInputReader, errors, true);
            Program syntaxTree = parser.Parse();
            Assert.That(errors.Errors(), Is.Empty);

            var types = new TypeSetBuilder(syntaxTree, errors).BuildTypeSet();
            var symbolTableBuilder = new SymbolTableBuilder(syntaxTree, types, errors);
            Assert.That(errors.Errors(), Is.Empty);

            SymbolTable symbolTable;
            Assert.True(symbolTableBuilder.BuildSymbolTable(out symbolTable));

            return new TypeChecker(syntaxTree, symbolTable);
        }

        [TestFixture]
        private class ReferenceCheckTest
        {
            [Test]
            public void UndefinedVariableReferenceCausesReferenceError()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tSystem.out.println(foo);\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("foo"));
            }

            [Test]
            public void CannotCallMethodForABuiltinType()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tint foo;\n" +
                                 "\t\tfoo = 42;\n" +
                                 "\t\tSystem.out.println(foo.bar());\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("builtin"));
            }

            [Test]
            public void CannotCallMethodOtherThanLengthForArray()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tint[] foo;\n" +
                                 "\t\tfoo = new int[10];\n" +
                                 "\t\tSystem.out.println(foo.bar());\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("array"));
            }

            [Test]
            public void CanCallLengthMethodForArray()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tint[] foo;\n" +
                                 "\t\tfoo = new int[10];\n" +
                                 "\t\tSystem.out.println(foo.length);\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void CanCallResolvableMethodInSameClass()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t}\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "\tpublic boolean foo()" +
                                 "\t{\n" +
                                 "\t\treturn true;" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "\tpublic boolean bar() {\n" +
                                 "\t\treturn this.foo();\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void CanCallMethodForAnInstance()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t}\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "\tpublic boolean foo()" +
                                 "\t{\n" +
                                 "\t\treturn true;" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class B {\n" +
                                 "\tA bar;\n" +
                                 "\tpublic boolean bar() {\n" +
                                 "\t\tbar = new A();\n" +
                                 "\t\treturn bar.foo();\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void CanCallMethodForAJustCreatedInstance()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t}\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "\tpublic boolean foo()" +
                                 "\t{\n" +
                                 "\t\treturn true;" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class B {\n" +
                                 "\tpublic boolean bar() {\n" +
                                 "\t\treturn new A().foo();\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void CannotCallUndefinedMethod()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t}\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "\tpublic boolean foo()" +
                                 "\t{\n" +
                                 "\t\treturn this.bar();" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("bar"));
            }

            [Test]
            public void CanReferenceVariableFromEnclosingClassScope()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t}\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "\tint foo;" +
                                 "\tpublic int foo()" +
                                 "\t{\n" +
                                 "\t\tfoo = 42;" +
                                 "\t\treturn foo;" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void VariableMustBeDeclaredBeforeReference()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t}\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "\tpublic boolean foo()" +
                                 "\t{\n" +
                                 "\t\tif (42 == 42)\n" +
                                 "\t\t\treturn foo;\n" +
                                 "\t\telse\n" +
                                 "\t\t\treturn false;\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("foo"));
            }

            [Test]
            public void IfBlockHasItsOwnScope()
            {
                string program = "class Factorial {\n" +
                                 "\t public static void main () {\n" +
                                 "\t\t if (true)\n" +
                                 "\t\t\t int foo;" +
                                 "\t\t foo = 42;\n" +
                                 "\t} \n" +
                                 "} \n\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Could not resolve symbol foo"));
            }

            [Test]
            public void IfAndElseBlocksAreInSeparateScopes()
            {
                string program = "class Factorial {\n" +
                                 "\t public static void main () {\n" +
                                 "\t\t if (true)\n" +
                                 "\t\t\t int foo;" +
                                 "\t\t else \n" +
                                 "\t\t\t foo = 42;\n" +
                                 "\t} \n" +
                                 "} \n\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Could not resolve symbol foo"));
            }

            [Test]
            public void CannotCallAStaticMethodForAnInstance()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t}\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "\tint foo;" +
                                 "\tpublic int foo()" +
                                 "\t{\n" +
                                 "\t\tnew Foo().main();" +
                                 "\t\treturn 1;" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("main").And.StringContaining("static"));
            }
        }

        [TestFixture]
        private class UnaryOperatorTypeCheckTest
        {
            [Test]
            public void InvalidOperandTypeForAUnaryOperatorCausesError()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = !42;\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("int"));
            }


            [Test]
            public void ValidOperandForAUnaryOperator()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = !true;\n" +
                                 "\t\tfoo = !foo;\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }
        }

        [TestFixture]
        private class InvalidOperandsForAnArithmeticBinaryOperatorTest
        {
            [Datapoints]
            public string[] arithmeticOperators = new [] { "+", "-", "/", "*", "%" };

            [Theory]
            public void InvalidLeftOperandForAnArithmeticBinaryOperatorCausesError(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tint foo;\n" +
                                 "\t\tfoo = new A() " + op + " 1;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("A"));
            }

            [Theory]
            public void InvalidRightOperandForAnArithmeticBinaryOperatorCausesError(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tint foo;\n" +
                                 "\t\tfoo = 1 " + op + " true;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("boolean"));
            }

            [Theory]
            public void InvalidBothOperandsForAnArithmeticBinaryOperatorCausesError(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tint foo;\n" +
                                 "\t\tfoo = new A() " + op + " true;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("A").And.StringContaining("boolean"));
            }

            [Theory]
            public void ValidOperandsForAnArithmeticBinaryOperator(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tint foo;\n" +
                                 "\t\tfoo = 5;\n" +
                                 "\t\tfoo = foo " + op + " 1;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }
        }

        // TODO: test other operator types
        // TODO: test other type checks
    }
}
