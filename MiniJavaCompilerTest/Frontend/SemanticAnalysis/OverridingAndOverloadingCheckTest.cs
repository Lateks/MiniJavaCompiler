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
        public class OverridingAndOverloadingCheckTest
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
    }
}
