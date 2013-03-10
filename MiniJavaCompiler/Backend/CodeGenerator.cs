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

namespace MiniJavaCompiler.BackEnd
{
    public partial class CodeGenerator
    {
        private readonly string _moduleName;
        private readonly Program _astRoot;
        private readonly SymbolTable _symbolTable;
        private readonly Dictionary<Type, ConstructorInfo> _constructors;
        private readonly Dictionary<string, TypeBuilder> _types;
        private readonly Dictionary<MethodSymbol, MethodBuilder> _methods;

        private AssemblyBuilder _asmBuilder;
        private ModuleBuilder _moduleBuilder;

        public CodeGenerator(SymbolTable symbolTable, Program abstractSyntaxTree, string moduleName)
        {
            _moduleName = moduleName;
            _astRoot = abstractSyntaxTree;
            _symbolTable = symbolTable;
            _constructors = new Dictionary<Type, ConstructorInfo>();
            _types = new Dictionary<string, TypeBuilder>();
            _methods = new Dictionary<MethodSymbol, MethodBuilder>();
        }

        public void GenerateCode(string outputFileName = "out.exe")
        {
            new AssemblyCreator(this).SetUpAssembly(); // Sets up _asmBuilder, _moduleBuilder and _constructors.
            new InstructionGenerator(this).GenerateInstructions();
            FinalizeTypes();
            _asmBuilder.Save(outputFileName);
        }

        private void FinalizeTypes()
        {
            foreach (var typeName in _symbolTable.ScalarTypeNames)
            {
                _types[typeName].CreateType();
            }
        }

        // If the method is not static, parameter 0 is a reference to the object
        // and this needs to be taken into account.
        private static int GetParameterIndex(VariableDeclaration node, MethodBuilder method)
        {
            return method.IsStatic ? node.LocalIndex : node.LocalIndex + 1;
        }

        private Type BuildType(string typeName, bool isArray)
        {
            Type type;
            if (typeName == MiniJavaInfo.VoidType)
            {
                type = typeof(void);
            }
            else if (typeName == MiniJavaInfo.IntType)
            {
                type = typeof(Int32);
            }
            else if (typeName == MiniJavaInfo.BoolType)
            {
                type = typeof(Boolean);
            }
            else
            {
                type = _types[typeName];
            }

            if (isArray)
            {
                type = type.MakeArrayType();
            }
            return type;
        }
    }
}
