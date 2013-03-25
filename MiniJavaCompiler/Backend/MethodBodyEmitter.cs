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
                    if (!_methodBody[i].Item1.HasValue)
                    {
                        continue;
                    }
                    MergeNotWithJump(i);
                    MergeComparisonWithJump(i);
                }
            }

            private void MergeComparisonWithJump(int i)
            {
                if (i == 0 || !(_methodBody[i].Item1.Value == OpCodes.Brtrue ||
                    _methodBody[i].Item1.Value == OpCodes.Brfalse))
                {
                    return;
                }

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
                }
            }

            private void MergeNotWithJump(int i)
            {
                if (i <= 1 || !(_methodBody[i].Item1.Value == OpCodes.Brtrue ||
                    _methodBody[i].Item1.Value == OpCodes.Brfalse))
                    return;

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
                }
            }
        }
    }
}
