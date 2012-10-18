using System.IO;
using System.Linq;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.SemanticAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.SyntaxAnalysis;
using NUnit.Framework;

namespace MiniJavaCompilerTest.Frontend.SemanticAnalysis
{
    [TestFixture]
    class SymbolTableBuilderTest
    {
        private IErrorReporter errors;
        private SymbolTable symbolTable;

        private bool BuildSymbolTableFor(string program)
        {
            var scanner = new MiniJavaScanner(new StringReader(program));
            errors = new ErrorLogger();
            var parserInputReader = new ParserInputReader(scanner, errors);
            var parser = new Parser(parserInputReader, errors);
            Program syntaxTree = parser.Parse();
            Assert.That(errors.Errors, Is.Empty);

            var types = new TypeSetBuilder(syntaxTree, errors).BuildTypeSet();
            var symbolTableBuilder = new SymbolTableBuilder(syntaxTree, types, errors);
            Assert.That(errors.Errors, Is.Empty);

            return symbolTableBuilder.BuildSymbolTable(out symbolTable);
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
            Assert.True(BuildSymbolTableFor(program));
            var fooClass = (UserDefinedTypeSymbol)symbolTable.GlobalScope.Resolve<SimpleTypeSymbol>("Foo");
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
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(errors.Errors.First().Content, Is.StringContaining("Unknown type 'void'"));
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
            Assert.True(BuildSymbolTableFor(program));
            var fooClass = (UserDefinedTypeSymbol)symbolTable.GlobalScope.Resolve<SimpleTypeSymbol>("Foo");
            var barClass = (UserDefinedTypeSymbol)symbolTable.GlobalScope.Resolve<SimpleTypeSymbol>("Bar");
            Assert.That(barClass.SuperClass, Is.EqualTo(fooClass));
            Assert.That(barClass.Resolve<MethodSymbol>("foo"), Is.Not.Null);
        }

        [Test]
        public void DefiningTheSameNameTwiceResultsInAnError()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t} \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "\t int foo; \n" +
                             "\t int foo; \n" +
                             "\t public int foo() { } \n" +
                             "\t public int foo() { } \n" +
                             "} \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.AreEqual(2, errors.Errors.Count);
            foreach (var error in errors.Errors)
            {
                Assert.That(error.Content, Is.StringContaining("Symbol 'foo' is already defined"));
            }
        }

        [Test]
        public void RecoversFromMethodAndVariableDefinitionFailure()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t} \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "\t int foo; \n" +
                             "\t int foo; \n" + // first error
                             "\t public int foo() { } \n" +
                             "\t public int foo() { int foo; int bar; int bar; } \n" + // second error (method name) and third error
                                                                                       // (two variable declarations with the same name inside method)
                             "\t public int foo() { } \n" + // fourth error
                             "\t int foo; \n" + // fifth error
                             "} \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.AreEqual(5, errors.Errors.Count);
            Assert.AreEqual(4, errors.Errors.Count(err => err.Message.Contains("Symbol 'foo' is already defined")));
            Assert.AreEqual(1, errors.Errors.Count(err => err.Message.Contains("Symbol 'bar' is already defined")));
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
            Assert.True(BuildSymbolTableFor(program));

            var firstClass = symbolTable.GlobalScope.Resolve<SimpleTypeSymbol>("Factorial");
            Assert.That(firstClass, Is.InstanceOf<UserDefinedTypeSymbol>());

            Assert.That(((UserDefinedTypeSymbol)firstClass).Resolve<MethodSymbol>("main"), Is.Not.Null);

            var secondClass = symbolTable.GlobalScope.Resolve<SimpleTypeSymbol>("Fac");
            Assert.That(secondClass, Is.Not.Null);

            var FacClassScope = (UserDefinedTypeSymbol) secondClass;
            var facMethod = (MethodSymbol)FacClassScope.Resolve<MethodSymbol>("ComputeFac");
            Assert.That(facMethod, Is.Not.Null);
            Assert.That(facMethod.Type, Is.InstanceOf<BuiltinTypeSymbol>());
            Assert.That(facMethod.Type.Name, Is.EqualTo("int"));

            var numVariable = (VariableSymbol)facMethod.Resolve<VariableSymbol>("num");
            Assert.That(numVariable, Is.Not.Null);
            var numVariableNode = symbolTable.Definitions[numVariable];
            Assert.That(symbolTable.Scopes[numVariableNode], Is.EqualTo(facMethod));
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
