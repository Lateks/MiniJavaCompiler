using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using System.Reflection;
using System.Reflection.Emit;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.Backend
{
    public class CodeGenerator : INodeVisitor
    {
        private readonly Program _astRoot;
        private readonly SymbolTable _symbolTable;
        private readonly AssemblyBuilder _asmBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private TypeBuilder _currentType;
        private MethodBuilder _currentMethod;
        private int _currentParameterNumber;

        public CodeGenerator(SymbolTable symbolTable, Program abstractSyntaxTree, string moduleName)
        {
            _symbolTable = symbolTable;
            _astRoot = abstractSyntaxTree;
            _currentParameterNumber = 0;

            // Set up a single module assembly.
            AssemblyName name = new AssemblyName(moduleName);
            _asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
            _moduleBuilder = _asmBuilder.DefineDynamicModule(moduleName);

            SetUpScalarTypes();
        }

        // Defines TypeBuilders for all user defined types and stores them.
        private void SetUpScalarTypes()
        {
            foreach (string typeName in _symbolTable.ScalarTypeNames)
            {
                TypeSymbol sym = _symbolTable.ResolveTypeName(typeName);
                TypeBuilder typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
                sym.Builder = typeBuilder;
            }
        }

        private void SetUpSuperClasses()
        {
            throw new NotImplementedException();
        }

        public void GenerateCode()
        {
            _astRoot.Accept(this);
        }

        public void Visit(Program node) { }

        public void Visit(MainClassDeclaration node)
        {
            _currentType = _symbolTable.ResolveTypeName(node.Name).Builder;
        }

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

        public void Visit(VariableDeclaration node)
        {
            switch (node.VariableKind)
            {
                case VariableDeclaration.Kind.Formal:
                    _currentMethod.DefineParameter(_currentParameterNumber, ParameterAttributes.In, node.Name); // TODO: is this builder still needed afterwards?
                    _currentParameterNumber++;
                    break;
                case VariableDeclaration.Kind.Local:
                    _currentMethod.GetILGenerator().DeclareLocal(BuildType(node.Type, node.IsArray));
                    break;
                case VariableDeclaration.Kind.Class:
                    _currentType.DefineField(node.Name, BuildType(node.Type, node.IsArray), FieldAttributes.Public);
                    break;
            }
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
            _currentParameterNumber = 0;
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
            Type retType;
            if (typeName == MiniJavaInfo.IntType)
            {
                retType = typeof(Int32);
            }
            else if (typeName == MiniJavaInfo.BoolType)
            {
                retType = typeof(Boolean);
            }
            else
            {
                retType = _symbolTable.ResolveTypeName(typeName).Builder;
            }

            if (isArray)
            {
                // TODO: need array types
            }
            return retType;
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

        public void Visit(PrintStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ReturnStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(BlockStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(AssertStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(AssignmentStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(IfStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(WhileStatement node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MethodInvocation node)
        {
            throw new NotImplementedException();
        }

        public void Visit(InstanceCreationExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(UnaryOperatorExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(BinaryOperatorExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(BooleanLiteralExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ThisExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ArrayIndexingExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableReferenceExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(IntegerLiteralExpression node)
        {
            throw new NotImplementedException();
        }

        public void Exit(ClassDeclaration node)
        {
            _currentType = null;
        }

        public void Exit(MainClassDeclaration node)
        {
            _currentType = null;
        }

        public void Exit(MethodDeclaration node)
        {
            _currentMethod = null;
        }

        public void Exit(BlockStatement node) { }
    }
}
