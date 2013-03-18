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

namespace MiniJavaCompiler.BackEnd
{
    public partial class CodeGenerator
    {
        private class InstructionGenerator : NodeVisitorBase
        {
            private CodeGenerator _parent;
            private TypeBuilder _currentType;
            private MethodBuilder _currentMethod;
            private ILGenerator IL
            {
                get { return _currentMethod.GetILGenerator(); }
            }

            private static Dictionary<MiniJavaInfo.Operator, OpCode[]> _operatorOpCodes =
                new Dictionary<MiniJavaInfo.Operator, OpCode[]>()
            {
                { MiniJavaInfo.Operator.Add, new OpCode[] { OpCodes.Add } },
                { MiniJavaInfo.Operator.Sub, new OpCode[] { OpCodes.Sub } },
                { MiniJavaInfo.Operator.Div, new OpCode[] { OpCodes.Div } },
                { MiniJavaInfo.Operator.Mul, new OpCode[] { OpCodes.Mul } },
                { MiniJavaInfo.Operator.Lt,  new OpCode[] { OpCodes.Clt } },
                { MiniJavaInfo.Operator.Gt,  new OpCode[] { OpCodes.Cgt } },
                { MiniJavaInfo.Operator.And, new OpCode[] { } }, // Logical operators receive special treatment
                { MiniJavaInfo.Operator.Or,  new OpCode[] { } }, // due to short circuiting.
                { MiniJavaInfo.Operator.Eq,  new OpCode[] { OpCodes.Ceq } },
                { MiniJavaInfo.Operator.Mod, new OpCode[] { OpCodes.Rem } },
                { MiniJavaInfo.Operator.Not, new OpCode[] { OpCodes.Ldc_I4_0, OpCodes.Ceq } }
            };

            private static OpCode[] _int32LoadOpcodes = new OpCode[]
            {
                OpCodes.Ldc_I4_0,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_3,
                OpCodes.Ldc_I4_4,
                OpCodes.Ldc_I4_5,
                OpCodes.Ldc_I4_6,
                OpCodes.Ldc_I4_7,
                OpCodes.Ldc_I4_8
            };

            public InstructionGenerator(CodeGenerator parent)
            {
                _parent = parent;
            }

            public void GenerateInstructions()
            {
                _parent._astRoot.Accept(this);
            }

            public override void Visit(ClassDeclaration node)
            {
                TypeBuilder thisType = _parent._types[node.Name];
                _currentType = thisType;
                if (node.InheritedClassName != null)
                {   // Emit non-default constructor body.
                    var superType = _parent._types[node.InheritedClassName];
                    var il = _parent._constructors[thisType].GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, _parent._constructors[superType]);
                    il.Emit(OpCodes.Ret);
                }
            }

            public override void Visit(MethodDeclaration node)
            {
                var sym = node.Symbol;
                _currentMethod = _parent._methods[sym];
            }

            public override void Visit(PrintStatement node)
            {
                IL.Emit(OpCodes.Call, GetPrintMethod<Int32>());
            }

            private static MethodInfo GetPrintMethod<T>()
            {
                return typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(T) });
            }

            public override void Visit(ReturnStatement node)
            {
                IL.Emit(OpCodes.Ret);
            }

            public override void Visit(BlockStatement node)
            {
                if (node.Label.HasValue)
                {
                    IL.MarkLabel(node.Label.Value);
                }
            }

            public override void Visit(AssertStatement node)
            {
                var jumpLabel = IL.DefineLabel();
                IL.Emit(OpCodes.Brtrue, jumpLabel); // assertion ok


                IL.Emit(OpCodes.Ldstr, String.Format("Exception occurred: AssertionError at {0}.{1}({2},{3})",
                    _currentMethod.DeclaringType.Name, _currentMethod.Name, node.Row, node.Col));
                IL.Emit(OpCodes.Call, GetPrintMethod<String>());

                MethodInfo exitMethod = typeof(System.Environment).GetMethod(
                    "Exit", new Type[] { typeof(Int32) });
                IL.Emit(OpCodes.Ldc_I4_1); // failure status
                IL.Emit(OpCodes.Call, exitMethod);

                IL.MarkLabel(jumpLabel);
            }

            public override void Visit(AssignmentStatement node)
            {   // The left hand side of an assignment must be either
                // a variable reference or an array indexing expression.
                if (node.LeftHandSide is VariableReferenceExpression)
                {
                    var reference = (VariableReferenceExpression)node.LeftHandSide;
                    var variable = reference.Scope.ResolveVariable(reference.Name);
                    var decl = (VariableDeclaration)_parent._symbolTable.Declarations[variable];
                    switch (decl.VariableKind)
                    {
                        case VariableDeclaration.Kind.Class:
                            IL.Emit(OpCodes.Stfld, _parent._fields[variable]);
                            break;
                        case VariableDeclaration.Kind.Local:
                            EmitLocalStore(decl.LocalIndex);
                            break;
                        case VariableDeclaration.Kind.Formal:
                            EmitArgStore(GetParameterIndex(decl, _currentMethod));
                            break;
                    }
                }
                else
                {   // The address to store to should be on the top of the stack just
                    // under the object being stored.
                    var rhsType = node.RightHandSide.Type;
                    if (rhsType.Name == MiniJavaInfo.IntType)
                    {
                        IL.Emit(OpCodes.Stelem_I4);
                    }
                    else if (rhsType.Name == MiniJavaInfo.BoolType)
                    {
                        IL.Emit(OpCodes.Stelem_I1);
                    }
                    else
                    {
                        IL.Emit(OpCodes.Stelem_Ref);
                    }
                }
            }

            public override void VisitAfterCondition(IfStatement node)
            {
                node.ExitLabel = IL.DefineLabel();
                if (node.ElseBranch != null)
                {
                    node.ElseBranch.Label = IL.DefineLabel();
                    IL.Emit(OpCodes.Brfalse, node.ElseBranch.Label.Value);
                }
                else
                {
                    IL.Emit(OpCodes.Brfalse, node.ExitLabel);
                }
            }

            public override void VisitAfterThenBranch(IfStatement node)
            {
                IL.Emit(OpCodes.Br, node.ExitLabel);
            }

            public override void Exit(IfStatement node)
            {
                IL.MarkLabel(node.ExitLabel);
            }

            public override void Visit(WhileStatement node)
            {
                Label test = IL.DefineLabel();
                node.ConditionLabel = test;
                node.LoopBody.Label = IL.DefineLabel();
                IL.Emit(OpCodes.Br, test); // unconditional branch to loop test
            }

            public override void VisitAfterBody(WhileStatement node)
            {
                IL.MarkLabel(node.ConditionLabel);
            }

            public override void Exit(WhileStatement node)
            {
                IL.Emit(OpCodes.Brtrue, node.LoopBody.Label.Value);
            }

            public override void Visit(MethodInvocation node)
            {
                if (node.MethodOwner.Type is ArrayType)
                {
                    IL.Emit(OpCodes.Ldlen);
                }
                else
                {
                    var methodScope = _parent._symbolTable.ResolveTypeName(node.MethodOwner.Type.Name).Scope;
                    var calledMethod = _parent._methods[methodScope.ResolveMethod(node.MethodName)];
                    IL.Emit(OpCodes.Callvirt, calledMethod);
                }
            }

            public override void Visit(InstanceCreationExpression node)
            {
                Type type = _parent.BuildType(node.CreatedTypeName, false);
                if (node.IsArrayCreation)
                {   // arraysize is on top of the stack
                    IL.Emit(OpCodes.Newarr, type);
                }
                else
                {
                    IL.Emit(OpCodes.Newobj, _parent._constructors[type]);
                }
            }

            public override void Visit(UnaryOperatorExpression node)
            {
                EmitOperator(node.Operator);
            }

            public override void VisitAfterLHS(BinaryOperatorExpression node)
            {
                if (!MiniJavaInfo.IsLogicalOperator(node.Operator))
                    return;
                // Emit the first jump code required for boolean operator short circuit.
                var jumpLabel = IL.DefineLabel();
                node.AfterLabel = jumpLabel;
                switch (node.Operator)
                {
                    case MiniJavaInfo.Operator.Or:
                        IL.Emit(OpCodes.Brtrue, jumpLabel);
                        break;
                    case MiniJavaInfo.Operator.And:
                        IL.Emit(OpCodes.Brfalse, jumpLabel);
                        break;
                    default:
                        break;
                }
            }

            public override void Visit(BinaryOperatorExpression node)
            {
                EmitOperator(node.Operator);
                if (MiniJavaInfo.IsLogicalOperator(node.Operator))
                {   // Emit the second jump code and jump labels required
                    // for boolean operator short circuit.
                    var rhsJumpLabel = IL.DefineLabel();
                    IL.Emit(OpCodes.Br, rhsJumpLabel);
                    IL.MarkLabel(node.AfterLabel.Value);
                    var successCode = node.Operator == MiniJavaInfo.Operator.And ?
                        OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1;
                    IL.Emit(successCode);
                    IL.MarkLabel(rhsJumpLabel); // We jump here if rhs was evaluated.
                }
            }

            private void EmitOperator(MiniJavaInfo.Operator op)
            {
                foreach (var opcode in _operatorOpCodes[op])
                {
                    IL.Emit(opcode);
                }
            }

            public override void Visit(BooleanLiteralExpression node)
            {
                IL.Emit(node.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            }

            public override void Visit(ThisExpression node)
            {
                IL.Emit(OpCodes.Ldarg_0);
            }

            public override void Visit(ArrayIndexingExpression node)
            {
                if (node.UsedAsAddress) return; // no need to load anything, index is already on the stack?
                if (node.Type.Name == MiniJavaInfo.IntType)
                {
                    IL.Emit(OpCodes.Ldelem_I4);
                }
                else if (node.Type.Name == MiniJavaInfo.BoolType)
                {
                    IL.Emit(OpCodes.Ldelem_I1);
                }
                else
                {
                    IL.Emit(OpCodes.Ldelem_Ref);
                }
            }


            public override void Visit(VariableReferenceExpression node)
            {
                var variable = node.Scope.ResolveVariable(node.Name);
                var definition = (VariableDeclaration)_parent._symbolTable.Declarations[variable];

                if (node.UsedAsAddress)
                {
                    if (definition.VariableKind == VariableDeclaration.Kind.Class)
                    {   // Load a "this" reference.
                        IL.Emit(OpCodes.Ldarg_0);
                    }
                    return;
                }

                switch (definition.VariableKind)
                {
                    case VariableDeclaration.Kind.Class:
                        IL.Emit(OpCodes.Ldarg_0);
                        IL.Emit(OpCodes.Ldfld, _parent._fields[variable]);
                        break;
                    case VariableDeclaration.Kind.Formal:
                        EmitArgLoad(GetParameterIndex(definition, _currentMethod));
                        break;
                    case VariableDeclaration.Kind.Local:
                        EmitLocalLoad(definition.LocalIndex);
                        break;
                }
            }

            public override void Visit(IntegerLiteralExpression node)
            {
                if (node.IntValue >= 0 && node.IntValue < _int32LoadOpcodes.Length)
                {
                    IL.Emit(_int32LoadOpcodes[node.IntValue]);
                }
                else
                {
                    IL.Emit(OpCodes.Ldc_I4, node.IntValue);
                }
            }

            public override void Exit(ClassDeclaration node)
            {
                _currentType = null;
            }

            public override void Exit(MethodDeclaration node)
            {
                // Emit the return statement for a void method.
                if (node.MethodBody.Count == 0 ||
                    !(node.MethodBody.Last() is ReturnStatement))
                {
                    IL.Emit(OpCodes.Ret);
                }
                _currentMethod = null;
            }

            private void EmitArgLoad(int index)
            {
                switch (index)
                {
                    case 0:
                        IL.Emit(OpCodes.Ldarg_0);
                        return;
                    case 1:
                        IL.Emit(OpCodes.Ldarg_1);
                        return;
                    case 2:
                        IL.Emit(OpCodes.Ldarg_2);
                        return;
                    case 3:
                        IL.Emit(OpCodes.Ldarg_3);
                        return;
                    default:
                        break;
                }
                if (index <= Byte.MaxValue)
                {
                    IL.Emit(OpCodes.Ldarg_S, (Byte) index);
                }
                else
                {
                    IL.Emit(OpCodes.Ldarg, index);
                }
            }

            private void EmitArgStore(int index)
            {
                if (index <= Byte.MaxValue)
                {
                    IL.Emit(OpCodes.Starg_S, (Byte) index);
                }
                else
                {
                    IL.Emit(OpCodes.Starg, index);
                }
            }

            private void EmitLocalLoad(int index)
            {
                switch (index)
                {
                    case 0:
                        IL.Emit(OpCodes.Ldloc_0);
                        return;
                    case 1:
                        IL.Emit(OpCodes.Ldloc_1);
                        return;
                    case 2:
                        IL.Emit(OpCodes.Ldloc_2);
                        return;
                    case 3:
                        IL.Emit(OpCodes.Ldloc_3);
                        return;
                    default:
                        break;
                }
                if (index <= Byte.MaxValue)
                {
                    IL.Emit(OpCodes.Ldloc_S, (Byte) index);
                }
                else
                {
                    IL.Emit(OpCodes.Ldloc, index);
                }
            }

            private void EmitLocalStore(int index)
            {
                switch (index)
                {
                    case 0:
                        IL.Emit(OpCodes.Stloc_0);
                        return;
                    case 1:
                        IL.Emit(OpCodes.Stloc_1);
                        return;
                    case 2:
                        IL.Emit(OpCodes.Stloc_2);
                        return;
                    case 3:
                        IL.Emit(OpCodes.Stloc_3);
                        return;
                    default:
                        break;
                }
                if (index <= Byte.MaxValue)
                {
                    IL.Emit(OpCodes.Stloc_S, (Byte) index);
                }
                else
                {
                    IL.Emit(OpCodes.Stloc, index);
                }
            }
        }
    }
}
