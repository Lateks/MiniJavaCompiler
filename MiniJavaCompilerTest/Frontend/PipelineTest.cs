using System.IO;
using MiniJavaCompiler.FrontEnd;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using NUnit.Framework;
using System.Linq;

namespace MiniJavaCompilerTest.FrontEndTest
{
    [TestFixture]
    class PipelineTest
    {
        [Test]
        public void ValidProgramGoesThroughWithoutProblems()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "   public int ComputeFac (int num) {\n" +
                             "     assert (num > 0 || num == 0);\n" +
                             "     int num_aux;\n" +
                             "     if (num == 0)\n" +
                             "       num_aux = 1;\n" +
                             "     else \n" +
                             "       num_aux = num * this.ComputeFac (num-1);\n" +
                             "     return num_aux;\n" +
                             "   }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.True(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.NotNull(syntaxTree);
            Assert.That(frontend.GetErrors(), Is.Empty);
            reader.Close();
        }

        [Test]
        public void StopsAfterSyntaxAnalysisWhenThereAreLexicalErrors()
        {
            string program = "class Factorial {\n" +
                             "   public static $@# main () {\n" +
                             "     System.out.prin3#@tln @#(new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "   public int ComputeFac (int#@ num) {\n" +
                             "     assert (num > 0 || num == 0);\n" +
                             "     int num_aux;::\n" +
                             "     if (num == 0)\n" +
                             "       num_aux := 1;\n" +
                             "     else \n" +
                             "       num_aux = num * this.ComputeFac (num-1);\n" +
                             "     return num_aux;\n" +
                             "   }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.IsNull(syntaxTree);
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            reader.Close();
        }

        [Test]
        public void StopsAfterSyntaxAnalysisWhenThereAreSyntacticErrors()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () \n" +
                             "     System.out.println (new Fac.ComputeFac 0));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "   public int ComputeFac (int num) {\n" +
                             "     assert (num > 0 || num == 0\n" +
                             "     int num_aux;\n" +
                             "     if (num == 0)\n" +
                             "       num" +
                             "     else \n" +
                             "       num_aux this.ComputeFac (num-1);\n" +
                             "     return num_aux;\n" +
                             "   }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.IsNull(syntaxTree);
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            reader.Close();
        }

        [Test]
        public void StopsAfterBuildingTheTypeSetIfErrorsFound()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println (new Factorial ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Factorial { \n" +
                             "   public int ComputeFac (int num) {\n" +
                             "     assert (num > 0 || num == 0);\n" +
                             "     int num_aux;\n" +
                             "     if (num == 0)\n" +
                             "       num_aux = 1;\n" +
                             "     else \n" +
                             "       num_aux = num * this.ComputeFac (num-1);\n" +
                             "     return num_aux;\n" +
                             "   }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            Assert.That(frontend.GetErrors().Last().ToString(), Is.StringContaining("Conflicting definitions for Factorial"));
            reader.Close();
        }

        [Test]
        public void CanContinueToTypeCheckIfNonFatalErrorsFound()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "   public int ComputeFac (int num) {\n" +
                             "     assert (num > 0 || num == 0);\n" +
                             "     void num_aux;\n" +
                             "     if (num == 0)\n" +
                             "       num_aux = 1;\n" +
                             "     else \n" +
                             "       num_aux = num * this.ComputeFac (num-1);\n" +
                             "     return num_aux;\n" +
                             "   }\n" +
                             "   public int ComputeFac () { }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            Assert.That(frontend.GetErrors().Last().ToString(), Is.StringContaining("Missing return statement in method ComputeFac"));
            reader.Close();
        }

        [Test]
        public void StopsBeforeTypeCheckIfCyclicDependenciesFound()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class A extends B { }\n" +
                             "class B extends A {\n" +
                             "   public int foo() { }\n" + // missing return statement is not detected
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            Assert.That(frontend.GetErrors().Last().ToString(), Is.StringContaining("Cyclic inheritance involving B"));
            reader.Close();
        }

        [Test]
        public void OnlyReportsTypeResolvingErrorsOnce()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            var errors = frontend.GetErrors();
            Assert.AreEqual(1, errors.Count);
            Assert.That(errors[0].Content, Is.StringContaining("Unknown type Fac"));
            reader.Close();
        }

        [Test]
        public void FailsIfThereAreReferenceOrTypeErrors()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println (new Factor ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "   public int ComputeFac (int num) {\n" +
                             "     assert (num > true || num == 0);\n" +
                             "     int num_aux;\n" +
                             "     if (num == 0)\n" +
                             "       num_aux = 1;\n" +
                             "     else \n" +
                             "       aux = num * this.ComputeFac (num-1);\n" +
                             "     return aux;\n" +
                             "   }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            reader.Close();
        }

        [Test]
        public void SemanticChecksCanHandleMethodSymbolDefinitionError()
        {
            string program = "class Test {\n" +
                 "   public static void main () {\n" +
                 "     int i;\n" +
                 "     i = new Class().get();\n" +
                 "} \n\n" +
                 "} \n" +
                 "class Class { \n" +
                 "   public boolean get() { return true; }" +
                 "   public Class getClass() { return new Class(); }\n" +
                 "   public int get () {\n" +
                 "     OtherClass x;\n" +
                 "     x = this.getClass();\n" +
                 "     if (true) {\n" +
                 "       if (this.get()) { return false; }\n" +
                 "     } else { return 0; }\n" +
                 "   }\n" +
                 "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            var errors = frontend.GetErrors();
            Assert.AreEqual(5, errors.Count);
            Assert.That(errors[0].ToString(), Is.StringContaining("Incompatible types")
                .And.StringContaining("int").And.StringContaining("boolean"));
            Assert.That(errors[1].ToString(), Is.StringContaining("already defined"));
            Assert.That(errors[2].ToString(), Is.StringContaining("Missing return statement in method get"));
            Assert.That(errors[3].ToString(), Is.StringContaining("Cannot convert expression of type boolean to int"));
            Assert.That(errors[4].ToString(), Is.StringContaining("Unknown type OtherClass"));
            reader.Close();
        }

        [Test]
        public void OrdersErrorsByCodeLocation()
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
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            reader.Close();

            var errors = frontend.GetErrors();
            int[] locationValues = new int[errors.Count()];
            for (int i = 0; i < locationValues.Length; i++)
            {
                locationValues[i] = errors[i].Row * 100 + errors[i].Col;
            }
            int[] expectedValues = new int[errors.Count()];
            locationValues.CopyTo(expectedValues, 0);
            System.Array.Sort(expectedValues);

            CollectionAssert.AreEqual(expectedValues, locationValues);
        }
    }
}
