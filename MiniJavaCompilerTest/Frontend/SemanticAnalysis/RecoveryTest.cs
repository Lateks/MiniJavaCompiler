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
        public class RecoveryTest
        {
            [Test]
            public void CanRecoverToFindAllTypeAndReferenceErrors()
            {
                string program = "class Foo {\n" +
                                 "  public static void main() {\n" +
                                 "    int foo;\n" +
                                 "    System.out.println(foo);\n" +
                                 "    foo = 10 + new A().alwaysTrue();\n" +
                                 "    A foo2;\n" +
                                 "    foo2 = new C();\n" +
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
                Assert.False(checker.RunCheck());
                Assert.That(errors.Count, Is.EqualTo(21));
                Assert.That(errors.Errors[0].ToString(), Is.StringContaining("Unknown type C"));
                Assert.That(errors.Errors[1].ToString(), Is.StringContaining("Unknown type C"));
                Assert.That(errors.Errors[2].ToString(), Is.StringContaining("Cannot find symbol zzz"));
                Assert.That(errors.Errors[3].ToString(), Is.StringContaining("Cannot find symbol zzz"));
                Assert.That(errors.Errors[4].ToString(), Is.StringContaining("Cannot find symbol zzz"));
                Assert.That(errors.Errors[5].ToString(), Is.StringContaining("Unknown type C")); // Note: No error about operands for || because neither one could be resolved.
                Assert.That(errors.Errors[6].ToString(), Is.StringContaining("Cannot find symbol zzz")); // Note: No error about array indexing because array expr could not be resolved.
                Assert.That(errors.Errors[7].ToString(), Is.StringContaining("Cannot find symbol zzz")); // Note: No error about array index type because variable could not be resolved.
                Assert.That(errors.Errors[8].ToString(), Is.StringContaining("Cannot find symbol zzz")); // Note: No error about invalid argument to assert statement because variable could not be resolved.
                Assert.That(errors.Errors[9].ToString(), Is.StringContaining("Variable foo might not have been initialized"));
                Assert.That(errors.Errors[10].ToString(), Is.StringContaining("Cannot apply operator + on arguments of type int and boolean"));
                Assert.That(errors.Errors[11].ToString(), Is.StringContaining("Incompatible types (expected int, found A)"));
                Assert.That(errors.Errors[12].ToString(), Is.StringContaining("Integer number 99999999999999999 too large."));
                Assert.That(errors.Errors[13].ToString(), Is.StringContaining("Wrong number of arguments to method alwaysTrue (1 for 0)"));
                Assert.That(errors.Errors[14].ToString(), Is.StringContaining("Cannot apply operator && on arguments of type int and boolean"));
                Assert.That(errors.Errors[15].ToString(), Is.StringContaining("Cannot apply operator || on arguments of type boolean and int"));
                Assert.That(errors.Errors[16].ToString(), Is.StringContaining("Invalid operand of type int for operator ||"));
                Assert.That(errors.Errors[17].ToString(), Is.StringContaining("Invalid operand of type int for operator &&"));
                Assert.That(errors.Errors[18].ToString(), Is.StringContaining("Missing return statement in method alwaysTrue"));
                Assert.That(errors.Errors[19].ToString(), Is.StringContaining("Cannot return a value from a method whose result type is void"));
                Assert.That(errors.Errors[20].ToString(), Is.StringContaining("Method alwaysTrue in class B overloads a method in class A"));
            }
        }
    }
}
