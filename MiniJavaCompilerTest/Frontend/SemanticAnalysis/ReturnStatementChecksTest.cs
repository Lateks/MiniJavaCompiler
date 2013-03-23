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
        public class ReturnStatementChecksTest
        {
            [Test]
            public void VoidMethodCannotHaveAReturnStatement()
            {   // Note: MiniJava does not define an empty return statement.
                string program = "class Foo{\n" +
                                 "   public static void main() { return 42; }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot return a value from a method whose result type is void"));
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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Missing return statement"));
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
                Assert.True(checker.RunCheck());
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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Missing return statement"));
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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Missing return statement"));
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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Missing return statement"));
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
                Assert.True(checker.RunCheck());
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
                Assert.True(checker.RunCheck());
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
                Assert.False(checker.RunCheck());
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
                Assert.True(checker.RunCheck());
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
                Assert.False(checker.RunCheck());
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
                Assert.False(checker.RunCheck());
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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot convert").
                    And.StringContaining("type A to A[]"));
            }
        }
    }
}
