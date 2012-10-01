using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.SemanticAnalysis;

namespace MiniJavaCompilerTest
{
    [TestFixture]
    class ClassScopeTest
    {
        private GlobalScope globalScope;
        private UserDefinedTypeSymbol testClass;
        private UserDefinedTypeSymbol superClass;
        private UserDefinedTypeSymbol superSuperClass;
        private IType someType;

        [SetUp]
        public void SetUp()
        {
            globalScope = new GlobalScope();
            superSuperClass = (UserDefinedTypeSymbol) Symbol.CreateAndDefine<UserDefinedTypeSymbol>("Foo", globalScope);
            superClass = (UserDefinedTypeSymbol) Symbol.CreateAndDefine<UserDefinedTypeSymbol>("Bar", globalScope);
            superClass.SuperClass = superSuperClass;
            testClass = (UserDefinedTypeSymbol) Symbol.CreateAndDefine<UserDefinedTypeSymbol>("Baz", globalScope);
            testClass.SuperClass = superClass;
            someType = new BuiltinTypeSymbol("int", globalScope);
        }

        [Test]
        public void MethodsCanBeDefined()
        {
            testClass.Define(new MethodSymbol("foo", someType, testClass));
            Assert.That(testClass.Resolve<MethodSymbol>("foo"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void VariablesCanBeDefined()
        {
            testClass.Define(new VariableSymbol("foo", someType, testClass));
            Assert.That(testClass.Resolve<VariableSymbol>("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void MethodsAndVariablesCanShareAName()
        {
            testClass.Define(new MethodSymbol("foo", someType, testClass));
            testClass.Define(new VariableSymbol("foo", someType, testClass));
            Assert.That(testClass.Resolve<VariableSymbol>("foo"), Is.InstanceOf<VariableSymbol>());
            Assert.That(testClass.Resolve<MethodSymbol>("foo"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void NewClassesCannotBeDefined()
        {
            Assert.Throws<NotSupportedException>(() => testClass.Define(new UserDefinedTypeSymbol("Foo", testClass)));
        }

        [Test]
        public void MethodsAreResolvedInSuperClasses()
        {
            superSuperClass.Define(new MethodSymbol("foo", someType, superSuperClass));
            superClass.Define(new MethodSymbol("bar", someType, superClass));
            var foo = testClass.Resolve<MethodSymbol>("foo");
            var bar = testClass.Resolve<MethodSymbol>("bar");
            Assert.That(foo, Is.InstanceOf<MethodSymbol>());
            Assert.That(foo.Name, Is.EqualTo("foo"));
            Assert.That(bar, Is.InstanceOf<MethodSymbol>());
            Assert.That(bar.Name, Is.EqualTo("bar"));
        }

        [Test]
        public void VariablesAreNotResolvedInSuperClasses()
        {
            superSuperClass.Define(new VariableSymbol("foo", someType, superSuperClass));
            Assert.That(testClass.Resolve<VariableSymbol>("foo"), Is.Null);
        }

        [Test]
        public void VariablesCanBeResolvedInEnclosingScopes()
        {
            globalScope.Define(new VariableSymbol("bar", someType, superClass));
            Assert.That(testClass.Resolve<VariableSymbol>("bar"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void NamesCanHideNamesInOtherScopes()
        {
            superClass.Define(new MethodSymbol("foo", someType, superClass));
            testClass.Define(new MethodSymbol("foo", someType, testClass));
            globalScope.Define(new VariableSymbol("bar", someType, globalScope));
            testClass.Define(new VariableSymbol("bar", someType, testClass));
            Assert.That(testClass.Resolve<MethodSymbol>("foo").EnclosingScope, Is.EqualTo(testClass));
            Assert.That(testClass.Resolve<VariableSymbol>("bar").EnclosingScope, Is.EqualTo(testClass));
        }

        [Test]
        public void CannotRedefineNames()
        {
            testClass.Define(new VariableSymbol("foo", someType, testClass));
            Assert.False(testClass.Define(new VariableSymbol("foo", someType, testClass)));
        }
    }

    [TestFixture]
    class MethodScopeTest
    {
        private GlobalScope globalScope;
        private UserDefinedTypeSymbol testClass;
        private MethodSymbol testMethod;
        private IType someType;

        [SetUp]
        public void SetUp()
        {
            globalScope = new GlobalScope();
            testClass = (UserDefinedTypeSymbol)Symbol.CreateAndDefine<UserDefinedTypeSymbol>("Foo", globalScope);
            testMethod = (MethodSymbol) Symbol.CreateAndDefine<MethodSymbol>(
                "foo", new BuiltinTypeSymbol("int", globalScope), testClass);
            someType = new BuiltinTypeSymbol("int", globalScope);
        }

        [Test]
        public void CanDefineVariables()
        {
            testMethod.Define(new VariableSymbol("foo", someType, testMethod));
            Assert.That(testMethod.Resolve<VariableSymbol>("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void CannotDefineMethods()
        {
            Assert.Throws<NotSupportedException>(() =>
                testMethod.Define(new MethodSymbol("bar", someType, testClass)));
        }

        [Test]
        public void CannotDefineClasses()
        {
            Assert.Throws<NotSupportedException>(() =>
                                                 testMethod.Define(new UserDefinedTypeSymbol("Bar", testMethod)));
        }

        [Test]
        public void ResolvesMethodsInEnclosingScope()
        {
            testClass.Define(new MethodSymbol("bar", someType, testClass));
            Assert.That(testMethod.Resolve<MethodSymbol>("bar"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void ResolvesVariablesInEnclosingScopes()
        {
            testClass.Define(new VariableSymbol("bar", someType, testClass));
            Assert.That(testMethod.Resolve<VariableSymbol>("bar"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void CannotRedefineNames()
        {
            testMethod.Define(new VariableSymbol("foo", someType, testMethod));
            Assert.False(testMethod.Define(new VariableSymbol("foo", someType, testMethod)));
        }
    }

    [TestFixture]
    class GenericScopeTest
    {
        private GlobalScope globalScope;
        private LocalScope blockScope;
        private IType someType;

        [SetUp]
        public void SetUp()
        {
            globalScope = new GlobalScope();
            blockScope = new LocalScope(globalScope);
            someType = new BuiltinTypeSymbol("int", globalScope);
        }

        [Test]
        public void NamesCanHideNamesInEnclosingScopes()
        {
            Symbol.CreateAndDefine<VariableSymbol>("foo", someType, globalScope);
            Symbol.CreateAndDefine<VariableSymbol>("foo", someType, blockScope);
            Assert.That(blockScope.Resolve<VariableSymbol>("foo").EnclosingScope, Is.EqualTo(blockScope));
        }

        [Test]
        public void VariablesAreResolvedInEnclosingScopes()
        {
            Symbol.CreateAndDefine<VariableSymbol>("foo", someType, globalScope);
            Assert.That(blockScope.Resolve<VariableSymbol>("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void CannotRedefineNames()
        {
            globalScope.Define(new VariableSymbol("foo", someType, globalScope));
            Assert.False(globalScope.Define(new VariableSymbol("foo", someType, globalScope)));
        }
    }
}