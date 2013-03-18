using System.IO;
using System.Linq;
using MiniJavaCompiler.FrontEnd.LexicalAnalysis;
using MiniJavaCompiler.FrontEnd.SemanticAnalysis;
using MiniJavaCompiler.FrontEnd.SyntaxAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using MiniJavaCompiler.Support.SymbolTable.Types;
using NUnit.Framework;

namespace MiniJavaCompilerTest.FrontEndTest.SemanticAnalysis
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

            var symbolTableBuilder = new SymbolTableBuilder(syntaxTree, _errors);
            Assert.That(_errors.Errors, Is.Empty);

            try
            {
                _symbolTable = symbolTableBuilder.BuildSymbolTable();
                return _errors.Count == 0;
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
                             "   public static void main () {\n" +
                             "     System.out.println(42);\n" +
                             "  } \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "   public void foo() {\n" +
                             "     System.out.println(42);\n" +
                             "  } \n" +
                             "} \n\n";
            Assert.True(BuildSymbolTableFor(program));
            var fooClass = (TypeSymbol)_symbolTable.GlobalScope.ResolveType("Foo");
            var fooMethod = fooClass.Scope.ResolveMethod("foo");
            Assert.That(fooMethod.Type, Is.InstanceOf<VoidType>());
        }

        [Test]
        public void DoesNotAcceptVoidTypeForAVariable()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println(42);\n" +
                             "  } \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "   void foo; \n" +
                             "} \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Errors.First().Content, Is.StringContaining("Unknown type void"));
        }

        [Test]
        public void BuildsSymbolTableRightWithInheritance()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println(42);\n" +
                             "  } \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "   public int foo() {\n" +
                             "     System.out.println(42); \n" +
                             "     return 42; \n" +
                             "  } \n" +
                             "}\n" +
                             "class Bar extends Foo {\n" +
                             "   int foo; \n" +
                             "} \n";
            Assert.True(BuildSymbolTableFor(program));
            var fooClass = (TypeSymbol)_symbolTable.GlobalScope.ResolveType("Foo");
            var barClass = (TypeSymbol)_symbolTable.GlobalScope.ResolveType("Bar");
            Assert.That(barClass.SuperClass, Is.EqualTo(fooClass));
            Assert.That(barClass.Scope.ResolveMethod("foo"), Is.Not.Null);
        }

        [Test]
        public void ClassCannotInheritFromSelf()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println(42);\n" +
                             "  } \n" +
                             "} \n\n" +
                             "class Foo extends Foo {\n" +
                             "   public int foo() {\n" +
                             "     System.out.println(42); \n" +
                             "     return 42; \n" +
                             "  } \n" +
                             "}\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(1));
            Assert.That(_errors.Errors[0].Content, Is.StringContaining("cyclic inheritance involving Foo"));
        }

        [Test]
        public void DetectsCyclicInheritance()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println(42);\n" +
                             "  } \n" +
                             "} \n\n" +
                             "class Foo extends Baz {\n" +
                             "   public int foo() {\n" +
                             "     System.out.println(42); \n" +
                             "     return 42; \n" +
                             "  } \n" +
                             "}\n" +
                             "class Bar extends Foo {\n" +
                             "   int foo; \n" +
                             "} \n" +
                             "class Baz extends Bar {\n" +
                             "   int foo; \n" +
                             "} \n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(3));
            Assert.That(_errors.Errors[0].Content, Is.StringContaining("cyclic inheritance involving Foo"));
            Assert.That(_errors.Errors[1].Content, Is.StringContaining("cyclic inheritance involving Bar"));
            Assert.That(_errors.Errors[2].Content, Is.StringContaining("cyclic inheritance involving Baz"));
        }

        [Test]
        public void DetectsAnUnknownTypeInSuperClassDeclaration()
        {
            string program = "class Foo {\n" +
                 "   public static void main () {\n" +
                 "     System.out.println(42);\n" +
                 "  } \n" +
                 "} \n\n" +
                 "class Bar extends Baz { } \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(1));
            Assert.That(_errors.Errors.First().Content, Is.StringContaining("Unknown type Baz"));
        }

        [Test]
        public void DetectsAnUnknownTypeInVariableDeclaration()
        {
            string program = "class Foo {\n" +
                 "   public static void main () {\n" +
                 "     System.out.println(42);\n" +
                 "  } \n" +
                 "} \n\n" +
                 "class Bar { Baz foo; } \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(1));
            Assert.That(_errors.Errors.First().Content, Is.StringContaining("Unknown type Baz"));
        }

        [Test]
        public void DetectsAnUnknownTypeInMethodDeclaration()
        {
            string program = "class Foo {\n" +
                 "   public static void main () {\n" +
                 "     System.out.println(42);\n" +
                 "  } \n" +
                 "} \n\n" +
                 "class Bar { public Baz foo(Buzz foo) { } } \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.That(_errors.Count, Is.EqualTo(2));
            Assert.That(_errors.Errors[0].Content, Is.StringContaining("Unknown type Baz"));
            Assert.That(_errors.Errors[1].Content, Is.StringContaining("Unknown type Buzz"));
        }

        [Test]
        public void DefiningTheSameNameTwiceResultsInAnError()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println(42);\n" +
                             "  } \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "   int foo; \n" +
                             "   int foo; \n" +
                             "   public int foo() { } \n" +
                             "   public int foo() { } \n" +
                             "} \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.AreEqual(2, _errors.Errors.Count);
            foreach (var error in _errors.Errors)
            {
                Assert.That(error.Content, Is.StringContaining("Symbol foo is already defined"));
            }
        }

        [Test]
        public void NameCanBeRedefinedAfterScopeEnds()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     while (true) {\n" +
                             "       int foo;\n" +
                             "       foo = 0;\n" +
                             "     }\n" + // scope for foo ends
                             "     int foo;\n" +
                             "  } \n" +
                             "} \n\n";
            Assert.True(BuildSymbolTableFor(program));
        }

        [Test]
        public void CannotRedefineLocalNamesInsideTheSameScopeEvenInsideBlocks()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     int foo;\n" +
                             "     while (true) {\n" +
                             "       int foo;\n" + // the original foo is still in scope
                             "     }\n" +
                             "  } \n" +
                             "} \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.AreEqual(1, _errors.Errors.Count);
            Assert.That(_errors.Errors[0].Content, Is.StringContaining("Symbol foo is already defined"));
        }

        [Test]
        public void CanRedefineFieldsInLocalScope()
        {
            string program = "class Factorial {\n" +
                             "  public static void main () { } \n" +
                             "} \n" +
                             "class Foo {" +
                             "  int foo;" +
                             "  public int bar() {" +
                             "    int foo;" +
                             "  }" +
                             "}";
            Assert.True(BuildSymbolTableFor(program));
        }

        [Test]
        public void RecoversFromMethodAndVariableDefinitionFailure()
        {
            string program = "class Factorial {\n" +
                             "   public static void main () {\n" +
                             "     System.out.println(42);\n" +
                             "  } \n" +
                             "} \n\n" +
                             "class Foo {\n" +
                             "   int foo; \n" +
                             "   int foo; \n" + // first error
                             "   public int foo() { } \n" +
                             "   public int foo() { int foo; int bar; int bar; } \n" + // second error (method name) and third error
                                                                                       // (two variable declarations with the same name inside method)
                             "   public int foo() { } \n" + // fourth error
                             "   int foo; \n" + // fifth error
                             "} \n\n";
            Assert.False(BuildSymbolTableFor(program));
            Assert.AreEqual(5, _errors.Errors.Count);
            Assert.AreEqual(4, _errors.Errors.Count(err => err.ToString().Contains("Symbol foo is already defined")));
            Assert.AreEqual(1, _errors.Errors.Count(err => err.ToString().Contains("Symbol bar is already defined")));
        }

        [Test]
        public void BuildsSymbolTableRightForSampleProgram()
        {
            // Note: The sample program from the site uses a unary minus that
            // is not defined by the CFG. I have therefore removed it from
            // this code.
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
            Assert.True(BuildSymbolTableFor(program));

            var firstClass = _symbolTable.GlobalScope.ResolveType("Factorial");
            Assert.That(firstClass, Is.InstanceOf<TypeSymbol>());

            Assert.That(firstClass.Scope.ResolveMethod("main"), Is.Not.Null);

            var secondClass = _symbolTable.GlobalScope.ResolveType("Fac");
            Assert.That(secondClass, Is.Not.Null);

            var FacClassScope = (TypeSymbol) secondClass;
            var facMethod = FacClassScope.Scope.ResolveMethod("ComputeFac");
            Assert.That(facMethod, Is.Not.Null);
            Assert.That(facMethod.Type, Is.InstanceOf<ScalarType>());
            Assert.That(facMethod.Type.Name, Is.EqualTo("int"));

            var numVariable = facMethod.Scope.ResolveVariable("num");
            Assert.That(numVariable, Is.Not.Null);
            var numVariableNode = _symbolTable.Declarations[numVariable];
            Assert.That(_symbolTable.Scopes[numVariableNode], Is.EqualTo(facMethod.Scope));
            Assert.That(numVariable.Type, Is.InstanceOf<ScalarType>());
            Assert.That(numVariable.Type.Name, Is.EqualTo("int"));
            Assert.AreEqual(numVariable.Type, facMethod.Type);

            var numAuxVariable = facMethod.Scope.ResolveVariable("num_aux");
            Assert.That(numAuxVariable, Is.Not.Null);
            Assert.That(numAuxVariable.Type, Is.InstanceOf<ScalarType>());
            Assert.That(numVariable.Type.Name, Is.EqualTo("int"));
        }
    }
}
