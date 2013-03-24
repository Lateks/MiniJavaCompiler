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
            MethodBuilder _currentMethod;
            private readonly Dictionary<MethodBuilder, List<Tuple<OpCode?, object>>> _methodBodies;
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
                _methodBodies = new Dictionary<MethodBuilder, List<Tuple<OpCode?, object>>>();
            }

            public void GenerateInstructions()
            {
                _parent._astRoot.Accept(this);
            }

            public override void Visit(ClassDeclaration node)
            {
                TypeBuilder thisType = _parent._types[node.Name];
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
                _currentMethod = _parent._methods[node.Symbol];
                _methodBodies[_currentMethod] = new List<Tuple<OpCode?, object>>();
            }

            public override void Visit(PrintStatement node)
            {
                AddInstruction(OpCodes.Call, GetPrintMethod<Int32>());
            }

            private static MethodInfo GetPrintMethod<T>()
            {
                return typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(T) });
            }

            private void AddInstruction(OpCode? instr, Object param = null)
            {
                _methodBodies[_currentMethod].Add(
                    Tuple.Create<OpCode?, object>(instr, param));
            }

            public override void Visit(ReturnStatement node)
            {
                AddInstruction(OpCodes.Ret);
            }

            public override void Visit(BlockStatement node)
            {
                if (node.Label.HasValue)
                {
                    AddInstruction(null, node.Label.Value);
                }
            }

            public override void Visit(AssertStatement node)
            {
                var jumpLabel = IL.DefineLabel();
                AddInstruction(OpCodes.Brtrue, jumpLabel); // assertion ok

                var errStr = String.Format("Exception occurred: AssertionError at {0}.{1}({2},{3})",
                    _currentMethod.DeclaringType.Name, _currentMethod.Name, node.Row, node.Col);
                AddInstruction(OpCodes.Ldstr, errStr);
                AddInstruction(OpCodes.Call, GetPrintMethod<String>());

                MethodInfo exitMethod = typeof(System.Environment).GetMethod(
                    "Exit", new Type[] { typeof(Int32) });
                AddInstruction(OpCodes.Ldc_I4_1); // failure status
                AddInstruction(OpCodes.Call, exitMethod);

                AddInstruction(null, jumpLabel);
            }

            public override void Visit(AssignmentStatement node)
            {   // The left hand side of an assignment must be either
                // a variable reference or an array indexing expression.
                if (node.LeftHandSide is VariableReferenceExpression)
                {
                    var reference = (VariableReferenceExpression)node.LeftHandSide;
                    var variable = reference.Scope.ResolveVariable(reference.Name);
                    var decl = (VariableDeclaration)variable.Declaration;
                    switch (decl.VariableKind)
                    {
                        case VariableDeclaration.Kind.Class:
                            AddInstruction(OpCodes.Stfld, _parent._fields[variable]);
                            break;
                        case VariableDeclaration.Kind.Local:
                            AddLocalStoreInstr(decl.LocalIndex);
                            break;
                        case VariableDeclaration.Kind.Formal:
                            AddArgStoreInstr(GetParameterIndex(decl, _currentMethod));
                            break;
                    }
                }
                else
                {   // The address to store to should be on the top of the stack just
                    // under the object being stored.
                    var rhsType = node.RightHandSide.Type;
                    if (rhsType.Name == MiniJavaInfo.IntType)
                    {
                        AddInstruction(OpCodes.Stelem_I4);
                    }
                    else if (rhsType.Name == MiniJavaInfo.BoolType)
                    {
                        AddInstruction(OpCodes.Stelem_I1);
                    }
                    else
                    {
                        AddInstruction(OpCodes.Stelem_Ref);
                    }
                }
            }

            public override void VisitAfterCondition(IfStatement node)
            {
                node.ExitLabel = IL.DefineLabel();
                if (node.ElseBranch != null)
                {
                    node.ElseBranch.Label = IL.DefineLabel();
                    AddInstruction(OpCodes.Brfalse, node.ElseBranch.Label.Value);
                }
                else
                {
                    AddInstruction(OpCodes.Brfalse, node.ExitLabel);
                }
            }

            public override void VisitAfterThenBranch(IfStatement node)
            {
                AddInstruction(OpCodes.Br, node.ExitLabel);
            }

            public override void Exit(IfStatement node)
            {
                AddInstruction(null, node.ExitLabel);
            }

            public override void Visit(WhileStatement node)
            {
                Label test = IL.DefineLabel();
                node.ConditionLabel = test;
                node.LoopBody.Label = IL.DefineLabel();
                AddInstruction(OpCodes.Br, test); // unconditional branch to loop test
            }

            public override void VisitAfterBody(WhileStatement node)
            {
                AddInstruction(null, node.ConditionLabel);
            }

            public override void Exit(WhileStatement node)
            {
                AddInstruction(OpCodes.Brtrue, node.LoopBody.Label.Value);
            }

            public override void Visit(MethodInvocation node)
            {
                if (node.MethodOwner.Type is ArrayType)
                {
                    AddInstruction(OpCodes.Ldlen);
                }
                else
                {
                    var calledMethod = _parent._methods[node.ReferencedMethod.Symbol];
                    AddInstruction(OpCodes.Callvirt, calledMethod);
                }
            }

            public override void Visit(InstanceCreationExpression node)
            {
                Type type = _parent.BuildType(node.CreatedTypeName, false);
                if (node.IsArrayCreation)
                {   // arraysize is on top of the stack
                    AddInstruction(OpCodes.Newarr, type);
                }
                else
                {
                    AddInstruction(OpCodes.Newobj, _parent._constructors[type]);
                }
            }

            public override void Visit(UnaryOperatorExpression node)
            {
                AddOperatorInstr(node.Operator);
            }

            public override void VisitAfterLHS(BinaryOperatorExpression node)
            {
                if (!MiniJavaInfo.IsLogicalOperator(node.Operator))
                    return;
                // The first jump code required for boolean operator short circuit.
                var jumpLabel = IL.DefineLabel();
                node.AfterLabel = jumpLabel;
                switch (node.Operator)
                {
                    case MiniJavaInfo.Operator.Or:
                        AddInstruction(OpCodes.Brtrue, jumpLabel);
                        break;
                    case MiniJavaInfo.Operator.And:
                        AddInstruction(OpCodes.Brfalse, jumpLabel);
                        break;
                    default:
                        break;
                }
            }

            public override void Visit(BinaryOperatorExpression node)
            {
                AddOperatorInstr(node.Operator);
                if (MiniJavaInfo.IsLogicalOperator(node.Operator))
                {   // The second jump code and jump labels required
                    // for boolean operator short circuit.
                    var rhsJumpLabel = IL.DefineLabel();
                    AddInstruction(OpCodes.Br, rhsJumpLabel);
                    AddInstruction(null, node.AfterLabel.Value);
                    var successCode = node.Operator == MiniJavaInfo.Operator.And ?
                        OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1;
                    AddInstruction(successCode);
                    AddInstruction(null, rhsJumpLabel); // We jump to this label if rhs was evaluated.
                }
            }

            private void AddOperatorInstr(MiniJavaInfo.Operator op)
            {
                foreach (var opcode in _operatorOpCodes[op])
                {
                    AddInstruction(opcode);
                }
            }

            public override void Visit(BooleanLiteralExpression node)
            {
                AddInstruction(node.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            }

            public override void Visit(ThisExpression node)
            {
                AddInstruction(OpCodes.Ldarg_0);
            }

            public override void Visit(ArrayIndexingExpression node)
            {
                if (node.UsedAsAddress) return; // no need to load anything, index is already on the stack
                if (node.Type.Name == MiniJavaInfo.IntType)
                {
                    AddInstruction(OpCodes.Ldelem_I4);
                }
                else if (node.Type.Name == MiniJavaInfo.BoolType)
                {
                    AddInstruction(OpCodes.Ldelem_I1);
                }
                else
                {
                    AddInstruction(OpCodes.Ldelem_Ref);
                }
            }


            public override void Visit(VariableReferenceExpression node)
            {
                var variable = node.Scope.ResolveVariable(node.Name);
                var definition = (VariableDeclaration)variable.Declaration;

                if (node.UsedAsAddress)
                {
                    if (definition.VariableKind == VariableDeclaration.Kind.Class)
                    {   // Load a "this" reference.
                        AddInstruction(OpCodes.Ldarg_0);
                    }
                    return;
                }

                switch (definition.VariableKind)
                {
                    case VariableDeclaration.Kind.Class:
                        AddInstruction(OpCodes.Ldarg_0);
                        AddInstruction(OpCodes.Ldfld, _parent._fields[variable]);
                        break;
                    case VariableDeclaration.Kind.Formal:
                        AddArgLoadInstr(GetParameterIndex(definition, _currentMethod));
                        break;
                    case VariableDeclaration.Kind.Local:
                        AddLocalLoadInstr(definition.LocalIndex);
                        break;
                }
            }

            public override void Visit(IntegerLiteralExpression node)
            {
                if (node.IntValue >= 0 && node.IntValue < _int32LoadOpcodes.Length)
                {
                    AddInstruction(_int32LoadOpcodes[node.IntValue]);
                }
                else if (node.IntValue <= SByte.MaxValue)
                {
                    AddInstruction(OpCodes.Ldc_I4_S, Convert.ToSByte(node.IntValue));
                }
                else
                {
                    AddInstruction(OpCodes.Ldc_I4, node.IntValue);
                }
            }

            public override void Exit(MethodDeclaration node)
            {
                // Emit the return statement for a void method.
                if (node.MethodBody.Count == 0 ||
                    !(node.MethodBody.Last() is ReturnStatement))
                {
                    AddInstruction(OpCodes.Ret);
                }
                OptimizeMethodBody();
                EmitMethodBody();
                _currentMethod = null;
            }

            private void EmitMethodBody()
            {
                var methodBody = _methodBodies[_currentMethod];
                foreach (var instruction in methodBody)
                {
                    if (instruction.Item1.HasValue)
                    {
                        if (instruction.Item2 != null)
                        {
                            var opcode = instruction.Item1.Value;
                            if (instruction.Item2 is sbyte)
                            {
                                IL.Emit(opcode, (sbyte)instruction.Item2);
                            }
                            else if (instruction.Item2 is byte)
                            {
                                IL.Emit(opcode, (byte)instruction.Item2);
                            }
                            else if (instruction.Item2 is int)
                            {
                                IL.Emit(opcode, (int)instruction.Item2);
                            }
                            else if (instruction.Item2 is ConstructorInfo)
                            {
                                IL.Emit(opcode, (ConstructorInfo)instruction.Item2);
                            }
                            else if (instruction.Item2 is MethodInfo)
                            {
                                IL.Emit(opcode, (MethodInfo)instruction.Item2);
                            }
                            else if (instruction.Item2 is Label)
                            {
                                IL.Emit(opcode, (Label)instruction.Item2);
                            }
                            else if (instruction.Item2 is string)
                            {
                                IL.Emit(opcode, (string)instruction.Item2);
                            }
                            else if (instruction.Item2 is FieldInfo)
                            {
                                IL.Emit(opcode, (FieldInfo)instruction.Item2);
                            }
                            else if (instruction.Item2 is TypeInfo)
                            {
                                IL.Emit(opcode, (TypeInfo)instruction.Item2);
                            }
                            else
                            {
                                throw new ArgumentException("Unknown parameter type for opcode.");
                            }
                        }
                        else
                        {
                            IL.Emit(instruction.Item1.Value);
                        }
                    }
                    else if (instruction.Item2 is Label)
                    {
                        IL.MarkLabel((Label)instruction.Item2);
                    }
                    else
                    {
                        throw new ArgumentException("Unknown instruction.");
                    }
                }
            }

            private void OptimizeMethodBody()
            {
                var methodBody = _methodBodies[_currentMethod];
                for (int i = 0; i < methodBody.Count; i++)
                {
                    if (!methodBody[i].Item1.HasValue)
                    {
                        continue;
                    }
                    MergeNotWithJump(methodBody, i);
                    MergeComparisonWithJump(methodBody, i);
                }
            }

            private static void MergeComparisonWithJump(List<Tuple<OpCode?, object>> methodBody, int i)
            {
                if (i == 0)
                    return;
                OpCode? replacementOpcode = null;
                if (methodBody[i - 1].Item1.HasValue)
                {
                    if (methodBody[i].Item1.Value == OpCodes.Brtrue)
                    {
                        if (methodBody[i - 1].Item1.Value == OpCodes.Ceq)
                        {
                            replacementOpcode = OpCodes.Beq;
                        }
                        else if (methodBody[i - 1].Item1.Value == OpCodes.Clt)
                        {
                            replacementOpcode = OpCodes.Blt;
                        }
                        else if (methodBody[i - 1].Item1.Value == OpCodes.Cgt)
                        {
                            replacementOpcode = OpCodes.Bgt;
                        }
                    }
                    else if (methodBody[i].Item1.Value == OpCodes.Brfalse)
                    {
                        if (methodBody[i - 1].Item1.Value == OpCodes.Clt)
                        {
                            replacementOpcode = OpCodes.Bge;
                        }
                        else if (methodBody[i - 1].Item1.Value == OpCodes.Cgt)
                        {
                            replacementOpcode = OpCodes.Ble;
                        }
                    }
                }
                if (replacementOpcode.HasValue)
                {
                    var replacementInstruction = Tuple.Create<OpCode?, object>(replacementOpcode, methodBody[i].Item2);
                    methodBody.RemoveAt(i);
                    methodBody.RemoveAt(i - 1);
                    methodBody.Insert(i - 1, replacementInstruction);
                }
            }

            private static void MergeNotWithJump(List<Tuple<OpCode?, object>> methodBody, int i)
            {
                if (i <= 1)
                    return;

                OpCode? newOpcode = null;
                if (methodBody[i - 2].Item1.HasValue &&
                    methodBody[i - 2].Item1.Value == OpCodes.Ldc_I4_0 &&
                    methodBody[i - 1].Item1.HasValue &&
                    methodBody[i - 1].Item1.Value == OpCodes.Ceq)
                {
                    if (methodBody[i].Item1.Value == OpCodes.Brtrue)
                    {
                        newOpcode = OpCodes.Brfalse;
                    }
                    else if (methodBody[i].Item1.Value == OpCodes.Brfalse)
                    {
                        newOpcode = OpCodes.Brtrue;
                    }
                }

                if (newOpcode.HasValue)
                {
                    var jumpLabel = methodBody[i].Item2;
                    methodBody.RemoveAt(i);
                    methodBody.RemoveAt(i - 1);
                    methodBody.RemoveAt(i - 2);
                    methodBody.Insert(i - 2, Tuple.Create<OpCode?, object>(newOpcode, jumpLabel));
                }
            }

            private void AddArgLoadInstr(int index)
            {
                switch (index)
                {
                    case 0:
                        AddInstruction(OpCodes.Ldarg_0);
                        return;
                    case 1:
                        AddInstruction(OpCodes.Ldarg_1);
                        return;
                    case 2:
                        AddInstruction(OpCodes.Ldarg_2);
                        return;
                    case 3:
                        AddInstruction(OpCodes.Ldarg_3);
                        return;
                    default:
                        break;
                }
                if (index <= Byte.MaxValue)
                {
                    AddInstruction(OpCodes.Ldarg_S, Convert.ToByte(index));
                }
                else
                {
                    AddInstruction(OpCodes.Ldarg, index);
                }
            }

            private void AddArgStoreInstr(int index)
            {
                if (index <= Byte.MaxValue)
                {
                    AddInstruction(OpCodes.Starg_S, Convert.ToByte(index));
                }
                else
                {
                    AddInstruction(OpCodes.Starg, index);
                }
            }

            private void AddLocalLoadInstr(int index)
            {
                switch (index)
                {
                    case 0:
                        AddInstruction(OpCodes.Ldloc_0);
                        return;
                    case 1:
                        AddInstruction(OpCodes.Ldloc_1);
                        return;
                    case 2:
                        AddInstruction(OpCodes.Ldloc_2);
                        return;
                    case 3:
                        AddInstruction(OpCodes.Ldloc_3);
                        return;
                    default:
                        break;
                }
                if (index <= Byte.MaxValue)
                {
                    AddInstruction(OpCodes.Ldloc_S, Convert.ToByte(index));
                }
                else
                {
                    AddInstruction(OpCodes.Ldloc, index);
                }
            }

            private void AddLocalStoreInstr(int index)
            {
                switch (index)
                {
                    case 0:
                        AddInstruction(OpCodes.Stloc_0);
                        return;
                    case 1:
                        AddInstruction(OpCodes.Stloc_1);
                        return;
                    case 2:
                        AddInstruction(OpCodes.Stloc_2);
                        return;
                    case 3:
                        AddInstruction(OpCodes.Stloc_3);
                        return;
                    default:
                        break;
                }
                if (index <= Byte.MaxValue)
                {
                    AddInstruction(OpCodes.Stloc_S, Convert.ToByte(index));
                }
                else
                {
                    AddInstruction(OpCodes.Stloc, index);
                }
            }
        }
    }
}
