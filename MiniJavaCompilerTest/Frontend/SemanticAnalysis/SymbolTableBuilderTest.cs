using System.IO;
using System.Linq;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Frontend.SemanticAnalysis;
using MiniJavaCompiler.Frontend.SyntaxAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using NUnit.Framework;

namespace MiniJavaCompilerTest.Frontend.SemanticAnalysis
{
    [TestFixture]
    class SymbolTableBuilderTest
    {
        private IErrorReporter _errors;
        private SymbolTable _symbolTable;

        private bool BuildSymbolTableFor(string program)
        {
            var reader = new StringReader(program);
            var scanner = new MiniJavaScanner(reader);
            _errors = new ErrorLogger();
            var parser = new Parser(scanner, _errors, true);
            Program syntaxTree = parser.Parse();
            reader.Close();
            Assert.That(_errors.Errors, Is.Empty);

            var types = new TypeSetBuilder(syntaxTree, _errors).BuildTypeSet();
            var symbolTableBuilder = new SymbolTableBuilder(syntaxTree, types, _errors);
            Assert.That(_errors.Errors, Is.Empty);

            try
            {
                _symbolTable = symbolTableBuilder.BuildSymbolTable();
                return true;
            }
            catch (CompilationError)
            {
                return false;
            }
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
            var fooClass = (UserDefinedTypeSymbol)_symbolTable.GlobalScope.ResolveType("Foo");
            var fooMethod = fooClass.Scope.ResolveMethod("foo");
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
            Assert.That(_errors.Errors.First().Content, Is.StringContaining("Unknown type 'void'"));
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
            var fooClass = (UserDefinedTypeSymbol)_symbolTable.GlobalScope.ResolveType("Foo");
            var barClass = (UserDefinedTypeSymbol)_symbolTable.GlobalScope.ResolveType("Bar");
            Assert.That(barClass.SuperClass, Is.EqualTo(fooClass));
            Assert.That(barClass.Scope.ResolveMethod("foo"), Is.Not.Null);
        }

        [Test]
        public void ClassCannotInheritFromSelf()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t} \n" +
                             "} \n\n" +
                             "class Foo extends Foo {\n" +
                             "\t public int foo() {\n" +
                             "\t\t System.out.println(42); \n" +
                             "\t\t return 42; \n" +
                             "\t} \n" +
                             "}\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(1));
            Assert.That(_errors.Errors[0].Content, Is.StringContaining("depends on itself"));
        }

        [Test]
        public void DetectsCyclicInheritance()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n" +
                             "\t\t System.out.println(42);\n" +
                             "\t} \n" +
                             "} \n\n" +
                             "class Foo extends Baz {\n" +
                             "\t public int foo() {\n" +
                             "\t\t System.out.println(42); \n" +
                             "\t\t return 42; \n" +
                             "\t} \n" +
                             "}\n" +
                             "class Bar extends Foo {\n" +
                             "\t int foo; \n" +
                             "} \n" +
                             "class Baz extends Bar {\n" +
                             "\t int foo; \n" +
                             "} \n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(3));
            Assert.That(_errors.Errors[0].Content, Is.StringContaining("Class Foo depends on itself"));
            Assert.That(_errors.Errors[1].Content, Is.StringContaining("Class Bar depends on itself"));
            Assert.That(_errors.Errors[2].Content, Is.StringContaining("Class Baz depends on itself"));
        }

        [Test]
        public void DetectsAnUnknownTypeInSuperClassDeclaration()
        {
            string program = "class Foo {\n" +
                 "\t public static void main () {\n" +
                 "\t\t System.out.println(42);\n" +
                 "\t} \n" +
                 "} \n\n" +
                 "class Bar extends Baz { } \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(1));
            Assert.That(_errors.Errors.First().Content, Is.StringContaining("Unknown type 'Baz'"));
        }

        [Test]
        public void DetectsAnUnknownTypeInVariableDeclaration()
        {
            string program = "class Foo {\n" +
                 "\t public static void main () {\n" +
                 "\t\t System.out.println(42);\n" +
                 "\t} \n" +
                 "} \n\n" +
                 "class Bar { Baz foo; } \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(1));
            Assert.That(_errors.Errors.First().Content, Is.StringContaining("Unknown type 'Baz'"));
        }

        [Test]
        public void DetectsAnUnknownTypeInMethodDeclaration()
        {
            string program = "class Foo {\n" +
                 "\t public static void main () {\n" +
                 "\t\t System.out.println(42);\n" +
                 "\t} \n" +
                 "} \n\n" +
                 "class Bar { public Baz foo(Buzz foo) { } } \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(2));
            Assert.That(_errors.Errors[0].Content, Is.StringContaining("Unknown type 'Baz'"));
            Assert.That(_errors.Errors[1].Content, Is.StringContaining("Unknown type 'Buzz'"));
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
            Assert.AreEqual(2, _errors.Errors.Count);
            foreach (var error in _errors.Errors)
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
            Assert.AreEqual(5, _errors.Errors.Count);
            Assert.AreEqual(4, _errors.Errors.Count(err => err.ToString().Contains("Symbol 'foo' is already defined")));
            Assert.AreEqual(1, _errors.Errors.Count(err => err.ToString().Contains("Symbol 'bar' is already defined")));
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
                             "\t\t assert (num > 0 || num == 0);\n" +
                             "\t\t int num_aux;\n" +
                             "\t\t if (num == 0)\n" +
                             "\t\t\t num_aux = 1;\n" +
                             "\t\t else \n" +
                             "\t\t\t num_aux = num * this.ComputeFac (num-1);\n" +
                             "\t\t return num_aux;\n" +
                             "\t }\n" +
                             "}\n";
            Assert.True(BuildSymbolTableFor(program));

            var firstClass = _symbolTable.GlobalScope.ResolveType("Factorial");
            Assert.That(firstClass, Is.InstanceOf<UserDefinedTypeSymbol>());

            Assert.That(firstClass.Scope.ResolveMethod("main"), Is.Not.Null);

            var secondClass = _symbolTable.GlobalScope.ResolveType("Fac");
            Assert.That(secondClass, Is.Not.Null);

            var FacClassScope = (UserDefinedTypeSymbol) secondClass;
            var facMethod = FacClassScope.Scope.ResolveMethod("ComputeFac");
            Assert.That(facMethod, Is.Not.Null);
            Assert.That(facMethod.Type, Is.InstanceOf<BuiltInTypeSymbol>());
            Assert.That(facMethod.Type.Name, Is.EqualTo("int"));

            var numVariable = facMethod.Scope.ResolveVariable("num");
            Assert.That(numVariable, Is.Not.Null);
            var numVariableNode = _symbolTable.Definitions[numVariable];
            Assert.That(_symbolTable.Scopes[numVariableNode], Is.EqualTo(facMethod.Scope));
            Assert.That(numVariable.Type, Is.InstanceOf<BuiltInTypeSymbol>());
            Assert.That(numVariable.Type.Name, Is.EqualTo("int"));
            Assert.AreEqual(numVariable.Type, facMethod.Type);

            var numAuxVariable = facMethod.Scope.ResolveVariable("num_aux");
            Assert.That(numAuxVariable, Is.Not.Null);
            Assert.That(numAuxVariable.Type, Is.InstanceOf<BuiltInTypeSymbol>());
            Assert.That(numVariable.Type.Name, Is.EqualTo("int"));
        }
    }
}
