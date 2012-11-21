using MiniJavaCompiler.Support.SymbolTable;
using NUnit.Framework;

namespace MiniJavaCompilerTest.Frontend.SemanticAnalysis
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
            _someType = new BuiltInTypeSymbol("int", _globalScope);
        }

        [Test]
        public void MethodsCanBeDefined()
        {
            var methodScope = (IMethodScope)_testClass.Scope;
            methodScope.Define(new MethodSymbol("foo", _someType, methodScope));
            Assert.That(_testClass.Scope.ResolveMethod("foo"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void VariablesCanBeDefined()
        {
            var variableScope = (IVariableScope)_testClass.Scope;
            variableScope.Define(new VariableSymbol("foo", _someType, variableScope));
            Assert.That(_testClass.Scope.ResolveVariable("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void MethodsAndVariablesCanShareAName()
        {
            var methodScope = (IMethodScope)_testClass.Scope;
            var variableScope = (IVariableScope)_testClass.Scope;
            methodScope.Define(new MethodSymbol("foo", _someType, methodScope));
            variableScope.Define(new VariableSymbol("foo", _someType, variableScope));
            Assert.That(_testClass.Scope.ResolveVariable("foo"), Is.InstanceOf<VariableSymbol>());
            Assert.That(_testClass.Scope.ResolveMethod("foo"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void MethodsAreResolvedInSuperClasses()
        {
            var superSuperMethodScope = (IMethodScope)_superSuperClass.Scope;
            var superMethodScope = (IMethodScope)_superClass.Scope;
            superSuperMethodScope.Define(new MethodSymbol("foo", _someType, superSuperMethodScope));
            superMethodScope.Define(new MethodSymbol("bar", _someType, superMethodScope));
            var foo = _testClass.Scope.ResolveMethod("foo");
            var bar = _testClass.Scope.ResolveMethod("bar");
            Assert.That(foo, Is.InstanceOf<MethodSymbol>());
            Assert.That(foo.Name, Is.EqualTo("foo"));
            Assert.That(bar, Is.InstanceOf<MethodSymbol>());
            Assert.That(bar.Name, Is.EqualTo("bar"));
        }

        [Test]
        public void VariablesAreNotResolvedInSuperClasses()
        {
            var superSuperVarScope = (IVariableScope)_superSuperClass.Scope;
            superSuperVarScope.Define(new VariableSymbol("foo", _someType, superSuperVarScope));
            Assert.That(_testClass.Scope.ResolveVariable("foo"), Is.Null);
        }

        [Test]
        public void CannotRedefineNames()
        {
            var variableScope = (IVariableScope)_testClass.Scope;
            variableScope.Define(new VariableSymbol("foo", _someType, variableScope));
            Assert.False(variableScope.Define(new VariableSymbol("foo", _someType, variableScope)));
        }
    }

    [TestFixture]
    class MethodScopeTest
    {
        private GlobalScope _globalScope;
        private IScope _testClassScope;
        private IScope _testMethodScope;
        private IType _someType;

        [SetUp]
        public void SetUp()
        {
            _globalScope = new GlobalScope();
            _testClassScope = SymbolCreationHelper.CreateAndDefineClass("Foo", _globalScope).Scope;
            _testMethodScope = SymbolCreationHelper.CreateAndDefineMethod(
                "foo", new BuiltInTypeSymbol("int", _globalScope), (IMethodScope)_testClassScope).Scope;
            _someType = new BuiltInTypeSymbol("int", _globalScope);
        }

        [Test]
        public void CanDefineVariables()
        {
            var variableScope = (IVariableScope)_testMethodScope;
            variableScope.Define(new VariableSymbol("foo", _someType, variableScope));
            Assert.That(_testMethodScope.ResolveVariable("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void ResolvesMethodsInEnclosingScope()
        {
            var methodScope = (IMethodScope)_testClassScope;
            methodScope.Define(new MethodSymbol("bar", _someType, methodScope));
            Assert.That(_testMethodScope.ResolveMethod("bar"), Is.InstanceOf<MethodSymbol>());
        }

        [Test]
        public void ResolvesVariablesInEnclosingScopes()
        {
            var variableScope = (IVariableScope)_testMethodScope;
            variableScope.Define(new VariableSymbol("bar", _someType, variableScope));
            Assert.That(_testMethodScope.ResolveVariable("bar"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void CannotRedefineNames()
        {
            var variableScope = (IVariableScope)_testMethodScope;
            variableScope.Define(new VariableSymbol("foo", _someType, variableScope));
            Assert.False(variableScope.Define(new VariableSymbol("foo", _someType, _testMethodScope)));
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
            _someType = new BuiltInTypeSymbol("int", _globalScope);
        }

        [Test]
        public void NamesCanHideNamesInEnclosingScopes()
        {
            SymbolCreationHelper.CreateAndDefineVariable("foo", _someType, _blockScope);
            SymbolCreationHelper.CreateAndDefineVariable("foo", _someType, _internalBlockScope);
            Assert.That(_internalBlockScope.ResolveVariable("foo").Scope, Is.EqualTo(_internalBlockScope));
        }

        [Test]
        public void VariablesAreResolvedInEnclosingScopes()
        {
            SymbolCreationHelper.CreateAndDefineVariable("foo", _someType, _blockScope);
            Assert.That(_internalBlockScope.ResolveVariable("foo"), Is.InstanceOf<VariableSymbol>());
        }

        [Test]
        public void CannotRedefineNames()
        {
            _globalScope.Define(new UserDefinedTypeSymbol("foo", _globalScope));
            Assert.False(_globalScope.Define(new UserDefinedTypeSymbol("foo", _globalScope)));
        }
    }
}