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
    public partial class CodeGenerator
    {
        private class InstructionGenerator : INodeVisitor
        {
            private CodeGenerator _parent;
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

            public InstructionGenerator(CodeGenerator parent)
            {
                _parent = parent;
            }

            public void GenerateInstructions()
            {
                _parent._astRoot.Accept(this);
            }

            public void Visit(Program node) { }

            public void Visit(ClassDeclaration node)
            {
                TypeBuilder thisType = _parent._symbolTable.ResolveTypeName(node.Name).Builder;
                _currentType = thisType;
            }

            public void Visit(VariableDeclaration node) { }

            public void Visit(MethodDeclaration node)
            {
                _currentMethod = _parent._symbolTable.Scopes[node].ResolveMethod(node.Name).Builder;
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
            { // The left hand side of an assignment must be either
                // a variable reference or an array indexing expression.
                var il = _currentMethod.GetILGenerator();
                if (node.LeftHandSide is VariableReferenceExpression)
                {
                    var reference = (VariableReferenceExpression)node.LeftHandSide;
                    var variable = _parent._symbolTable.Scopes[reference].ResolveVariable(reference.Name);
                    var decl = (VariableDeclaration)_parent._symbolTable.Definitions[variable];
                    switch (decl.VariableKind)
                    {
                        case VariableDeclaration.Kind.Class:
                            il.Emit(OpCodes.Stfld, decl.Name);
                            break;
                        case VariableDeclaration.Kind.Local:
                            il.Emit(OpCodes.Stloc, decl.LocalIndex);
                            break;
                        case VariableDeclaration.Kind.Formal:
                            il.Emit(OpCodes.Starg, GetParameterIndex(decl, _currentMethod));
                            break;
                    }
                }
                else
                {
                    // TODO: handle array case
                    throw new NotImplementedException();
                }
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
                MethodInfo method;
                if (node.MethodOwner.Type is ArrayType)
                {
                    method = _parent.BuildType(node.MethodOwner.Type.Name, true).
                        GetMethod("Length", new Type[] { });
                }
                else
                {   // TODO: should there be a separate pass to define all method builders?
                    method = _parent._symbolTable.ResolveTypeName(node.MethodOwner.Type.Name).
                        Scope.ResolveMethod(node.MethodName).Builder;
                }
                _currentMethod.GetILGenerator().Emit(OpCodes.Call, method);
            }

            public void Visit(InstanceCreationExpression node)
            {
                // TODO: handle array creation cases
                Type type = _parent.BuildType(node.CreatedType, node.IsArrayCreation);
                _currentMethod.GetILGenerator().Emit(OpCodes.Newobj, _parent._constructors[type]);
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
                var variable = _parent._symbolTable.Scopes[node].ResolveVariable(node.Name);
                var definition = (VariableDeclaration)_parent._symbolTable.Definitions[variable];
                var il = _currentMethod.GetILGenerator();
                switch (definition.VariableKind)
                {
                    case VariableDeclaration.Kind.Class:
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, node.Name);
                        break;
                    case VariableDeclaration.Kind.Formal:
                        il.Emit(OpCodes.Ldarg, GetParameterIndex(definition, _currentMethod));
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
}
