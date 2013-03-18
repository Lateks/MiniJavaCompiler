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
    internal class TypeCheckerTest
    {
        internal static SemanticsChecker SetUpTypeAndReferenceChecker(string program, out IErrorReporter errorLog)
        {
            var reader = new StringReader(program);
            var scanner = new MiniJavaScanner(reader);
            var errors = new ErrorLogger();
            var parser = new Parser(scanner, errors, true);
            Program syntaxTree = parser.Parse();
            reader.Close();
            Assert.That(errors.Errors, Is.Empty);

            var symbolTableBuilder = new SymbolTableBuilder(syntaxTree, errors);
            Assert.That(errors.Errors, Is.Empty);

            SymbolTable symbolTable = null;
            Assert.DoesNotThrow(() => symbolTable = symbolTableBuilder.BuildSymbolTable());
            errorLog = new ErrorLogger();

            return new SemanticsChecker(syntaxTree, symbolTable, errorLog);
        }

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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol bar"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("void cannot be dereferenced"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol bar"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("resolve").
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve").And.StringContaining("foo"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve").And.StringContaining("foo"));

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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol foo"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol foo"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol foo"));
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
            public void TypeMustBeResolvableInInstanceCreation()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() { int foo; foo = new A().foo(); }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(2, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol A"));
                Assert.That(errors.Errors[1].ToString(), Is.StringContaining("Cannot resolve symbol foo"));
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
                Assert.AreEqual(2, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol A"));
                Assert.That(errors.Errors[1].ToString(), Is.StringContaining("Cannot resolve symbol length")); // method cannot be resolved because type could not be either
            }
        }

        [TestFixture]
        public class UnaryOperatorTypeCheckTest
        {
            [Test]
            public void InvalidOperandTypeForAUnaryOperatorCausesError()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = !42;\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("int"));
            }


            [Test]
            public void ValidOperandForAUnaryOperator()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = !true;\n" +
                                 "    foo = !foo;\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void CannotIndexNonArrayExpression()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "     int foo;\n" +
                                 "     foo = 0;\n" +
                                 "     foo[0] = 42;\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot index into"));
            }

            [Test]
            public void ArrayIndexExpressionMustBeAnInteger()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int[] foo;\n" +
                                 "    foo = new int[10];\n" +
                                 "    foo[true] = 0;\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Invalid array index"));
            }

            [Test]
            public void ArraySizeMustBeAnInteger()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() { int foo; foo = new int[true].length; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Array size must be numeric"));
            }

            [Test]
            public void ValidArraySize()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() { int foo; foo = new int[10 + 11 % 2].length; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
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
                                 "  public static void main() {\n" +
                                 "    int foo;\n" +
                                 "    foo = new A() " + op + " 1;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("A"));
            }

            [Theory]
            public void InvalidRightOperandForAnArithmeticBinaryOperatorCausesError(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int foo;\n" +
                                 "    foo = 1 " + op + " true;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("boolean"));
            }

            [Theory]
            public void InvalidBothOperandsForAnArithmeticBinaryOperatorCausesError(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int foo;\n" +
                                 "    foo = new A() " + op + " true;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("A").And.StringContaining("boolean"));
            }

            [Theory]
            public void ValidOperandsForAnArithmeticBinaryOperator(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int foo;\n" +
                                 "    foo = 5;\n" +
                                 "    foo = foo " + op + " 1;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
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
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = new A() " + op + " false;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("A"));
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
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("int"));
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
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("int").And.StringContaining("A"));
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
                Assert.DoesNotThrow(checker.RunCheck);
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
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = new A() " + op + " 100;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("A"));
            }

            [Theory]
            public void InvalidRightOperandForAComparisonOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = 99 " + op + " false;\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("boolean"));
            }

            [Theory]
            public void InvalidBothOperandsForAComparisonOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = new A() " + op + " false;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("boolean").And.StringContaining("A"));
            }

            [Theory]
            public void ValidOperandsForAComparisonOperatorTest(string op)
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = 4 " + op + " 5;\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }
        }

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
                                 "     A[] bar;\n" +
                                 "     bar = new A[2];\n" +
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").And.
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").And.
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").And.
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").And.
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").And.
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").And.
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").
                    And.StringContaining("expected int").And.StringContaining("found void"));
            }
        }

        [TestFixture]
        public class EqualsOperatorTypeCheckTest
        {
            [Test]
            public void ValidEqualsOperations()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "   boolean a;\n" +
                                 "   int b; b = 10;\n" +
                                 "   a = 10 == 42;\n" +
                                 "   a = b == 10;\n" +
                                 "   a = false == true;\n" +
                                 "   A foo;\n foo = new A();\n" +
                                 "   a = new A() == foo;\n" +
                                 "   a = new B() == new A();\n" +
                                 "   a = foo == new B();\n" +
                                 "   a = new A[10] == new A[10];\n" +
                                 "   a = new int[10] == new int[100];\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidEqualsOperations()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "   boolean a;\n" +
                                 "   int b; b = 10;\n" +
                                 "   a = new A() == 42;\n" +
                                 "   a = false == b;\n" +
                                 "   a = true == new B();\n" +
                                 "   a = false == 0;\n" +
                                 "   a = new A() == new A[10];\n" +
                                 "   a = new A[10] == new B[10]; // arrays are compatible only if they have the same element type\n" +
                                 "   a = new int[10] == new boolean[10];\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A { }\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.That(errors.Count, Is.EqualTo(7));
            }
        }

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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve").And.StringContaining("foo"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("incompatible types").
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

        [TestFixture]
        public class TestReturnStatementChecks
        {
            [Test]
            public void VoidMethodCannotHaveAReturnStatement()
            {   // Note: MiniJava does not define an empty return statement.
                string program = "class Foo{\n" +
                                 "   public static void main() { return 42; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("cannot return a value from a method whose result type is void"));
            }

            [Test]
            public void NonVoidMethodRequiresAReturnStatement()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo() { }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("missing return statement"));
            }

            [Test]
            public void OkReturnStatements()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(boolean bar) {\n" +
                                 "     int foo;\n" +
                                 "     foo = 10;\n" +
                                 "     if (bar) \n" +
                                 "       foo = foo + 1;\n" +
                                 "     else \n" +
                                 "       foo = foo - 1;\n" +
                                 "     return foo;\n" +
                                 "   }\n" +
                                 "   public int bar(boolean foo) {\n" +
                                 "     int baz;\n" +
                                 "     baz = 0;\n" +
                                 "     if (foo) {\n" +
                                 "       baz = baz + 1;\n" +
                                 "       return baz;\n" +
                                 "     } else \n" +
                                 "       return 0;\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void AllPathsInANonVoidMethodRequireReturnStatements_FaultyIfBranch()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(boolean bar) {\n" +
                                 "     int foo;\n" +
                                 "     foo = 10;\n" +
                                 "     if (bar) \n" +
                                 "       foo = foo + 1;\n" +
                                 "     else\n" +
                                 "       return foo;" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("missing return statement"));
            }

            [Test]
            public void AllPathsInANonVoidMethodRequireReturnStatements_FaultyElseBranch()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(boolean bar) {\n" +
                                 "     int foo;\n" +
                                 "     foo = 10;\n" +
                                 "     if (bar) \n" +
                                 "       return foo;\n" +
                                 "     else\n" +
                                 "       foo = foo + 1;\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("missing return statement"));
            }

            [Test]
            public void AllPathsInANonVoidMethodRequireReturnStatements_IfWithoutAnElseBranch()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(boolean bar) {\n" +
                                 "     int foo;\n" +
                                 "     foo = 10;\n" +
                                 "     if (bar) \n" +
                                 "       return foo;\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("missing return statement"));
            }

            [Test]
            public void ReturnStatementChecksTakeAnonymousBlocksIntoAccount()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo() {\n" +
                                 "     int foo;\n" +
                                 "     foo = 42;\n" +
                                 "     {\n" +
                                 "       {\n" +
                                 "         return foo;\n" +
                                 "       }\n" +
                                 "     }\n" +
                                 "}\n" +
                                 "   public int bar() {\n" +
                                 "     {\n" +
                                 "       { }\n" +
                                 "     }\n" +
                                 "     return 0;\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void BasicValidReturnStatements()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo() { return 42; }\n" +
                                 "   public boolean bar() { return true; }\n" +
                                 "}\n" +
                                 "class B {\n" +
                                 "   public A foo() { return new A(); }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidReturnStatement()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo() { return false; }\n" +
                                 "}\n" +
                                 "class B { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert").
                    And.StringContaining("type boolean to int"));
            }

            [Test]
            public void ValidPolymorphicReturnStatement()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public A foo() { return new B(); }\n" +
                                 "}\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidPolymorphicReturnStatement()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public B foo() { return new A(); }\n" +
                                 "}\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert").
                    And.StringContaining("type A to B"));
            }

            [Test]
            public void ArraysAreNonPolymorphicInReturnStatements()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public A[] foo() { return new B[10]; }\n" +
                                 "}\n" +
                                 "class B extends A { }\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert").
                    And.StringContaining("type B[] to A[]"));
            }

            [Test]
            public void ReturnTypeChecksTakeArraysIntoAccountCorrectly()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public A[] foo() { return new A(); }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert").
                    And.StringContaining("type A to A[]"));
            }
        }

        [TestFixture]
        public class OverridingAndOverloading
        {
            [Test]
            public void CannotOverloadASuperClassMethod()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo() { return 10; }\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "   public int foo(int bar) { return bar; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Overloading is not allowed"));
            }

            [Test]
            public void OverloadCheckTakesArraysIntoAccountCorrectly()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo(int[] bar) { return 10; }\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "   public int foo(int bar) { return bar; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Overloading is not allowed"));
            }

            [Test]
            public void CannotOverrideASuperClassMethodWithADifferentReturnType()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo() { return 10; }\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "   public boolean foo() { return true; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("different return type"));
            }

            [Test]
            public void CanOverrideASuperClassMethodWithTheSameReturnType()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public int foo() { return 10; }\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "   public int foo() { return 5; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void CanOverrideAVoidSuperClassMethod()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public void doSomething() { }\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "   public void doSomething() { }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            // Note: this differs from Java specification. Return type covariance
            // is not implemented because the .NET runtime does not support it
            // natively and therefore this would require a substantial amount of
            // work in implementing a working method dispatch strategy.
            //
            // According to Eric Lippert, this is the reason return type covariance
            // is not supported by C# either (see his post at
            // http://stackoverflow.com/questions/5709034/does-c-sharp-support-return-type-covariance/5709191#5709191).
            [Test]
            public void ReturnTypeCovarianceIsNOTAllowedInOverridingMethods()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public A foo() { return new A(); }\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "   public B foo() { return new B(); }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].Content,
                    Is.StringContaining("different return type"));
            }

            [Test]
            public void IgnoresUnresolvedParameterTypeInOverloadCheck()
            {
                string program = "class Foo{\n" +
                 "   public static void main() { }\n" +
                 "}\n" +
                 "class A {\n" +
                 "   public int foo(C foo) { return 10; }\n" +
                 "}\n" +
                 "class B extends A {\n" +
                 "   public int foo(int foo) { return 0; }\n" +
                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(0, errors.Count);
            }

            [Test]
            public void ReturnTypeContravarianceIsNotAllowedInOverridingMethods()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() { }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "   public B foo() { return new B(); }\n" +
                                 "}\n" +
                                 "class B extends A {\n" +
                                 "   public A foo() { return new A(); }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("different return type"));
            }
        }

        [TestFixture]
        public class MethodCallParametersAndSimilarTypeChecks
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert expression of type int to boolean"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert expression of type A to boolean"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert expression of type int to boolean"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot print expression of type boolean"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot print expression of type int[]"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot print expression of type A"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert expression of type int to boolean"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Wrong type of argument to method foo"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Wrong type of argument to method foo"));
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
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol C"));
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

        [TestFixture]
        public class IntegerLiterals
        {
            [Test]
            public void ValidIntegerLiterals()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     int foo;\n" +
                                 "     foo = 0;\n" +
                                 "     foo = 999999999;\n" +
                                 "     foo = 2147483647;\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.DoesNotThrow(checker.RunCheck);
            }

            [Test]
            public void InvalidIntegerLiteral()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     int foo;\n" +
                                 "     foo = 2147483648;\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("2147483648").
                    And.StringContaining("Cannot fit").And.StringContaining("32-bit integer"));
            }
        }

        [TestFixture]
        public class Recovery
        {
            [Test]
            public void CanRecoverToFindAllTypeAndReferenceErrors()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int foo;\n" +
                                 "    foo = 10 + new A().alwaysTrue();\n" +
                                 "    A foo2;\n" +
                                 "     foo2 = new C();\n" +
                                 "    int bar;\n" +
                                 "    bar = new A();\n" +
                                 "    bar = 99999999999999999;\n" +
                                 "    boolean baz; baz = 15 && new A().alwaysTrue(10) || new C() || foo;\n" +
                                 "    baz = zzz || foo;\n" +
                                 "    baz = foo && zzz;\n" +
                                 "    baz = zzz || new C();\n" +
                                 "    foo = zzz[zzz];\n" +
                                 "    assert(zzz);\n" +
                                 "  }\n" +
                                 "}\n" +
                                 "class A {\n" +
                                 "  public boolean alwaysTrue() {\n" +
                                 "     if (true) { }\n" +
                                 "     else { return true; }\n" +
                                 "  }\n" +
                                 "  public void foo() { return 10; }\n" +
                                 "  public boolean bar() { return true; }\n" +
                                 "}\n" +
                                 "class B extends A {" +
                                 "  public boolean alwaysTrue(int foo) { return true; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.Throws<CompilationError>(checker.RunCheck);
                Assert.That(errors.Count, Is.EqualTo(20));
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot resolve symbol C"));
                Assert.That(errors.Errors[1].ToString(), Is.StringContaining("Cannot resolve symbol C"));
                Assert.That(errors.Errors[2].ToString(), Is.StringContaining("Cannot resolve symbol zzz"));
                Assert.That(errors.Errors[3].ToString(), Is.StringContaining("Cannot resolve symbol zzz"));
                Assert.That(errors.Errors[4].ToString(), Is.StringContaining("Cannot resolve symbol zzz"));
                Assert.That(errors.Errors[5].ToString(), Is.StringContaining("Cannot resolve symbol C")); // Note: No error about operands for || because neither one could be resolved.
                Assert.That(errors.Errors[6].ToString(), Is.StringContaining("Cannot resolve symbol zzz")); // Note: No error about array indexing because array expr could not be resolved.
                Assert.That(errors.Errors[7].ToString(), Is.StringContaining("Cannot resolve symbol zzz")); // Note: No error about array index type because variable could not be resolved.
                Assert.That(errors.Errors[8].ToString(), Is.StringContaining("Cannot resolve symbol zzz")); // Note: No error about invalid argument to assert statement because variable could not be resolved.
                Assert.That(errors.Errors[9].ToString(), Is.StringContaining("Cannot apply operator + on arguments of type int and boolean"));
                Assert.That(errors.Errors[10].ToString(), Is.StringContaining("incompatible types (expected int, found A)"));
                Assert.That(errors.Errors[11].ToString(), Is.StringContaining("Cannot fit integer literal 99999999999999999 into a 32-bit integer variable"));
                Assert.That(errors.Errors[12].ToString(), Is.StringContaining("Wrong number of arguments to method alwaysTrue (1 for 0)"));
                Assert.That(errors.Errors[13].ToString(), Is.StringContaining("Cannot apply operator && on arguments of type int and boolean"));
                Assert.That(errors.Errors[14].ToString(), Is.StringContaining("Cannot apply operator || on arguments of type boolean and int"));
                Assert.That(errors.Errors[15].ToString(), Is.StringContaining("Invalid operand of type int for operator ||"));
                Assert.That(errors.Errors[16].ToString(), Is.StringContaining("Invalid operand of type int for operator &&"));
                Assert.That(errors.Errors[17].ToString(), Is.StringContaining("missing return statement in method alwaysTrue"));
                Assert.That(errors.Errors[18].ToString(), Is.StringContaining("cannot return a value from a method whose result type is void"));
                Assert.That(errors.Errors[19].ToString(), Is.StringContaining("Method alwaysTrue in class B overloads a method in class A"));
            }

        }
    }
}
