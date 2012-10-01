using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.SyntaxAnalysis;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.SemanticAnalysis;

namespace MiniJavaCompilerTest
{
    [TestFixture]
    class SymbolTableBuilderTest
    {
        private IErrorReporter errors;
        private Program syntaxTree;

        private SymbolTable BuildSymbolTableFor(string program)
        {
            var scanner = new MiniJavaScanner(new StringReader(program));
            errors = new ErrorLogger();
            var parserInputReader = new ParserInputReader(scanner, errors);
            var parser = new Parser(parserInputReader, errors);
            syntaxTree = parser.Parse();

            var types = new TypeSetBuilder(syntaxTree, errors).BuildTypeSet();
            var symbolTableBuilder = new SymbolTableBuilder(syntaxTree, types, errors);
            Assert.That(errors.Errors(), Is.Empty);
            return symbolTableBuilder.BuildSymbolTable();
        }

        [Test]
        [Ignore("TODO: complete test")]
        public void TestMainClass()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t\t assert (10 > 5);" +
                             "} \n\n";
            var symbolTable = BuildSymbolTableFor(program);
        }

        [Test]
        [Ignore("TODO: complete test")]
        public void TestTwoClasses()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "}";
            var symbolTable = BuildSymbolTableFor(program);
        }

        [Test]
        [Ignore("TODO: complete test")]
        public void TestAssignment()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t int foo;\n" +
                             "\t\t foo = 42;\n" +
                             "\t\t System.out.println(foo);\n" +
                             "} \n\n";
            var symbolTable = BuildSymbolTableFor(program);
        }

        [Test]
        [Ignore("TODO: complete test")]
        public void TestEquals()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t int foo;\n" +
                             "\t\t foo = 42;\n" +
                             "\t\t if (foo == 42)" +
                             "\t\t System.out.println(foo);\n" +
                             "} \n\n";
            var symbolTable = BuildSymbolTableFor(program);
        }

        [Test]
        [Ignore("TODO: complete test")]
        public void TestAssignmentPrecedence()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t int foo;\n" +
                             "\t\t\t foo = bar * this.ComputeFac (num-1);\n" +
                             "\t\t System.out.println(foo);\n" +
                             "} \n\n";
            var symbolTable = BuildSymbolTableFor(program);
        }

        [Test]
        public void BuildsSymbolTableRightForSampleProgram()
        {
            // Note: The sample program from the site uses a unary minus that
            // is not defined by the CFG. I have therefore removed it from
            // this code.
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println (new Fac ().ComputeFac (10));\n" +
                             "} \n\n" +
                             "} \n" +
                             "class Fac { \n" +
                             "\t public int ComputeFac (int num) {\n" +
                             "\t\t assert (num > 1);\n" +
                             "\t\t int num_aux;\n" +
                             "\t\t if (num == 0)\n" +
                             "\t\t\t num_aux = 1;\n" +
                             "\t\t else \n" +
                             "\t\t\t num_aux = num * this.ComputeFac (num-1);\n" +
                             "\t\t return num_aux;\n" +
                             "\t }\n" +
                             "}\n";
            var symbolTable = BuildSymbolTableFor(program);

            var firstClass = symbolTable.GlobalScope.Resolve<TypeSymbol>("Factorial");
            Assert.That(firstClass, Is.InstanceOf<UserDefinedTypeSymbol>());

            // Main method is not in the symbol table.
            Assert.That(((UserDefinedTypeSymbol)firstClass).Resolve<MethodSymbol>("main"), Is.Null);

            var secondClass = symbolTable.GlobalScope.Resolve<TypeSymbol>("Fac");
            Assert.That(secondClass, Is.Not.Null);

            var FacClassScope = (UserDefinedTypeSymbol) secondClass;
            var facMethod = (MethodSymbol)FacClassScope.Resolve<MethodSymbol>("ComputeFac");
            Assert.That(facMethod, Is.Not.Null);
            Assert.That(facMethod.Type, Is.InstanceOf<BuiltinTypeSymbol>());

            var numVariable = (VariableSymbol)facMethod.Resolve<VariableSymbol>("num");
            Assert.That(numVariable, Is.Not.Null);
            Assert.That(numVariable.Type, Is.InstanceOf<BuiltinTypeSymbol>());

            var numAuxVariable = (VariableSymbol) facMethod.Resolve<VariableSymbol>("num_aux");
            Assert.That(numAuxVariable, Is.Not.Null);
            Assert.That(numAuxVariable.Type, Is.InstanceOf<BuiltinTypeSymbol>());
        }
    }
}
