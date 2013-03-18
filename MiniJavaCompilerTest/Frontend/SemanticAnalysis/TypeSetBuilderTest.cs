using System.Collections.Generic;
using System.Linq;
using MiniJavaCompiler.FrontEnd.SemanticAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using NUnit.Framework;
using MiniJavaCompiler.Support.SymbolTable.Scopes;

namespace MiniJavaCompilerTest.FrontEndTest.SemanticAnalysis
{
    [TestFixture]
    public class TypeSetBuildingTest
    {
        [Test]
        public void ShouldConflictWithPredefinedTypesTest()
        {
            var mainMethod = MethodDeclaration.CreateMainMethodDeclaration(new List<IStatement>(), 0, 0);
            var mainClass = ClassDeclaration.CreateMainClassDeclaration("int", mainMethod, 1, 0);
            var secondClass = new ClassDeclaration("boolean", null, new List<Declaration>(), 2, 0);
            var thirdClass = new ClassDeclaration("int", null, new List<Declaration>(), 3, 0);
            var program = new Program(mainClass, new List<ClassDeclaration> (
                new [] { secondClass, thirdClass }));

            var errorReporter = new ErrorLogger();
            var builder = new SymbolTableBuilder(program, errorReporter);
            Assert.Throws<CompilationError>(() => builder.BuildSymbolTable());
            var errors = errorReporter.Errors;
            Assert.AreEqual(3, errors.Count);
            Assert.AreEqual("Conflicting definitions for int.", errors[0].Content);
            Assert.AreEqual(1, errors[0].Row);
            Assert.AreEqual("Conflicting definitions for boolean.", errors[1].Content);
            Assert.AreEqual(2, errors[1].Row);
            Assert.AreEqual("Conflicting definitions for int.", errors[2].Content);
            Assert.AreEqual(3, errors[2].Row);
        }

        [Test]
        public void ShouldConflictWithMainClassTest()
        {
            var mainMethod = MethodDeclaration.CreateMainMethodDeclaration(new List<IStatement>(), 0, 0);
            var mainClass = ClassDeclaration.CreateMainClassDeclaration("Foo", mainMethod, 0, 0);
            var otherClass = new ClassDeclaration("Foo", null, new List<Declaration>(), 2, 0);
            var program = new Program(mainClass, new List<ClassDeclaration>(
                new ClassDeclaration[] { otherClass }));

            var errorReporter = new ErrorLogger();
            var builder = new SymbolTableBuilder(program, errorReporter);
            Assert.Throws<CompilationError>(() => builder.BuildSymbolTable());
            var errors = errorReporter.Errors;
            Assert.AreEqual(errors.Count, 1);
            Assert.AreEqual("Conflicting definitions for Foo.", errors[0].Content);
            Assert.AreEqual(errors[0].Row, 2);
        }

        [Test]
        public void ShouldConflictWithSelfDefinedTypesTest()
        {
            var mainMethod = MethodDeclaration.CreateMainMethodDeclaration(new List<IStatement>(), 0, 0);
            var mainClass = ClassDeclaration.CreateMainClassDeclaration("Foo", mainMethod, 0, 0);
            var secondClass = new ClassDeclaration("Bar", null, new List<Declaration>(), 2, 0);
            var thirdClass = new ClassDeclaration("Bar", null, new List<Declaration>(), 3, 0);
            var program = new Program(mainClass, new List<ClassDeclaration>(
                new ClassDeclaration[] { secondClass, thirdClass }));

            var errorReporter = new ErrorLogger();
            var builder = new SymbolTableBuilder(program, errorReporter);
            Assert.Throws<CompilationError>(() => builder.BuildSymbolTable());
            var errors = errorReporter.Errors;
            Assert.AreEqual(errors.Count, 1);
            Assert.AreEqual("Conflicting definitions for Bar.", errors[0].Content);
            Assert.AreEqual(errors[0].Row, 3);
        }

        [Test]
        public void ValidTypeDefinitionsTest()
        {
            var mainMethod = MethodDeclaration.CreateMainMethodDeclaration(new List<IStatement>(), 0, 0);
            var mainClass = ClassDeclaration.CreateMainClassDeclaration("Foo", mainMethod, 0, 0);
            var secondClass = new ClassDeclaration("Bar", null, new List<Declaration>(), 2, 0);
            var thirdClass = new ClassDeclaration("Baz", null, new List<Declaration>(), 3, 0);
            var program = new Program(mainClass, new List<ClassDeclaration>(
                new [] { secondClass, thirdClass }));

            var errorReporter = new ErrorLogger();
            var builder = new SymbolTableBuilder(program, errorReporter);
            builder.BuildSymbolTable();
            var types = (program.Scope as GlobalScope).UserDefinedTypeNames.ToList();
            Assert.Contains("Foo", types);
            Assert.Contains("Bar", types);
            Assert.Contains("Baz", types);
            Assert.That(types, Is.Not.Contains("int"));
            Assert.That(types, Is.Not.Contains("boolean"));
            Assert.That(types.Count, Is.EqualTo(3));
        }
    }
}
