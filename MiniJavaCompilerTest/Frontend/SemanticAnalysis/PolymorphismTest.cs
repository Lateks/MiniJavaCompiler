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
        public class PolymorphismTest
        {
            [Test]
            public void ValidPolymorphicAssignmentTest()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     A foo;\n" +
                                 "     foo = new B();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidPolymorphicAssignmentTest()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     B foo;\n" +
                                 "     foo = new A();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").
                    And.StringContaining("found A").And.StringContaining("expected B"));
            }

            [Test]
            public void ValidPolymorphicMethodCallTest()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     A foo;\n" +
                                 "     foo = new B();\n" +
                                 "     int bar;\n" +
                                 "     bar = foo.foo();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo() { return 42; }\n" +
                                 "}\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidPolymorphicMethodCallTest()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     A foo;\n" +
                                 "     foo = new B();\n" +
                                 "     int bar;\n" +
                                 "     bar = foo.foo();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A {\n" +
                                 "   public int foo() { return 42; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find").
                    And.StringContaining("foo"));
            }

            [Test]
            public void ArraysAreNonPolymorphicInAssignments()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { A[] foo; foo = new B[10]; }\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").
                    And.StringContaining("found B[]").And.StringContaining("expected A[]"));
            }

            [Test]
            public void ArraysAreNonPolymorphicInMethodCalls()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { int foo; foo = new A().arrayLen(new B[10]); }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int arrayLen(A[] array) { return array.length; }\n" +
                                 " }\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Wrong type of argument").
                    And.StringContaining("B[]").And.StringContaining("A[]"));
            }
        }
    }
}
