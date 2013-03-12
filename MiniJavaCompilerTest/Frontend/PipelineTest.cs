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
            Assert.That(frontend.GetErrors().Last().ToString(), Is.StringContaining("missing return statement in method ComputeFac"));
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
            Assert.That(frontend.GetErrors().Last().ToString(), Is.StringContaining("cyclic inheritance involving B"));
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
            Assert.That(errors[1].Content, Is.StringContaining("Cannot resolve symbol ComputeFac"));
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
    }
}
