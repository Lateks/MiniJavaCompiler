using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.SyntaxAnalysis;
using NUnit.Framework;
using MiniJavaCompiler.SemanticAnalysis;

namespace MiniJavaCompilerTest.Frontend
{
    [TestFixture]
    class ReferenceCheckTest
    {
        public TypeChecker SetUpReferenceChecker(string program)
        {
            var scanner = new MiniJavaScanner(new StringReader(program));
            var errors = new ErrorLogger();
            var parserInputReader = new ParserInputReader(scanner, errors);
            var parser = new Parser(parserInputReader, errors, true);
            Program syntaxTree = parser.Parse();
            Assert.That(errors.Errors(), Is.Empty);

            var types = new TypeSetBuilder(syntaxTree, errors).BuildTypeSet();
            var symbolTableBuilder = new SymbolTableBuilder(syntaxTree, types, errors);
            Assert.That(errors.Errors(), Is.Empty);

            SymbolTable symbolTable;
            Assert.True(symbolTableBuilder.BuildSymbolTable(out symbolTable));

            return new TypeChecker(syntaxTree, symbolTable);
        }

        [Test]
        public void UndefinedVariableReferenceCausesReferenceError()
        {
            string program = "class Foo {\n" +
                             "\tpublic static void main() {\n" +
                             "\t\tSystem.out.println(foo);\n" +
                             "\t}\n" +
                             "}\n";
            var checker = SetUpReferenceChecker(program);
            var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
            Assert.That(exception.Message, Is.StringContaining("foo"));
        }

        [Test]
        public void CannotCallMethodForABuiltinType()
        {
            string program = "class Foo {\n" +
                             "\tpublic static void main() {\n" +
                             "\t\tint foo;\n" +
                             "\t\tfoo = 42;\n" +
                             "\t\tSystem.out.println(foo.bar());\n" +
                             "\t}\n" +
                             "}\n";
            var checker = SetUpReferenceChecker(program);
            var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
            Assert.That(exception.Message, Is.StringContaining("builtin"));
        }

        [Test]
        public void CannotCallMethodOtherThanLengthForArray()
        {
            string program = "class Foo {\n" +
                             "\tpublic static void main() {\n" +
                             "\t\tint[] foo;\n" +
                             "\t\tfoo = new int[10];\n" +
                             "\t\tSystem.out.println(foo.bar());\n" +
                             "\t}\n" +
                             "}\n";
            var checker = SetUpReferenceChecker(program);
            var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
            Assert.That(exception.Message, Is.StringContaining("array"));
        }

        [Test]
        public void CanCallLengthMethodForArray()
        {
            string program = "class Foo {\n" +
                             "\tpublic static void main() {\n" +
                             "\t\tint[] foo;\n" +
                             "\t\tfoo = new int[10];\n" +
                             "\t\tSystem.out.println(foo.length);\n" +
                             "\t}\n" +
                             "}\n";
            var checker = SetUpReferenceChecker(program);
            Assert.DoesNotThrow(checker.CheckTypesAndReferences);
        }

        [Test]
        public void CanCallResolvableMethod()
        {
            string program = "class Foo {\n" +
                             "\tpublic static void main() {\n" +
                             "\t}\n" +
                             "}\n\n" +
                             "class A {\n" +
                             "\tpublic boolean foo()" +
                             "\t{\n" +
                             "\t\treturn true;" +
                             "\t}\n" +
                             "}\n" +
                             "class B extends A {\n" +
                             "\tpublic boolean bar() {\n" +
                             "\t\treturn this.foo();\n" +
                             "\t}\n" +
                             "}\n";
            var checker = SetUpReferenceChecker(program);
            Assert.DoesNotThrow(checker.CheckTypesAndReferences);
        }

        [Test]
        public void CannotCallUndefinedMethod()
        {
            string program = "class Foo {\n" +
                             "\tpublic static void main() {\n" +
                             "\t}\n" +
                             "}\n\n" +
                             "class A {\n" +
                             "\tpublic boolean foo()" +
                             "\t{\n" +
                             "\t\treturn this.bar();" +
                             "\t}\n" +
                             "}\n";
            var checker = SetUpReferenceChecker(program);
            var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
            Assert.That(exception.Message, Is.StringContaining("bar"));
        }

        [Test]
        public void CanReferenceVariableFromEnclosingClassScope()
        {
            string program = "class Foo {\n" +
                             "\tpublic static void main() {\n" +
                             "\t}\n" +
                             "}\n\n" +
                             "class A {\n" +
                             "\tint foo;" +
                             "\tpublic int foo()" +
                             "\t{\n" +
                             "\t\tfoo = 42;" +
                             "\t\treturn foo;" +
                             "\t}\n" +
                             "}\n";
            var checker = SetUpReferenceChecker(program);
            Assert.DoesNotThrow(checker.CheckTypesAndReferences);
        }

        [Test]
        public void VariableMustBeDeclaredBeforeReference()
        {
            string program = "class Foo {\n" +
                             "\tpublic static void main() {\n" +
                             "\t}\n" +
                             "}\n\n" +
                             "class A {\n" +
                             "\tpublic boolean foo()" +
                             "\t{\n" +
                             "\t\tif (42 == 42)\n" +
                             "\t\t\treturn foo;\n" +
                             "\t\telse\n" +
                             "\t\t\treturn false;\n" +
                             "\t\tboolean foo;\n" +
                             "\t}\n" +
                             "}\n";
            var checker = SetUpReferenceChecker(program);
            var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
            Assert.That(exception.Message, Is.StringContaining("foo"));
        }

        [Test]
        public void IfBlockHasItsOwnScope()
        {
            string program = "class Factorial {\n" +
                 "\t public static void main () {\n" +
                 "\t\t if (true)\n" +
                 "\t\t\t int foo;" +
                 "\t\t foo = 42;\n" +
                 "\t} \n" +
                 "} \n\n";
            var checker = SetUpReferenceChecker(program);
            var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
            Assert.That(exception.Message, Is.StringContaining("Could not resolve symbol foo"));
        }

        [Test]
        public void IfAndElseBlocksAreInSeparateScopes()
        {
            string program = "class Factorial {\n" +
                 "\t public static void main () {\n" +
                 "\t\t if (true)\n" +
                 "\t\t\t int foo;" +
                 "\t\t else \n" +
                 "\t\t\t foo = 42;\n" +
                 "\t} \n" +
                 "} \n\n";
            var checker = SetUpReferenceChecker(program);
            var exception = Assert.Throws<ReferenceError>(checker.CheckTypesAndReferences);
            Assert.That(exception.Message, Is.StringContaining("Could not resolve symbol foo"));
        }

        [Test]
        [Ignore("TODO")]
        public void CannotCallAStaticMethodForAnInstance()
        {
        }

        [Test]
        [Ignore("TODO")]
        public void CanCallMainMethodFromInsideProgram() // makes no sense but should still be possible
        {
        }
    }

    [TestFixture]
    class TypeCheckTest
    {
    }
}
