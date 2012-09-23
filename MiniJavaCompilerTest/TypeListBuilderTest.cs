using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.SemanticAnalysis;

namespace MiniJavaCompilerTest
{
    [TestFixture]
    public class TypeListBuilderTest
    {
        [Test]
        public void ShouldConflictWithPredefinedTypesTest()
        {
            var mainClass = new MainClassDeclaration("int", new List<IStatement>(), 1, 0);
            var secondClass = new ClassDeclaration("boolean", null, new List<Declaration>(), 2, 0);
            var thirdClass = new ClassDeclaration("int", null, new List<Declaration>(), 3, 0);
            var program = new Program(mainClass, new List<ClassDeclaration> (
                new ClassDeclaration[] { secondClass, thirdClass }));

            var builder = new TypeSetBuilder(program);
            var error = Assert.Catch<ErrorReport>(() => builder.BuildTypeSet());
            Assert.AreEqual(error.ErrorMsgs.Count, 3);
            Assert.AreEqual(error.ErrorMsgs[0].Content, "Conflicting definitions for int.");
            Assert.AreEqual(error.ErrorMsgs[0].Row, 1);
            Assert.AreEqual(error.ErrorMsgs[1].Content, "Conflicting definitions for boolean.");
            Assert.AreEqual(error.ErrorMsgs[1].Row, 2);
            Assert.AreEqual(error.ErrorMsgs[2].Content, "Conflicting definitions for int.");
            Assert.AreEqual(error.ErrorMsgs[2].Row, 3);
        }

        [Test]
        public void ShouldConflictWithMainClassTest()
        {
            var mainClass = new MainClassDeclaration("Foo", new List<IStatement>(), 1, 0);
            var otherClass = new ClassDeclaration("Foo", null, new List<Declaration>(), 2, 0);
            var program = new Program(mainClass, new List<ClassDeclaration>(
                new ClassDeclaration[] { otherClass }));

            var builder = new TypeSetBuilder(program);
            var error = Assert.Catch<ErrorReport>(() => builder.BuildTypeSet());
            Assert.AreEqual(error.ErrorMsgs.Count, 1);
            Assert.AreEqual(error.ErrorMsgs[0].Content, "Conflicting definitions for Foo.");
            Assert.AreEqual(error.ErrorMsgs[0].Row, 2);
        }

        [Test]
        public void ShouldConflictWithSelfDefinedTypesTest()
        {
            var mainClass = new MainClassDeclaration("Foo", new List<IStatement>(), 1, 0);
            var secondClass = new ClassDeclaration("Bar", null, new List<Declaration>(), 2, 0);
            var thirdClass = new ClassDeclaration("Bar", null, new List<Declaration>(), 3, 0);
            var program = new Program(mainClass, new List<ClassDeclaration>(
                new ClassDeclaration[] { secondClass, thirdClass }));

            var builder = new TypeSetBuilder(program);
            var error = Assert.Catch<ErrorReport>(() => builder.BuildTypeSet());
            Assert.AreEqual(error.ErrorMsgs.Count, 1);
            Assert.AreEqual(error.ErrorMsgs[0].Content, "Conflicting definitions for Bar.");
            Assert.AreEqual(error.ErrorMsgs[0].Row, 3);
        }

        [Test]
        public void ValidTypeDefinitionsTest()
        {
            var mainClass = new MainClassDeclaration("Foo", new List<IStatement>(), 1, 0);
            var secondClass = new ClassDeclaration("Bar", null, new List<Declaration>(), 2, 0);
            var thirdClass = new ClassDeclaration("Baz", null, new List<Declaration>(), 3, 0);
            var program = new Program(mainClass, new List<ClassDeclaration>(
                new ClassDeclaration[] { secondClass, thirdClass }));

            var builder = new TypeSetBuilder(program);
            var types = builder.BuildTypeSet().ToList();
            Assert.Contains("Foo", types);
            Assert.Contains("Bar", types);
            Assert.Contains("Baz", types);
            Assert.Contains("int", types);
            Assert.Contains("boolean", types);
            Assert.That(types.Count, Is.EqualTo(5));
        }
    }
}
