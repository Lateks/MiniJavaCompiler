using System.IO;
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
        public class MethodCallParameterCheckTest
        {
            [Test]
            public void ValidAssertions()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     assert(true);\n" +
                                 "     assert(false);\n" +
                                 "     boolean foo;\n" +
                                 "     foo = true;\n" +
                                 "     assert(foo && true && !(10 == 9));\n" +
                                 "     int bar;\n" +
                                 "     bar = 10;\n" +
                                 "     assert(bar > 9);\n" +
                                 "     assert(bar == 10);\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidIntAssertion()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     assert(10 + 1);" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Cannot convert expression of type int to boolean"));
            }

            [Test]
            public void InvalidUserDefinedTypeAssertion()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     assert(new A());" +
                                 "}\n" +
                                 "}\n" +
                                 "class A { }";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Cannot convert expression of type A to boolean"));
            }

            [Test]
            public void ValidWhileLoopConditions()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     while (true) { }\n" +
                                 "     boolean foo;\n" +
                                 "     foo = true;\n" +
                                 "     while (foo) { }\n" +
                                 "     int bar;\n" +
                                 "     bar = 10;\n" +
                                 "     while (bar > 9) { }\n" +
                                 "     while (bar == 10) { }\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidWhileLoopCondition()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     while (10 + 1 % 2) { }\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Cannot convert expression of type int to boolean"));
            }

            [Test]
            public void ValidIfConditions()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     if (true) { }\n" +
                                 "     boolean foo;\n" +
                                 "     foo = true;\n" +
                                 "     if (foo) { }\n" +
                                 "     int bar;\n" +
                                 "     bar = 10;\n" +
                                 "     if (bar > 9 && bar < 15 && !(bar == 14) || bar % 2 == 0) { }\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void ValidPrintStatements()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(10 + 1 % 2);\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void CannotPrintABooleanValue()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(true);\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Cannot print expression of type boolean"));
            }

            [Test]
            public void CannotPrintAnArray()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(new int[10]);\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Cannot print expression of type int[]"));
            }

            [Test]
            public void CannotPrintAUserDefinedType()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(new A());\n" +
                                 "}\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Cannot print expression of type A"));
            }

            [Test]
            public void InvalidIfCondition()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     if (10 % 2) { }\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Cannot convert expression of type int to boolean"));
            }

            [Test]
            public void ValidMethodCallParameters()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(new A().foo(10, new B(), new B[10]));" +
                                 "}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(int bar, B baz, B[] bs) { return 0; }" +
                                 "}\n" +
                                 "class B { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void MethodCallParameterChecksTakeArraysIntoAccountCorrectly()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(new A().foo(10, new B(), new B()));" +
                                 "}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(int bar, B baz, B[] bs) { return 0; }" +
                                 "}\n" +
                                 "class B { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Wrong type of argument to method foo"));
            }

            [Test]
            public void InvalidMethodCallParameters()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(new A().foo(10, 11, new B()));\n" +
                                 "}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(int bar, B baz, B b) { return 0; }" +
                                 "}\n" +
                                 "class B { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining(
                    "Wrong type of argument to method foo"));
            }

            [Test]
            public void IgnoresParametersWithUnresolvedTypesInTypeCheck()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(new A().foo(new C()));\n" +
                                 "     System.out.println(new A().foo(10));\n" +
                                 "}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(B bar) { return 0; }" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find symbol C"));
                // And no errors about method call parameters...
            }

            [Test]
            public void WrongNumberOfArgumentsToFunctionCall()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(new A().foo(10, new B()));\n" +
                                 "}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(int bar, B baz, B b) { return 0; }" +
                                 "}\n" +
                                 "class B { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Wrong number of arguments"));
            }

            [Test]
            public void NoArgumentsToMethodCallThatRequiresThem()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     System.out.println(new A().foo());\n" +
                                 "}\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(int bar, B baz, B b) { return 0; }" +
                                 "}\n" +
                                 "class B { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Wrong number of arguments"));
            }
        }
    }
}
