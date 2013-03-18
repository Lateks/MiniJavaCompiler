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
        public class ReferenceCheckTest
        {
            [Test]
            public void ReferenceToUndeclaredVariableReferenceCausesError()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    System.out.println(foo);\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("foo"));
            }

            [Test]
            public void LocalVariableMustBeInitializedBeforeReference()
            {
                string program = "class Foo {\n" +
                  "  public static void main() {\n" +
                  "    int foo;\n" +
                  "    System.out.println(foo);\n" +
                  "  }\n" +
                  "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("foo might not have been initialized"));
            }

            [Test]
            public void LocalVariableInitializationErrorIsOnlyReportedOnce()
            {
                string program = "class Foo {\n" +
                  "  public static void main() {\n" +
                  "     int[] foo;\n" +
                  "     foo[0] = 0;\n" +
                  "     foo[1] = 1;\n" +
                  "  }\n" +
                  "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("foo might not have been initialized"));
            }

            [Test]
            public void ClassVariableIsInitializedAutomaticallyAndCanBeReferenced()
            {
                string program = "class Foo {\n" +
                  "   public static void main() {\n" +
                  "     Bar foo;\n" +
                  "     foo = new Bar();\n" +
                  "     System.out.println(foo.getFieldValue());\n" +
                  "  }\n" +
                  "}\n" +
                  "class Bar {\n" +
                  "   int x;\n" +
                  "   public int getFieldValue() { return x; }\n" +
                  "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
                Assert.AreEqual(0, errors.Count);
            }

            [Test]
            public void CannotCallMethodForABuiltInType()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int foo;\n" +
                                 "    foo = 42;\n" +
                                 "    System.out.println(foo.bar());\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find symbol bar"));
            }

            [Test]
            public void CannotCallMethodForAVoidType()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    System.out.println(new A().foo().bar());\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public void foo() { }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Void cannot be dereferenced"));
            }

            [Test]
            public void CannotCallMethodOtherThanLengthForArray()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int[] foo;\n" +
                                 "    foo = new int[10];\n" +
                                 "    System.out.println(foo.bar());\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find symbol bar"));
            }

            [Test]
            public void CanCallLengthMethodForArray()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int[] foo;\n" +
                                 "    foo = new int[10];\n" +
                                 "    System.out.println(foo.length);\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void CanCallResolvableMethodsInSameClassOrSuperclass()
            {
                string program = "class Foo {" +
                                 "  public static void main() { }" +
                                 "}" +
                                 "class A {" +
                                 "  public boolean foo()" +
                                 "  {" +
                                 "    return true;" +
                                 "  }" +
                                 "}" +
                                 "class B extends A {" +
                                 "  public boolean bar() {" +
                                 "    this.baz();" +
                                 "    return this.foo();" +
                                 "  }" +
                                 "  public void baz() { }" +
                                 "}";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void CannotCallSubclassMethodThroughSuperclassVariable()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    A a;\n" +
                                 "    a = new B();\n" +
                                 "    a.foo(); // Ok, and calls the method defined in B (although this is actually resolved at runtime).\n" +
                                 "    a.bar(); // Not ok, because bar cannot be resolved at compile time.\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "  public boolean foo()\n" +
                                 "  {\n" +
                                 "    return true;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "  public boolean foo() {\n" +
                                 "    return false;\n" +
                                 "  }\n" +
                                 "  public void bar() { }\n" +
                                 "}";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("find").
                    And.StringContaining("bar"));
                Assert.AreEqual(6, errors.Errors[0].Row);
            }

            [Test]
            public void CanCallMethodForAnInstance()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "  }\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "  public boolean foo()" +
                                 "  {\n" +
                                 "    return true;" +
                                 "  }\n" +
                                 "}\n" +
                                 "class B {\n" +
                                 "  A bar;\n" +
                                 "  public boolean bar() {\n" +
                                 "    bar = new A();\n" +
                                 "    return bar.foo();\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void CanCallMethodForAJustCreatedInstance()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "  }\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "  public boolean foo()" +
                                 "  {\n" +
                                 "    return true;" +
                                 "  }\n" +
                                 "}\n" +
                                 "class B {\n" +
                                 "  public boolean bar() {\n" +
                                 "    return new A().foo();\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void CannotCallUndefinedMethod()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "  }\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "  public boolean foo()" +
                                 "  {\n" +
                                 "    return this.bar();" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("bar"));
            }

            [Test]
            public void CanReferenceVariableFromEnclosingClassScope()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "  }\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "  int foo;" +
                                 "  public int foo()" +
                                 "  {\n" +
                                 "    foo = 42;" +
                                 "     while (foo > 0)\n" +
                                 "       foo = foo - 1;\n" +
                                 "    return foo;" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void VariableMustBeDeclaredBeforeReference()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "   }\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "   public boolean foo()" +
                                 "  {\n" +
                                 "     if (42 == 42)\n" +
                                 "       return foo;\n" +
                                 "     else\n" +
                                 "       return false;\n" +
                                 "     boolean foo;\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find").And.StringContaining("foo"));
            }

            [Test]
            public void VariableMustBeDeclaredBeforeReferenceEvenIfOnTheSamePhysicalRow()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "  System.out.println(foo); int foo; foo = 4;" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find").And.StringContaining("foo"));

            }

            [Test]
            public void IfBlockHasItsOwnScope()
            {
                string program = "class Factorial {\n" +
                                 "   public static void main () {\n" +
                                 "     if (true)\n" +
                                 "       int foo;" +
                                 "     foo = 42;\n" +
                                 "  } \n" +
                                 "} \n\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find symbol foo"));
            }

            [Test]
            public void WhileLoopHasItsOwnScope()
            {
                string program = "class Factorial {\n" +
                                 "   public static void main () {\n" +
                                 "     while (true)\n" +
                                 "       int foo;" +
                                 "     foo = 42;\n" +
                                 "  } \n" +
                                 "} \n\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find symbol foo"));
            }

            [Test]
            public void IfAndElseBlocksAreInSeparateScopes()
            {
                string program = "class Factorial {\n" +
                                 "   public static void main () {\n" +
                                 "     if (true)\n" +
                                 "       int foo;" +
                                 "     else \n" +
                                 "       foo = 42;\n" +
                                 "  } \n" +
                                 "} \n\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot find symbol foo"));
            }

            [Test]
            public void CannotCallAStaticMethodForAnInstance()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "  }\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "  int foo;" +
                                 "  public int foo()" +
                                 "  {\n" +
                                 "    new Foo().main();" +
                                 "    return 1;" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("main").And.StringContaining("static"));
            }

            [Test]
            public void CanDoRecursion()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "  }\n" +
                                 "}\n\n" +
                                 "class A {\n" +
                                 "  public int fib(int n)" +
                                 "  {\n" +
                                 "    if (n == 0 || n == 1)\n" +
                                 "       return 1;\n" +
                                 "    else\n" +
                                 "       return this.fib(n-1) + this.fib(n-2);\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void DoesNotReportReferenceErrorForMethodIfTypeNotResolved()
            {
                string program = "class Foo {\n" +
                  "  public static void main()\n" +
                  "  {\n" +
                  "    new A().foo();\n" +
                  "    A foo;\n" +
                  "    foo.foo();\n" +
                  "  }\n" +
                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(2, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Unknown type A.")); // instance creation error (no error about "A foo;" because declarations are checked in symbol table building phase)
                Assert.That(errors.Errors[1].ToString(), Is.StringContaining("Variable foo might not have been initialized."));
                // No errors about symbol foo() not being found because the compiler
                // does not even know where to start looking...
            }

            [Test]
            public void TypeMustBeResolvableInInstanceCreation()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() { int foo; foo = new A().foo(); }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Unknown type A"));
            }

            [Test]
            public void TypeMustBeResolvableInArrayCreation()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() { int foo; foo = new A[10].length; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Unknown type A"));
            }

            [Test]
            public void CannotCreateArrayOfVoidType()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() { int foo; foo = new void[10].length; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Illegal type void for array elements."));
            }
        }
    }
}
