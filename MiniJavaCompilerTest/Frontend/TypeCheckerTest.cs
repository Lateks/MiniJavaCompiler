using System.IO;
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
        public class ReferenceCheckTest
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
            public void VariableMustBeDeclaredBeforeReferenceEvenIfOnTheSamePhysicalRow()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\tSystem.out.println(foo); int foo; foo = 4;" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Could not resolve").And.StringContaining("foo"));

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

            [Test]
            public void CanDoRecursion()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t}\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "\tpublic int fib(int n)" +
                                 "\t{\n" +
                                 "\t\tif (n == 0 || n == 1)\n" +
                                 "\t\t\t return 1;\n" +
                                 "\t\telse\n" +
                                 "\t\t\t return this.fib(n-1) + this.fib(n-2);\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }
        }

        [TestFixture]
        public class UnaryOperatorTypeCheckTest
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
        public class OperandsForAnArithmeticBinaryOperatorTest
        {
            [Datapoints]
            public string[] ArithmeticOperators = new [] { "+", "-", "/", "*", "%" };

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

        [TestFixture]
        public class OperandsForALogicalBinaryOperatorTest
        {
            [Datapoints]
            public string[] LogicalOperators = new [] { "&&", "||" };

            [Theory]
            public void InvalidLeftOperandForALogicalOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = new A() " + op + " false;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("A"));
            }

            [Theory]
            public void InvalidRightOperandForALogicalOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = true " + op + " 1;\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("int"));
            }

            [Theory]
            public void InvalidBothOperandsForALogicalOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = new A() " + op + " 1;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("int").And.StringContaining("A"));
            }

            [Theory]
            public void ValidOperandsForALogicalOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = true " + op + " foo;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }
        }

        [TestFixture]
        public class OperandsForComparisonOperatorsTest
        {
            [Datapoints]
            public string[] ComparisonOperators = new [] { "<", ">" };

            [Theory]
            public void InvalidLeftOperandForAComparisonOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = new A() " + op + " 100;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("A"));
            }

            [Theory]
            public void InvalidRightOperandForAComparisonOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = 99 " + op + " false;\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("boolean"));
            }

            [Theory]
            public void InvalidBothOperandsForAComparisonOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = new A() " + op + " false;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot apply operator").And.StringContaining("boolean").And.StringContaining("A"));
            }

            [Theory]
            public void ValidOperandsForAComparisonOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = 4 " + op + " 5;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }
        }

        [TestFixture]
        public class AssignmentTypeCheckTest
        {
            [Test]
            public void BasicValidAssignmentsTest()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tint foo;\n" +
                                 "\t\tfoo = 10;\n" +
                                 "\t\tint bar;\n" +
                                 "\t\tbar = foo;\n" +
                                 "\t\tboolean baz; baz = new A().alwaysTrue();\n" +
                                 "\t\tboolean baz_copy;\n" +
                                 "\t\tbaz_copy = true\n;" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "\tA foo;\n" +
                                 "\tpublic boolean alwaysTrue() {\n" +
                                 "\t\tfoo = new A(); // pointless side effect\n" +
                                 "\t\treturn true;\n\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void InvalidAssignmentToBuiltinFromMethod()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = new A().foo();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "\tpublic int foo() {" +
                                 "\t\treturn 42;\n" +
                                 "\t}\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot assign").And.
                    StringContaining("int").And.StringContaining("boolean"));
            }

            [Test]
            public void InvalidAssignmentToBuiltin()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tboolean foo;\n" +
                                 "\t\tfoo = new A();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot assign").And.
                    StringContaining("A").And.StringContaining("boolean"));
            }

            [Test]
            public void InvalidAssignmentToArrayVariable()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tA[] foo;\n" +
                                 "\t\tfoo = new A();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot assign").And.
                    StringContaining("A").And.StringContaining("array[A]"));
            }

            [Test]
            public void InvalidArrayAssignment()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tA foo;\n" +
                                 "\t\tfoo = new A[10];\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot assign").And.
                    StringContaining("A").And.StringContaining("array[A]"));
            }

            [Test]
            public void InvalidAssignmentToUserDefinedTypeVariable()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t\tA foo;\n" +
                                 "\t\tfoo = new B();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot assign").And.
                    StringContaining("A").And.StringContaining("B"));
            }

            [Test]
            public void UnassignableLeftHandSideInAssignment()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t new A() = new A();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("is not assignable"));
            }
        }

        [TestFixture]
        public class EqualsOperatorTypeCheckTest
        {
            [Test]
            public void AcceptsAnyOperands()
            {
                string program = "class Foo {\n" +
                                 "\tpublic static void main() {\n" +
                                 "\t boolean a;\n" +
                                 "\t a = new A() == 42;\n" +
                                 "\t a = false == true;\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }
        }

        [TestFixture]
        public class PolymorphismTest
        {
            [Test]
            public void ValidPolymorphicAssignmentTest()
            {
                string program = "class Foo{\n" +
                                 "\t public static void main() {\n" +
                                 "\t\t A foo;\n" +
                                 "\t\t foo = new B();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void InvalidPolymorphicAssignmentTest()
            {
                string program = "class Foo{\n" +
                                 "\t public static void main() {\n" +
                                 "\t\t B foo;\n" +
                                 "\t\t foo = new A();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot assign").
                    And.StringContaining("type A").And.StringContaining("type B"));
            }

            [Test]
            public void ValidPolymorphicMethodCallTest()
            {
                string program = "class Foo{\n" +
                                 "\t public static void main() {\n" +
                                 "\t\t A foo;\n" +
                                 "\t\t foo = new B();\n" +
                                 "\t\t int bar;\n" +
                                 "\t\t bar = foo.foo();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "\t public int foo() { return 42; }\n" +
                                 "}\n" +
                                 "class B extends A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void InvalidPolymorphicMethodCallTest()
            {
                string program = "class Foo{\n" +
                                 "\t public static void main() {\n" +
                                 "\t\t A foo;\n" +
                                 "\t\t foo = new B();\n" +
                                 "\t\t int bar;\n" +
                                 "\t\t bar = foo.foo();\n" +
                                 "\t}\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A {\n" +
                                 "\t public int foo() { return 42; }\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot resolve").And.StringContaining("foo"));
            }
        }

        [TestFixture]
        public class TestReturnStatementChecks
        {
            [Test]
            public void VoidMethodCannotHaveAReturnStatement()
            {
                string program = "class Foo{\n" +
                 "\t public static void main() { return 42; }\n" +
                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Method of type void cannot have return statements"));
            }

            [Test]
            public void BasicValidReturnStatements()
            {
                string program = "class Foo{\n" +
                                 "\t public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "\t public int foo() { return 42; }\n" +
                                 "\t public boolean bar() { return true; }\n" +
                                 "}\n" +
                                 "class B {\n" +
                                 "\t public A foo() { return new A(); }\n" +
                                 "}\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void InvalidReturnStatement()
            {
                string program = "class Foo{\n" +
                                 "\t public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "\t public int foo() { return false; }\n" +
                                 "}\n" +
                                 "class B { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot convert").
                    And.StringContaining("type boolean to int"));
            }

            [Test]
            public void ValidPolymorphicReturnStatement()
            {
                string program = "class Foo{\n" +
                                 "\t public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "\t public A foo() { return new B(); }\n" +
                                 "}\n" +
                                 "class B extends A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                Assert.DoesNotThrow(checker.CheckTypesAndReferences);
            }

            [Test]
            public void InvalidPolymorphicReturnStatement()
            {
                string program = "class Foo{\n" +
                                 "\t public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "\t public B foo() { return new A(); }\n" +
                                 "}\n" +
                                 "class B extends A { }\n";
                var checker = SetUpTypeAndReferenceChecker(program);
                var exception = Assert.Throws<TypeError>(checker.CheckTypesAndReferences);
                Assert.That(exception.Message, Is.StringContaining("Cannot convert").
                    And.StringContaining("type A to B"));
            }

            // TODO: arrays are non-polymorphic
        }

        // TODO: test other type checks
    }
}
