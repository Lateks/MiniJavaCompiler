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
        public class AssignmentTypeCheckTest
        {
            [Test]
            public void BasicValidAssignmentsTest()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int foo;\n" +
                                 "    foo = 10;\n" +
                                 "    int bar;\n" +
                                 "    bar = foo;\n" +
                                 "    boolean baz; baz = new A().alwaysTrue();\n" +
                                 "    boolean baz_copy;\n" +
                                 "    baz_copy = true\n;" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "  A foo;\n" +
                                 "  public boolean alwaysTrue() {\n" +
                                 "    foo = new A();\n" +
                                 "    A[] bar;\n" +
                                 "    bar = new A[2];\n" +
                                 "    bar[0] = foo;\n" + // can insert an object of type A into an array of type A
                                 "    bar[1] = new B();\n" + // can also insert an object of type B that inherits from A, even though corresponding array types would be incompatible
                                 "    return true;\n\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidArrayIndexAssignment()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "     A[] foo;\n" +
                                 "     foo = new A[10];\n" +
                                 "     foo[0] = new B();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").And.
                    StringContaining("A").And.StringContaining("B"));
            }

            [Test]
            public void InvalidAssignmentToBuiltInFromMethod()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = new A().foo();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "  public int foo() {" +
                                 "    return 42;\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").And.
                    StringContaining("int").And.StringContaining("boolean"));
            }

            [Test]
            public void InvalidAssignmentToBuiltIn()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = new A();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").And.
                    StringContaining("A").And.StringContaining("boolean"));
            }

            [Test]
            public void InvalidAssignmentToArrayVariable()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    A[] foo;\n" +
                                 "    foo = new A();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").And.
                    StringContaining("A").And.StringContaining("A[]"));
            }

            [Test]
            public void InvalidArrayAssignment()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    A foo;\n" +
                                 "    foo = new A[10];\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").And.
                    StringContaining("expected A").And.StringContaining("found A[]"));
            }

            [Test]
            public void InvalidAssignmentToUserDefinedTypeVariable()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    A foo;\n" +
                                 "    foo = new B();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").And.
                    StringContaining("A").And.StringContaining("B"));
            }

            [Test]
            public void UnassignableLeftHandSideInAssignment()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "   new A() = new A();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("is not assignable"));
            }

            [Test]
            public void CannotAssignReturnTypeOfVoidMethod()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "   int foo; foo = new A().foo();\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { public void foo() { } }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Incompatible types").
                    And.StringContaining("expected int").And.StringContaining("found void"));
            }
        }
    }
}
