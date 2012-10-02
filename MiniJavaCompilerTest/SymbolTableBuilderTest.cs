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
        public void HandlesVoidTypeRight()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t} \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "\t public void foo() {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t} \n" +
                             "} \n\n";
            var symbolTable = BuildSymbolTableFor(program);
            var fooClass = (UserDefinedTypeSymbol)symbolTable.GlobalScope.Resolve<TypeSymbol>("Foo");
            var fooMethod = fooClass.Resolve<MethodSymbol>("foo");
            Assert.That(fooMethod.Type, Is.InstanceOf<VoidType>());
        }

        [Test]
        public void DoesNotAcceptVoidTypeForAVariable()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t} \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "\t void foo; \n" +
                             "} \n\n";
            Assert.Throws<Exception>(() => BuildSymbolTableFor(program));
        }

        [Test]
        public void BuildsSymbolTableRightWithInheritance()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t} \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "\t public int foo() {\n" +
                             "\t\t System.out.println(42); \n" +
                             "\t\t return 42; \n" +
                             "\t} \n" +
                             "}\n" +
                             "class Bar extends Foo {\n" +
                             "\t int foo; \n" +
                             "} \n";
            var symbolTable = BuildSymbolTableFor(program);
            var fooClass = (UserDefinedTypeSymbol)symbolTable.GlobalScope.Resolve<TypeSymbol>("Foo");
            var barClass = (UserDefinedTypeSymbol)symbolTable.GlobalScope.Resolve<TypeSymbol>("Bar");
            Assert.That(barClass.SuperClass, Is.EqualTo(fooClass));
            Assert.That(barClass.Resolve<MethodSymbol>("foo"), Is.Not.Null);
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
                             "\t\t assert (num > 0 - 1);\n" +
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
            Assert.That(facMethod.Type.Name, Is.EqualTo("int"));

            var numVariable = (VariableSymbol)facMethod.Resolve<VariableSymbol>("num");
            Assert.That(numVariable, Is.Not.Null);
            Assert.That(numVariable.Type, Is.InstanceOf<BuiltinTypeSymbol>());
            Assert.That(numVariable.Type.Name, Is.EqualTo("int"));
            Assert.AreEqual(numVariable.Type, facMethod.Type);

            var numAuxVariable = (VariableSymbol) facMethod.Resolve<VariableSymbol>("num_aux");
            Assert.That(numAuxVariable, Is.Not.Null);
            Assert.That(numAuxVariable.Type, Is.InstanceOf<BuiltinTypeSymbol>());
            Assert.That(numVariable.Type.Name, Is.EqualTo("int"));
        }
    }
}
