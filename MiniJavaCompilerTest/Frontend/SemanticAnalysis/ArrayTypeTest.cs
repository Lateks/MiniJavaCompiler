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
        class ArrayTypeTest
        {
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
                Assert.False(checker.RunCheck());
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
                Assert.False(checker.RunCheck());
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
                Assert.False(checker.RunCheck());
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
                Assert.True(checker.RunCheck());
            }
        }
    }
}
