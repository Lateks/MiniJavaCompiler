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
        public class ComparisonOperatorsTest
        {
            [Datapoints]
            public string[] ComparisonOperators = new[] { "<", ">" };

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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").
                    And.StringContaining("A"));
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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").
                    And.StringContaining("boolean"));
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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Cannot apply operator").
                    And.StringContaining("boolean").And.StringContaining("A"));
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
                Assert.True(checker.RunCheck());
            }
        }
    }
}
