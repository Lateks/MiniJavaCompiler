using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using System.Reflection;
using System.Reflection.Emit;
using MiniJavaCompiler.Support.SymbolTable.Symbols;

namespace MiniJavaCompiler.Backend
{
    public class CodeGenerator : INodeVisitor
    {
        private readonly Program _astRoot;
        private readonly SymbolTable _symbolTable;
        private readonly AssemblyBuilder _asmBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private TypeBuilder _currentType;

        public CodeGenerator(SymbolTable symbolTable, Program abstractSyntaxTree, string moduleName)
        {
            _symbolTable = symbolTable;
            _astRoot = abstractSyntaxTree;

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
            throw new NotImplementedException();
        }

        public void Visit(MethodDeclaration node)
        {   
            MethodBuilder methodBuilder = _currentType.DefineMethod(node.Name, GetMethodAttributes(node));
            if (node.Name == "main")
            {
                _asmBuilder.SetEntryPoint(methodBuilder);
            }
            // TODO: set parameter types
            // TODO: set return type
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
            throw new NotImplementedException();
        }

        public void Exit(MainClassDeclaration node)
        {
            throw new NotImplementedException();
        }

        public void Exit(MethodDeclaration node)
        {
            throw new NotImplementedException();
        }

        public void Exit(BlockStatement node)
        {
            throw new NotImplementedException();
        }
    }
}
