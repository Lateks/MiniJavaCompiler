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
        private class AssemblyGenerator : NodeVisitorBase
        {
            private CodeGenerator _parent;
            private TypeBuilder _currentType;
            private MethodBuilder _currentMethod;

            public AssemblyGenerator(CodeGenerator parent)
            {
                _parent = parent;
            }

            // Sets up a single module assembly as well as all user-defined types, methods and variables.
            public void SetUpAssembly(string outputFileName)
            {
                AssemblyName name = new AssemblyName(_parent._moduleName);
                _parent._asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
                _parent._moduleBuilder = _parent._asmBuilder.DefineDynamicModule(_parent._moduleName, outputFileName);

                SetUpScalarTypes();
                _parent._astRoot.Accept(this);
            }

            // Defines TypeBuilders for all user defined types and stores them
            // for later reference.
            private void SetUpScalarTypes()
            {
                foreach (string typeName in _parent._symbolTable.UserDefinedTypeNames)
                {
                    _parent._types[typeName] = _parent._moduleBuilder.DefineType(
                        typeName, TypeAttributes.Public | TypeAttributes.Class);
                }
            }

            public override void Visit(ClassDeclaration node)
            {
                TypeBuilder thisType = _parent._types[node.Name];
                if (node.InheritedClassName != null)
                {   // Define non-default constructor. Constructor body is not emitted
                    // until instruction generation.
                    TypeBuilder superClass = _parent._types[node.InheritedClassName];
                    thisType.SetParent(superClass);
                    _parent._constructors[thisType] =
                        thisType.DefineConstructor(MethodAttributes.Public,
                        CallingConventions.HasThis, Type.EmptyTypes);
                }
                else
                {
                    _parent._constructors[thisType] =
                        thisType.DefineDefaultConstructor(MethodAttributes.Public);
                }
                _currentType = thisType;
            }

            public override void Exit(ClassDeclaration node)
            {
                _currentType = null;
            }

            public override void Visit(VariableDeclaration node)
            {
                switch (node.VariableKind)
                {   // Local and formal variables can be referred to by their index.
                    // Fields need to be stored as FieldBuilders for future reference.
                    case VariableDeclaration.Kind.Formal:
                        _currentMethod.DefineParameter(node.LocalIndex,
                            ParameterAttributes.In, node.Name);
                        break;
                    case VariableDeclaration.Kind.Local:
                        // Do not generate the variable if it is never used (if optimization enabled).
                        if (!_parent._optimize || node.Used)
                        {
                            _currentMethod.GetILGenerator().DeclareLocal(
                                _parent.BuildType(node.TypeName, node.IsArray));
                        }
                        break;
                    case VariableDeclaration.Kind.Class:
                        var fieldBuilder = _currentType.DefineField(node.Name,
                            _parent.BuildType(node.TypeName, node.IsArray), FieldAttributes.Private);
                        var sym = node.Scope.ResolveVariable(node.Name);
                        _parent._fields[sym] = fieldBuilder;
                        break;
                }
            }

            public override void Visit(MethodDeclaration node)
            {
                MethodBuilder methodBuilder = _currentType.DefineMethod(node.Name, GetMethodAttributes(node));
                if (node.IsEntryPoint)
                {
                    _parent._asmBuilder.SetEntryPoint(methodBuilder, PEFileKinds.ConsoleApplication);
                }

                methodBuilder.SetReturnType(GetReturnType(node));
                methodBuilder.SetParameters(GetParameterTypes(node));

                var sym = node.Scope.ResolveMethod(node.Name);
                _parent._methods[sym] = methodBuilder;

                _currentMethod = methodBuilder;
            }

            public override void Exit(MethodDeclaration node)
            {
                _currentMethod = null;
            }

            private Type[] GetParameterTypes(MethodDeclaration node)
            {
                if (node.Formals.Count == 0) return Type.EmptyTypes;
                Type[] types = new Type[node.Formals.Count];
                for (int i = 0; i < node.Formals.Count; i++)
                {
                    VariableDeclaration decl = node.Formals[i];
                    types[i] = _parent.BuildType(decl.TypeName, decl.IsArray);
                }
                return types;
            }

            private Type GetReturnType(MethodDeclaration node)
            {
                return _parent.BuildType(node.TypeName, node.IsArray);
            }

            private static MethodAttributes GetMethodAttributes(MethodDeclaration node)
            {
                MethodAttributes attrs = MethodAttributes.Public; // all methods are public
                if (node.IsStatic)
                {
                    attrs |= MethodAttributes.Static;
                }
                else
                {
                    attrs |= MethodAttributes.Virtual;
                }
                return attrs;
            }
        }
    }
}
