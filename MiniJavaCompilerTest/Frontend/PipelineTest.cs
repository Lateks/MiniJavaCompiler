﻿using System.IO;
using MiniJavaCompiler.Frontend;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using NUnit.Framework;
using System.Linq;

namespace MiniJavaCompilerTest.Frontend
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
            Assert.That(frontend.GetErrors().Last().Message, Is.StringContaining("Conflicting definitions for Factorial"));
            reader.Close();
        }

        [Test]
        public void StopsAfterBuildingTheSymbolTableIfErrorsFound()
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
            Assert.That(frontend.GetErrors().Last().Message, Is.StringContaining("Symbol 'ComputeFac' is already defined"));
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