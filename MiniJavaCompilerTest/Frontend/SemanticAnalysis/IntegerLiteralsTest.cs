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
        public class IntegerLiteralsTest
        {
            [Test]
            public void ValidIntegerLiterals()
            {
                string program = "class Foo{\n" +
                                 "   public static void main() {\n" +
                                 "     int foo;\n" +
                                 "     foo = 0;\n" +
                                 "     foo = 00115;\n" + // this would be an octal integer literal in Java but is interpreted as int (with leading zeros dropped) in this implementation
                                 "     foo = 999999999;\n" +
                                 "     foo = 2147483647;\n" +
                                 "}\n" +
                                 "}\n";
                IErrorReporter errors;
                var checker = SetUpTypeAndReferenceChecker(program, out errors);
                Assert.True(checker.RunCheck());
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
                Assert.False(checker.RunCheck());
                Assert.AreEqual(1, errors.Count);
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("2147483648")
                    .And.StringContaining("too large"));
            }
        }
    }
}
