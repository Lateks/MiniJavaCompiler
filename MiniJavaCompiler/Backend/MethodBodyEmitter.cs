using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Scopes;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MiniJavaCompiler.BackEnd
{
    public partial class CodeGenerator
    {
        // Optimizes and emits the code for method bodies.
        private class MethodBodyEmitter
        {
            private MethodBuilder _method;
            private List<Tuple<OpCode?, object>> _methodBody;

            // Does not list the short form jump codes (e.g. Br_S)
            // because I did not use them.
            private static OpCode[] jumpCodes = new OpCode[] {
                OpCodes.Br, OpCodes.Brfalse, OpCodes.Brtrue,
                OpCodes.Br, OpCodes.Beq, OpCodes.Bge,
                OpCodes.Bgt, OpCodes.Ble, OpCodes.Blt
            };

            public MethodBodyEmitter(MethodBuilder methodBldr, List<Tuple<OpCode?, object>> methodBody)
            {
                _method = methodBldr;
                _methodBody = methodBody;
            }

            public void OptimizeAndEmitMethodBody()
            {
                OptimizeMethodBody();
                EmitMethodBody();
            }

            public void EmitMethodBody()
            {
                var IL = _method.GetILGenerator();
                foreach (var instruction in _methodBody)
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
                            else if (instruction.Item2 is short)
                            {
                                IL.Emit(opcode, (short)instruction.Item2);
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
                for (int i = 0; i < _methodBody.Count; i++)
                {
                    if (!_methodBody[i].Item1.HasValue ||
                        !jumpCodes.Contains(_methodBody[i].Item1.Value))
                    {
                        continue;
                    }
                    if (_methodBody[i].Item1.Value == OpCodes.Brtrue ||
                        _methodBody[i].Item1.Value == OpCodes.Brfalse)
                    {
                        i += MergeNotWithJump(i);
                        i += MergeComparisonWithJump(i);
                    }
                    OptimizeJump(i);
                }
            }
            
            // Does not remove any instructions from the method body,
            // only streamlines jumps by eliminating useless unconditional
            // jumps. E.g. when the else branch of an if statement
            // begins with a while loop, the if statement can use e.g.
            // brfalse to jump straight to the while loop condition
            // instead of jumping to the intermediate unconditional jump
            // point first.
            private void OptimizeJump(int i)
            {
                var jumpLabel = (Label) _methodBody[i].Item2;
                int jumpLabelIndex = -1;
                for (int j = 0; j < _methodBody.Count; j++)
                {
                    if (!_methodBody[j].Item1.HasValue &&
                        ((Label) _methodBody[j].Item2) == jumpLabel)
                    {
                        jumpLabelIndex = j;
                        break;
                    }
                }
                if (jumpLabelIndex >= 0 &&
                    jumpLabelIndex < _methodBody.Count - 1 &&
                    _methodBody[jumpLabelIndex + 1].Item1.HasValue &&
                    _methodBody[jumpLabelIndex + 1].Item1 == OpCodes.Br)
                {
                    var newLabel = _methodBody[jumpLabelIndex + 1].Item2;
                    _methodBody[i] = Tuple.Create(_methodBody[i].Item1, newLabel);
                }
            }

            // The return value indicates change in method body length.
            private int MergeComparisonWithJump(int i)
            {
                if (i == 0) return 0;

                OpCode? replacementOpcode = null;
                if (_methodBody[i - 1].Item1.HasValue)
                {
                    if (_methodBody[i].Item1.Value == OpCodes.Brtrue)
                    {
                        if (_methodBody[i - 1].Item1.Value == OpCodes.Ceq)
                        {
                            replacementOpcode = OpCodes.Beq;
                        }
                        else if (_methodBody[i - 1].Item1.Value == OpCodes.Clt)
                        {
                            replacementOpcode = OpCodes.Blt;
                        }
                        else if (_methodBody[i - 1].Item1.Value == OpCodes.Cgt)
                        {
                            replacementOpcode = OpCodes.Bgt;
                        }
                    }
                    else
                    {
                        if (_methodBody[i - 1].Item1.Value == OpCodes.Clt)
                        {
                            replacementOpcode = OpCodes.Bge;
                        }
                        else if (_methodBody[i - 1].Item1.Value == OpCodes.Cgt)
                        {
                            replacementOpcode = OpCodes.Ble;
                        }
                    }
                }
                if (replacementOpcode.HasValue)
                {
                    var replacementInstruction = Tuple.Create<OpCode?, object>(replacementOpcode, _methodBody[i].Item2);
                    _methodBody.RemoveAt(i);
                    _methodBody.RemoveAt(i - 1);
                    _methodBody.Insert(i - 1, replacementInstruction);
                    return -1;
                }
                return 0;
            }

            // The return value indicates change in method body length.
            private int MergeNotWithJump(int i)
            {
                if (i <= 1) return 0;

                OpCode? newOpcode = null;
                if (_methodBody[i - 2].Item1.HasValue &&
                    _methodBody[i - 2].Item1.Value == OpCodes.Ldc_I4_0 &&
                    _methodBody[i - 1].Item1.HasValue &&
                    _methodBody[i - 1].Item1.Value == OpCodes.Ceq)
                {
                    if (_methodBody[i].Item1.Value == OpCodes.Brtrue)
                    {
                        newOpcode = OpCodes.Brfalse;
                    }
                    else
                    {
                        newOpcode = OpCodes.Brtrue;
                    }
                }

                if (newOpcode.HasValue)
                {
                    var jumpLabel = _methodBody[i].Item2;
                    _methodBody.RemoveAt(i);
                    _methodBody.RemoveAt(i - 1);
                    _methodBody.RemoveAt(i - 2);
                    _methodBody.Insert(i - 2, Tuple.Create<OpCode?, object>(newOpcode, jumpLabel));
                    return -2;
                }
                return 0;
            }
        }
    }
}
