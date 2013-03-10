using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MiniJavaCompiler.BackEnd
{
    public partial class CodeGenerator
    {
        private class AssemblyCreator : INodeVisitor
        {
            private CodeGenerator _parent;
            private TypeBuilder _currentType;
            private MethodBuilder _currentMethod;

            public AssemblyCreator(CodeGenerator parent)
            {
                _parent = parent;
            }

            // Sets up a single module assembly as well as all user-defined types, methods and variables.
            public void SetUpAssembly()
            {
                AssemblyName name = new AssemblyName(_parent._moduleName);
                _parent._asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
                _parent._moduleBuilder = _parent._asmBuilder.DefineDynamicModule(_parent._moduleName);

                SetUpScalarTypes();
                _parent._astRoot.Accept(this);
            }

            // Defines TypeBuilders for all user defined types and stores them and their constructors.
            private void SetUpScalarTypes()
            {
                foreach (string typeName in _parent._symbolTable.ScalarTypeNames)
                {
                    TypeBuilder typeBuilder = _parent._moduleBuilder.DefineType(
                        typeName, TypeAttributes.Public | TypeAttributes.Class); // TODO: are these IsByRef by default?
                    _parent._types[typeName] = typeBuilder;
                    _parent._constructors[typeBuilder] =
                        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.Static);
                }
            }

            public void Visit(Program node) { }

            public void Visit(ClassDeclaration node)
            {
                TypeBuilder thisType = _parent._types[node.Name];
                if (node.InheritedClassName != null)
                {
                    TypeBuilder superClass = _parent._types[node.InheritedClassName];
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
                        _currentMethod.DefineParameter(GetParameterIndex(node, _currentMethod),
                            ParameterAttributes.In, node.Name); // TODO: is this builder still needed afterwards?
                        break;
                    case VariableDeclaration.Kind.Local:
                        _currentMethod.GetILGenerator().DeclareLocal(
                            _parent.BuildType(node.Type, node.IsArray));
                        break;
                    case VariableDeclaration.Kind.Class:
                        _currentType.DefineField(node.Name,
                            _parent.BuildType(node.Type, node.IsArray), FieldAttributes.Public);
                        break;
                }
            }

            public void Visit(MethodDeclaration node)
            {
                MethodBuilder methodBuilder = _currentType.DefineMethod(node.Name, GetMethodAttributes(node));
                if (node.Name == MiniJavaInfo.MainMethodIdent)
                {
                    _parent._asmBuilder.SetEntryPoint(methodBuilder);
                }

                methodBuilder.SetReturnType(GetReturnType(node));
                methodBuilder.SetParameters(GetParameterTypes(node));

                var sym = _parent._symbolTable.Scopes[node].ResolveMethod(node.Name);
                _parent._methods[sym] = methodBuilder;

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
                    types[i] = _parent.BuildType(decl.Type, decl.IsArray);
                }
                return types;
            }

            private Type GetReturnType(MethodDeclaration node)
            {
                return _parent.BuildType(node.Type, node.IsArray);
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
}
