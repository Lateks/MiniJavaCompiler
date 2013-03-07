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
        private readonly Dictionary<Type, ConstructorInfo> _constructors;
        private TypeBuilder _currentType;
        private MethodBuilder _currentMethod;

        private static Dictionary<MiniJavaInfo.Operator, OpCode> operators =
            new Dictionary<MiniJavaInfo.Operator, OpCode>()
        {
            { MiniJavaInfo.Operator.Add, OpCodes.Add },
            { MiniJavaInfo.Operator.Sub, OpCodes.Sub },
            { MiniJavaInfo.Operator.Div, OpCodes.Div },
            { MiniJavaInfo.Operator.Mul, OpCodes.Mul },
            { MiniJavaInfo.Operator.Lt, OpCodes.Clt },
            { MiniJavaInfo.Operator.Gt, OpCodes.Cgt },
            { MiniJavaInfo.Operator.And, OpCodes.And },
            { MiniJavaInfo.Operator.Or, OpCodes.Or },
            { MiniJavaInfo.Operator.Eq, OpCodes.Ceq },
            { MiniJavaInfo.Operator.Mod, OpCodes.Rem },
            { MiniJavaInfo.Operator.Not, OpCodes.Not }
        };

        public CodeGenerator(SymbolTable symbolTable, Program abstractSyntaxTree, string moduleName)
        {
            _symbolTable = symbolTable;
            _astRoot = abstractSyntaxTree;
            _constructors = new Dictionary<Type, ConstructorInfo>();

            // Set up a single module assembly.
            AssemblyName name = new AssemblyName(moduleName);
            _asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
            _moduleBuilder = _asmBuilder.DefineDynamicModule(moduleName);

            SetUpScalarTypes();
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

        public void GenerateCode()
        {
            _astRoot.Accept(this);
            _asmBuilder.Save("out.exe"); // TODO: naming
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
                    _currentMethod.DefineParameter(getParameterIndex(node), ParameterAttributes.In, node.Name); // TODO: is this builder still needed afterwards?
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
        private int getParameterIndex(VariableDeclaration node)
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

        public void Visit(PrintStatement node)
        {
            MethodInfo printMethod = typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            _currentMethod.GetILGenerator().Emit(OpCodes.Call, printMethod);
        }

        public void Visit(ReturnStatement node)
        {
            _currentMethod.GetILGenerator().Emit(OpCodes.Ret);
        }

        public void Visit(BlockStatement node)
        {
            _currentMethod.GetILGenerator().BeginScope();
        }

        public void Visit(AssertStatement node)
        {
            MethodInfo assertMethod = typeof(System.Diagnostics.Debug).GetMethod("Assert", new Type[] { typeof(bool) });
            _currentMethod.GetILGenerator().Emit(OpCodes.Call, assertMethod);
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
            // TODO: handle array creation cases
            Type type = BuildType(node.CreatedType, node.IsArrayCreation);
            _currentMethod.GetILGenerator().Emit(OpCodes.Newobj, _constructors[type]);
        }

        public void Visit(UnaryOperatorExpression node)
        {
            _currentMethod.GetILGenerator().Emit(operators[node.Operator]);
        }

        public void Visit(BinaryOperatorExpression node)
        {
            _currentMethod.GetILGenerator().Emit(operators[node.Operator]);
        }

        public void Visit(BooleanLiteralExpression node)
        {
            _currentMethod.GetILGenerator().Emit(node.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        }

        public void Visit(ThisExpression node)
        {
            _currentMethod.GetILGenerator().Emit(OpCodes.Ldarg_0);
        }

        public void Visit(ArrayIndexingExpression node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableReferenceExpression node)
        {
            var variable = _symbolTable.Scopes[node].ResolveVariable(node.Name);
            var definition = (VariableDeclaration) _symbolTable.Definitions[variable];
            var il = _currentMethod.GetILGenerator();
            switch (definition.VariableKind)
            {
                case VariableDeclaration.Kind.Class:
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, node.Name);
                    break;
                case VariableDeclaration.Kind.Formal:
                    il.Emit(OpCodes.Ldarg, getParameterIndex(definition));
                    break;
                case VariableDeclaration.Kind.Local:
                    il.Emit(OpCodes.Ldloc, definition.LocalIndex);
                    break;
            }
        }

        public void Visit(IntegerLiteralExpression node)
        {
            _currentMethod.GetILGenerator().Emit(OpCodes.Ldc_I4, node.IntValue);
        }

        public void Exit(ClassDeclaration node)
        {
            _currentType.CreateType();
            _currentType = null;
        }

        public void Exit(MainClassDeclaration node)
        {
            _currentType.CreateType();
            _currentType = null;
        }

        public void Exit(MethodDeclaration node)
        {
            _currentMethod = null;
        }

        public void Exit(BlockStatement node)
        {
            _currentMethod.GetILGenerator().EndScope();
        }
    }
}
