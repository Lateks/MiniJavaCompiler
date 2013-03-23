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
        public class UnaryOperatorTypeCheckTest
        {
            [Test]
            public void InvalidOperandTypeForAUnaryOperatorCausesError()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    boolean foo;\n" +
                                 "    foo = !42;\n" +
                                 "    foo = !(new Foo());\n" +
                                 "  }\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.False(checker.RunCheck());
                Assert.AreEqual(2, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("int"));
                Assert.That(errors.Errors[1].ToString(), Is.StringContaining("Cannot apply operator").And.StringContaining("Foo"));
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
                Assert.True(checker.RunCheck());
            }
        }
    }
}
