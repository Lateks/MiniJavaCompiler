using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Type, method and field builders need to be stored for reference in
        // code generation. Local variables and parameters can be referenced
        // by their index, so they do not need to be stored.
        private readonly Dictionary<String, TypeBuilder> _types;
        private readonly Dictionary<MethodSymbol, MethodBuilder> _methods;
        private readonly Dictionary<VariableSymbol, FieldBuilder> _fields;

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
            _fields = new Dictionary<VariableSymbol, FieldBuilder>();
        }

        public void GenerateCode(string outputFileName = "out.exe")
        {
            new AssemblyCreator(this).SetUpAssembly(outputFileName); // Sets up _asmBuilder, _moduleBuilder and _constructors.
            new InstructionGenerator(this).GenerateInstructions();
            _moduleBuilder.CreateGlobalFunctions();
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
        private static short GetParameterIndex(VariableDeclaration node, MethodBuilder method)
        {
            return method.IsStatic ? node.LocalIndex : (short)(node.LocalIndex + 1);
        }

        private Type BuildType(string typeName, bool isArray)
        {
            Debug.Assert(!(typeName == MiniJavaInfo.VoidType && isArray));
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
