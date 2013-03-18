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
    }
}
