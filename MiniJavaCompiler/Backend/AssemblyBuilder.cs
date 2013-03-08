using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MiniJavaCompiler.Backend
{
    public struct AssemblyData
    {
        public AssemblyBuilder AsmBuilder;
        public ModuleBuilder ModBuilder;
        public Dictionary<Type, ConstructorInfo> Constructors;

        public AssemblyData(AssemblyBuilder asmBuilder, ModuleBuilder modBuilder,
            Dictionary<Type, ConstructorInfo> constructors)
        {
            AsmBuilder = asmBuilder;
            ModBuilder = modBuilder;
            Constructors = constructors;
        }
    }

    public class AssemblyCreator : INodeVisitor
    {
        private readonly string _moduleName;
        private readonly Program _astRoot;
        private readonly SymbolTable _symbolTable;
        private readonly Dictionary<Type, ConstructorInfo> _constructors;

        private TypeBuilder _currentType;
        private MethodBuilder _currentMethod;
        private AssemblyBuilder _asmBuilder;
        private ModuleBuilder _moduleBuilder;

        public AssemblyCreator(SymbolTable symbolTable, Program abstractSyntaxTree, string moduleName)
        {
            _symbolTable = symbolTable;
            _astRoot = abstractSyntaxTree;
            _constructors = new Dictionary<Type, ConstructorInfo>();
            _moduleName = moduleName;
        }

        // Set up a single module assembly as well as all user-defined types, methods and variables.
        public AssemblyData SetUpAssembly()
        {
            AssemblyName name = new AssemblyName(_moduleName);
            _asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
            _moduleBuilder = _asmBuilder.DefineDynamicModule(_moduleName);

            SetUpScalarTypes();
            _astRoot.Accept(this);

            return new AssemblyData(_asmBuilder, _moduleBuilder, _constructors);
        }

        // Defines TypeBuilders for all user defined types and stores them and their constructors.
        private void SetUpScalarTypes()
        {
            foreach (string typeName in _symbolTable.ScalarTypeNames)
            {
                TypeSymbol sym = _symbolTable.ResolveTypeName(typeName);
                TypeBuilder typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class); // TODO: are these IsByRef by default?
                sym.Builder = typeBuilder;
                _constructors[typeBuilder] =
                    typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.Static);
            }
        }

        public void Visit(Program node) { }

        public void Visit(ClassDeclaration node)
        {
            TypeBuilder thisType = _symbolTable.ResolveTypeName(node.Name).Builder;
            if (node.InheritedClass != null)
            {
                TypeBuilder superClass = _symbolTable.ResolveTypeName(node.InheritedClass).Builder;
                thisType.SetParent(superClass);
            }
            _currentType = thisType;
        }

        public void Exit(ClassDeclaration node)
        {
            _currentType = null;
        }

        public void Visit(VariableDeclaration node)
        {
            switch (node.VariableKind)
            {
                case VariableDeclaration.Kind.Formal:
                    _currentMethod.DefineParameter(GetParameterIndex(node), ParameterAttributes.In, node.Name); // TODO: is this builder still needed afterwards?
                    break;
                case VariableDeclaration.Kind.Local:
                    _currentMethod.GetILGenerator().DeclareLocal(BuildType(node.Type, node.IsArray));
                    break;
                case VariableDeclaration.Kind.Class:
                    _currentType.DefineField(node.Name, BuildType(node.Type, node.IsArray), FieldAttributes.Public);
                    break;
            }
        }

        // If the method is not static, parameter 0 is a reference to the object
        // and this needs to be taken into account.
        private int GetParameterIndex(VariableDeclaration node)
        {
            return _currentMethod.IsStatic ? node.LocalIndex : node.LocalIndex + 1;
        }

        public void Visit(MethodDeclaration node)
        {
            MethodBuilder methodBuilder = _currentType.DefineMethod(node.Name, GetMethodAttributes(node));
            if (node.Name == MiniJavaInfo.MainMethodIdent)
            {
                _asmBuilder.SetEntryPoint(methodBuilder);
            }

            methodBuilder.SetReturnType(GetReturnType(node));
            methodBuilder.SetParameters(GetParameterTypes(node));

            _currentMethod = methodBuilder;
        }

        public void Exit(MethodDeclaration node)
        {
            _currentMethod = null;
        }

        private Type[] GetParameterTypes(MethodDeclaration node)
        {
            Type[] types = new Type[node.Formals.Count];
            for (int i = 0; i < node.Formals.Count; i++)
            {
                VariableDeclaration decl = node.Formals[i];
                types[i] = BuildType(decl.Type, decl.IsArray);
            }
            return types;
        }

        private Type GetReturnType(MethodDeclaration node)
        {
            MethodSymbol sym = _symbolTable.ResolveClass(node).Scope.ResolveMethod(node.Name);
            return BuildType(node.Type, node.IsArray);
        }

        private Type BuildType(string typeName, bool isArray)
        {
            Type type;
            if (typeName == MiniJavaInfo.VoidType)
            {
                type = typeof(void);
            }
            if (typeName == MiniJavaInfo.IntType)
            {
                type = typeof(Int32);
            }
            else if (typeName == MiniJavaInfo.BoolType)
            {
                type = typeof(Boolean);
            }
            else
            {
                type = _symbolTable.ResolveTypeName(typeName).Builder;
            }

            if (isArray)
            {
                type = type.MakeArrayType();
            }
            return type;
        }

        private static MethodAttributes GetMethodAttributes(MethodDeclaration node)
        {
            MethodAttributes attrs = MethodAttributes.Public; // all methods are public
            if (node.IsStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            return attrs;
        }

        public void Visit(PrintStatement node) { }

        public void Visit(ReturnStatement node) { }

        public void Visit(BlockStatement node) { }

        public void Visit(AssertStatement node) { }

        public void Visit(AssignmentStatement node) { }

        public void Visit(IfStatement node) { }

        public void Visit(WhileStatement node) { }

        public void Visit(MethodInvocation node) { }

        public void Visit(InstanceCreationExpression node) { }

        public void Visit(UnaryOperatorExpression node) { }

        public void Visit(BinaryOperatorExpression node) { }

        public void Visit(BooleanLiteralExpression node) { }

        public void Visit(ThisExpression node) { }

        public void Visit(ArrayIndexingExpression node) { }

        public void Visit(VariableReferenceExpression node) { }

        public void Visit(IntegerLiteralExpression node) { }

        public void Exit(BlockStatement node) { }
    }
}
