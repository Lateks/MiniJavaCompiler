using NUnit.Framework;
using MiniJavaCompiler.Support.SymbolTable;

namespace MiniJavaCompilerTest.Frontend
{
    internal static class SymbolCreationHelper
    {
        internal static UserDefinedTypeSymbol CreateAndDefineClass(string name, ITypeScope scope)
        {
            var sym = new UserDefinedTypeSymbol(name, scope);
            scope.Define(sym);
            return sym;
        }

        internal static MethodSymbol CreateAndDefineMethod(string name, IType type, IMethodScope scope)
        {
            var sym = new MethodSymbol(name, type, scope);
            scope.Define(sym);
            return sym;
        }

        internal static VariableSymbol CreateAndDefineVariable(string name, IType type, IVariableScope scope)
        {
            var sym = new VariableSymbol(name, type, scope);
            scope.Define(sym);
            return sym;
        }

    }

    [TestFixture]
    class ClassScopeTest
    {
        private GlobalScope _globalScope;
        private UserDefinedTypeSymbol _testClass;
        private UserDefinedTypeSymbol _superClass;
        private UserDefinedTypeSymbol _superSuperClass;
        private IType _someType;

        [SetUp]
        public void SetUp()
        {
            _globalScope = new GlobalScope();
            _superSuperClass = SymbolCreationHelper.CreateAndDefineClass("Foo", _globalScope);
            _superClass = SymbolCreationHelper.CreateAndDefineClass("Bar", _globalScope);
            _superClass.SuperClass = _superSuperClass;
            _testClass = SymbolCreationHelper.CreateAndDefineClass("Baz", _globalScope);
            _testClass.SuperClass = _superClass;
            _someType = new BuiltinTypeSymbol("int", _globalScope);
        }

        [Test]
        public void MethodsCanBeDefined()
        {
            _testClass.Define(new MethodSymbol("foo", _someType, _testClass));
            Assert.That(_testClass.Resolve<MethodSymbol>("foo"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void VariablesCanBeDefined()
        {
            _testClass.Define(new VariableSymbol("foo", _someType, _testClass));
            Assert.That(_testClass.Resolve<VariableSymbol>("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void MethodsAndVariablesCanShareAName()
        {
            _testClass.Define(new MethodSymbol("foo", _someType, _testClass));
            _testClass.Define(new VariableSymbol("foo", _someType, _testClass));
            Assert.That(_testClass.Resolve<VariableSymbol>("foo"), Is.InstanceOf<VariableSymbol>());
            Assert.That(_testClass.Resolve<MethodSymbol>("foo"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void MethodsAreResolvedInSuperClasses()
        {
            _superSuperClass.Define(new MethodSymbol("foo", _someType, _superSuperClass));
            _superClass.Define(new MethodSymbol("bar", _someType, _superClass));
            var foo = _testClass.Resolve<MethodSymbol>("foo");
            var bar = _testClass.Resolve<MethodSymbol>("bar");
            Assert.That(foo, Is.InstanceOf<MethodSymbol>());
            Assert.That(foo.Name, Is.EqualTo("foo"));
            Assert.That(bar, Is.InstanceOf<MethodSymbol>());
            Assert.That(bar.Name, Is.EqualTo("bar"));
        }

        [Test]
        public void VariablesAreNotResolvedInSuperClasses()
        {
            _superSuperClass.Define(new VariableSymbol("foo", _someType, _superSuperClass));
            Assert.That(_testClass.Resolve<VariableSymbol>("foo"), Is.Null);
        }

        [Test]
        public void CannotRedefineNames()
        {
            _testClass.Define(new VariableSymbol("foo", _someType, _testClass));
            Assert.False(_testClass.Define(new VariableSymbol("foo", _someType, _testClass)));
        }
    }

    [TestFixture]
    class MethodScopeTest
    {
        private GlobalScope _globalScope;
        private UserDefinedTypeSymbol _testClass;
        private MethodSymbol _testMethod;
        private IType _someType;

        [SetUp]
        public void SetUp()
        {
            _globalScope = new GlobalScope();
            _testClass = SymbolCreationHelper.CreateAndDefineClass("Foo", _globalScope);
            _testMethod = SymbolCreationHelper.CreateAndDefineMethod(
                "foo", new BuiltinTypeSymbol("int", _globalScope), _testClass);
            _someType = new BuiltinTypeSymbol("int", _globalScope);
        }

        [Test]
        public void CanDefineVariables()
        {
            _testMethod.Define(new VariableSymbol("foo", _someType, _testMethod));
            Assert.That(_testMethod.Resolve<VariableSymbol>("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void ResolvesMethodsInEnclosingScope()
        {
            _testClass.Define(new MethodSymbol("bar", _someType, _testClass));
            Assert.That(_testMethod.Resolve<MethodSymbol>("bar"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void ResolvesVariablesInEnclosingScopes()
        {
            _testClass.Define(new VariableSymbol("bar", _someType, _testClass));
            Assert.That(_testMethod.Resolve<VariableSymbol>("bar"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void CannotRedefineNames()
        {
            _testMethod.Define(new VariableSymbol("foo", _someType, _testMethod));
            Assert.False(_testMethod.Define(new VariableSymbol("foo", _someType, _testMethod)));
        }
    }

    [TestFixture]
    class GenericScopeTest
    {
        private GlobalScope _globalScope;
        private LocalScope _blockScope;
        private LocalScope _internalBlockScope;
        private IType _someType;

        [SetUp]
        public void SetUp()
        {
            _globalScope = new GlobalScope();
            _blockScope = new LocalScope(_globalScope);
            _internalBlockScope = new LocalScope(_blockScope);
            _someType = new BuiltinTypeSymbol("int", _globalScope);
        }

        [Test]
        public void NamesCanHideNamesInEnclosingScopes()
        {
            SymbolCreationHelper.CreateAndDefineVariable("foo", _someType, _blockScope);
            SymbolCreationHelper.CreateAndDefineVariable("foo", _someType, _internalBlockScope);
            Assert.That(_internalBlockScope.Resolve<VariableSymbol>("foo").EnclosingScope, Is.EqualTo(_internalBlockScope));
        }

        [Test]
        public void VariablesAreResolvedInEnclosingScopes()
        {
            SymbolCreationHelper.CreateAndDefineVariable("foo", _someType, _blockScope);
            Assert.That(_internalBlockScope.Resolve<VariableSymbol>("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void CannotRedefineNames()
        {
            _globalScope.Define(new UserDefinedTypeSymbol("foo", _globalScope));
            Assert.False(_globalScope.Define(new UserDefinedTypeSymbol("foo", _globalScope)));
        }
    }
}