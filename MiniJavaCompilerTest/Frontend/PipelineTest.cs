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
                             "\t public static void main () {\n" +
                             "\t\t System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "\t public int ComputeFac (int num) {\n" +
                             "\t\t assert (num > 0 || num == 0);\n" +
                             "\t\t int num_aux;\n" +
                             "\t\t if (num == 0)\n" +
                             "\t\t\t num_aux = 1;\n" +
                             "\t\t else \n" +
                             "\t\t\t num_aux = num * this.ComputeFac (num-1);\n" +
                             "\t\t return num_aux;\n" +
                             "\t }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.True(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.NotNull(symbolTable);
            Assert.NotNull(syntaxTree);
            Assert.That(frontend.GetErrors(), Is.Empty);
            reader.Close();
        }

        [Test]
        public void StopsAfterSyntaxAnalysisWhenThereAreLexicalErrors()
        {
            string program = "class Factorial {\n" +
                             "\t public static $@# main () {\n" +
                             "\t\t System.out.prin3#@tln @#(new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "\t public int ComputeFac (int#@ num) {\n" +
                             "\t\t assert (num > 0 || num == 0);\n" +
                             "\t\t int num_aux;::\n" +
                             "\t\t if (num == 0)\n" +
                             "\t\t\t num_aux := 1;\n" +
                             "\t\t else \n" +
                             "\t\t\t num_aux = num * this.ComputeFac (num-1);\n" +
                             "\t\t return num_aux;\n" +
                             "\t }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.IsNull(symbolTable);
            Assert.IsNull(syntaxTree);
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            reader.Close();
        }

        [Test]
        public void StopsAfterSyntaxAnalysisWhenThereAreSyntacticErrors()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () \n" +
                             "\t\t System.out.println (new Fac.ComputeFac 0));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "\t public int ComputeFac (int num) {\n" +
                             "\t\t assert (num > 0 || num == 0\n" +
                             "\t\t int num_aux;\n" +
                             "\t\t if (num == 0)\n" +
                             "\t\t\t num" +
                             "\t\t else \n" +
                             "\t\t\t num_aux this.ComputeFac (num-1);\n" +
                             "\t\t return num_aux;\n" +
                             "\t }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.IsNull(symbolTable);
            Assert.IsNull(syntaxTree);
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            reader.Close();
        }

        [Test]
        public void StopsAfterBuildingTheTypeSetIfErrorsFound()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println (new Factorial ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Factorial { \n" +
                             "\t public int ComputeFac (int num) {\n" +
                             "\t\t assert (num > 0 || num == 0);\n" +
                             "\t\t int num_aux;\n" +
                             "\t\t if (num == 0)\n" +
                             "\t\t\t num_aux = 1;\n" +
                             "\t\t else \n" +
                             "\t\t\t num_aux = num * this.ComputeFac (num-1);\n" +
                             "\t\t return num_aux;\n" +
                             "\t }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.IsNull(symbolTable);
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            Assert.That(frontend.GetErrors().Last().ToString(), Is.StringContaining("Conflicting definitions for Factorial"));
            reader.Close();
        }

        [Test]
        public void CanContinueToTypeCheckIfNonFatalErrorsFound()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "\t public int ComputeFac (int num) {\n" +
                             "\t\t assert (num > 0 || num == 0);\n" +
                             "\t\t void num_aux;\n" +
                             "\t\t if (num == 0)\n" +
                             "\t\t\t num_aux = 1;\n" +
                             "\t\t else \n" +
                             "\t\t\t num_aux = num * this.ComputeFac (num-1);\n" +
                             "\t\t return num_aux;\n" +
                             "\t }\n" +
                             "\t public int ComputeFac () { }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.IsNull(symbolTable);
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            Assert.That(frontend.GetErrors().Last().ToString(), Is.StringContaining("Missing return statement in method ComputeFac"));
            reader.Close();
        }

        [Test]
        public void StopsBeforeTypeCheckIfCyclicDependenciesFound()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class A extends B { }\n" +
                             "class B extends A {\n" +
                             "\t public int foo() { }\n" + // missing return statement is not detected
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.IsNull(symbolTable);
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            Assert.That(frontend.GetErrors().Last().ToString(), Is.StringContaining("Cyclic inheritance involving B"));
            reader.Close();
        }

        [Test]
        public void OnlyReportsTypeResolvingErrorsOnce()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.IsNull(symbolTable);
            var errors = frontend.GetErrors();
            Assert.AreEqual(2, errors.Count);
            Assert.That(errors[0].Content, Is.StringContaining("Unknown type Fac"));
            Assert.That(errors[1].Content, Is.StringContaining("Cannot find symbol ComputeFac"));
            reader.Close();
        }

        [Test]
        public void FailsIfThereAreReferenceOrTypeErrors()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println (new Factor ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "\t public int ComputeFac (int num) {\n" +
                             "\t\t assert (num > true || num == 0);\n" +
                             "\t\t int num_aux;\n" +
                             "\t\t if (num == 0)\n" +
                             "\t\t\t num_aux = 1;\n" +
                             "\t\t else \n" +
                             "\t\t\t aux = num * this.ComputeFac (num-1);\n" +
                             "\t\t return aux;\n" +
                             "\t }\n" +
                             "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.IsNull(symbolTable);
            Assert.That(frontend.GetErrors(), Is.Not.Empty);
            reader.Close();
        }

        [Test]
        public void OrdersErrorsByCodeLocation()
        {
            string program = "class Foo {\n" +
                 "\tpublic static void main() {\n" +
                 "\t\tint foo;\n" +
                 "\t\tfoo = 10 + new A().alwaysTrue();\n" +
                 "\t\tA foo2;\n" +
                 "\t\t foo2 = new C();\n" +
                 "\t\tint bar;\n" +
                 "\t\tbar = new A();\n" +
                 "\t\tbar = 99999999999999999;\n" +
                 "\t\tboolean baz; baz = 15 && new A().alwaysTrue(10) || new C() || foo;\n" +
                 "\t\tbaz = zzz || foo;\n" +
                 "\t\tbaz = foo && zzz;\n" +
                 "\t\tbaz = zzz || new C();\n" +
                 "\t\tfoo = zzz[zzz];\n" +
                 "\t\tassert(zzz);\n" +
                 "\t}\n" +
                 "}\n" +
                 "class A {\n" +
                 "\tpublic boolean alwaysTrue() {\n" +
                 "\t\t if (true) { }\n" +
                 "\t\t else { return true; }\n" +
                 "\t}\n" +
                 "\tpublic void foo() { return 10; }\n" +
                 "\tpublic boolean bar() { return true; }\n" +
                 "}\n" +
                 "class B extends A {" +
                 "\tpublic boolean alwaysTrue(int foo) { return true; }\n" +
                 "}\n";
            var reader = new StringReader(program);
            var frontend = new FrontEnd(reader);
            SymbolTable symbolTable;
            Program syntaxTree;
            Assert.False(frontend.TryProgramAnalysis(out syntaxTree, out symbolTable));
            Assert.NotNull(syntaxTree); // syntax analysis was ok
            Assert.IsNull(symbolTable);
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
