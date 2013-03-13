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
        private class InstructionGenerator : INodeVisitor
        {
            private CodeGenerator _parent;
            private TypeBuilder _currentType;
            private MethodBuilder _currentMethod;
            private ILGenerator IL
            {
                get { return _currentMethod.GetILGenerator(); }
            }

            private static Dictionary<MiniJavaInfo.Operator, OpCode[]> operatorOpCodes =
                new Dictionary<MiniJavaInfo.Operator, OpCode[]>()
            {
                { MiniJavaInfo.Operator.Add, new OpCode[] { OpCodes.Add } },
                { MiniJavaInfo.Operator.Sub, new OpCode[] { OpCodes.Sub } },
                { MiniJavaInfo.Operator.Div, new OpCode[] { OpCodes.Div } },
                { MiniJavaInfo.Operator.Mul, new OpCode[] { OpCodes.Mul } },
                { MiniJavaInfo.Operator.Lt,  new OpCode[] { OpCodes.Clt } },
                { MiniJavaInfo.Operator.Gt,  new OpCode[] { OpCodes.Cgt } },
                { MiniJavaInfo.Operator.And, new OpCode[] { OpCodes.And } },
                { MiniJavaInfo.Operator.Or,  new OpCode[] { OpCodes.Or  } },
                { MiniJavaInfo.Operator.Eq,  new OpCode[] { OpCodes.Ceq } },
                { MiniJavaInfo.Operator.Mod, new OpCode[] { OpCodes.Rem } },
                { MiniJavaInfo.Operator.Not, new OpCode[] { OpCodes.Ldc_I4_0, OpCodes.Ceq } }
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
                TypeBuilder thisType = _parent._types[node.Name];
                _currentType = thisType;
            }

            public void Visit(VariableDeclaration node) { }

            public void Visit(MethodDeclaration node)
            {
                var sym = _parent._symbolTable.Scopes[node].ResolveMethod(node.Name);
                _currentMethod = _parent._methods[sym];
            }

            public void Visit(PrintStatement node)
            {
                IL.Emit(OpCodes.Call, GetPrintMethod<Int32>());
            }

            private static MethodInfo GetPrintMethod<T>()
            {
                return typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(T) });
            }

            public void Visit(ReturnStatement node)
            {
                IL.Emit(OpCodes.Ret);
            }

            public void Visit(BlockStatement node)
            {
                if (node.Label.HasValue)
                {
                    IL.MarkLabel(node.Label.Value);
                }
            }

            public void Visit(AssertStatement node)
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

            public void Visit(AssignmentStatement node)
            {   // The left hand side of an assignment must be either
                // a variable reference or an array indexing expression.
                if (node.LeftHandSide is VariableReferenceExpression)
                {
                    var reference = (VariableReferenceExpression)node.LeftHandSide;
                    var variable = _parent._symbolTable.Scopes[reference].ResolveVariable(reference.Name);
                    var decl = (VariableDeclaration)_parent._symbolTable.Declarations[variable];
                    switch (decl.VariableKind)
                    {
                        case VariableDeclaration.Kind.Class:
                            IL.Emit(OpCodes.Stfld, _parent._fields[variable]);
                            break;
                        case VariableDeclaration.Kind.Local:
                            IL.Emit(OpCodes.Stloc, decl.LocalIndex);
                            break;
                        case VariableDeclaration.Kind.Formal:
                            IL.Emit(OpCodes.Starg, GetParameterIndex(decl, _currentMethod));
                            break;
                    }
                }
                else
                {   // The address to store to should be on the top of the stack just
                    // under the object being stored.
                    var rhsType = node.RightHandSide.Type;
                    if (MiniJavaInfo.IsBuiltInType(rhsType.Name))
                    {
                        IL.Emit(OpCodes.Stelem_I4);
                    }
                    else
                    {
                        IL.Emit(OpCodes.Stelem_Ref);
                    }
                }
            }

            public void VisitAfterCondition(IfStatement node)
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

            public void VisitAfterThenBranch(IfStatement node)
            {
                IL.Emit(OpCodes.Br, node.ExitLabel);
            }

            public void Exit(IfStatement node)
            {
                IL.MarkLabel(node.ExitLabel);
            }

            public void Visit(WhileStatement node)
            {
                Label test = IL.DefineLabel();
                node.ConditionLabel = test;
                node.LoopBody.Label = IL.DefineLabel();
                IL.Emit(OpCodes.Br, test); // unconditional branch to loop test
            }

            public void VisitAfterBody(WhileStatement node)
            {
                IL.MarkLabel(node.ConditionLabel);
            }

            public void Exit(WhileStatement node)
            {
                IL.Emit(OpCodes.Brtrue, node.LoopBody.Label.Value);
            }

            public void Visit(MethodInvocation node)
            {
                if (node.MethodOwner.Type is ArrayType)
                {
                    IL.Emit(OpCodes.Ldlen);
                }
                else
                {   // TODO: check call parameters
                    var methodScope = _parent._symbolTable.ResolveTypeName(node.MethodOwner.Type.Name).Scope;
                    var calledMethod = _parent._methods[methodScope.ResolveMethod(node.MethodName)];
                    _currentMethod.GetILGenerator().Emit(OpCodes.Call, calledMethod);
                }
            }

            public void Visit(InstanceCreationExpression node)
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

            public void Visit(UnaryOperatorExpression node)
            {
                EmitOperator(node.Operator);
            }

            public void Visit(BinaryOperatorExpression node)
            {
                EmitOperator(node.Operator);
            }

            private void EmitOperator(MiniJavaInfo.Operator op)
            {
                foreach (var opcode in operatorOpCodes[op])
                {
                    IL.Emit(opcode);
                }
            }

            public void Visit(BooleanLiteralExpression node)
            {
                IL.Emit(OpCodes.Ldc_I4, node.Value ? 1 : 0);
            }

            public void Visit(ThisExpression node)
            {
                IL.Emit(OpCodes.Ldarg_0);
            }

            public void Visit(ArrayIndexingExpression node)
            {
                if (node.UsedAsAddress) return; // no need to load anything, index is already on the stack?
                if (MiniJavaInfo.IsBuiltInType(node.Type.Name))
                {
                    IL.Emit(OpCodes.Ldelem_I4);
                }
                else
                {
                    IL.Emit(OpCodes.Ldelem_Ref);
                }
            }


            public void Visit(VariableReferenceExpression node)
            {
                var variable = _parent._symbolTable.Scopes[node].ResolveVariable(node.Name);
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
                        IL.Emit(OpCodes.Ldarg, GetParameterIndex(definition, _currentMethod));
                        break;
                    case VariableDeclaration.Kind.Local:
                        IL.Emit(OpCodes.Ldloc, definition.LocalIndex);
                        break;
                }
            }

            public void Visit(IntegerLiteralExpression node)
            {
                IL.Emit(OpCodes.Ldc_I4, node.IntValue);
            }

            public void Exit(ClassDeclaration node)
            {
                _currentType = null;
            }

            public void Exit(MethodDeclaration node)
            {
                // Emit the return statement for a void method.
                if (!(node.MethodBody.Last() is ReturnStatement))
                {
                    IL.Emit(OpCodes.Ret);
                }
                _currentMethod = null;
            }

            public void Exit(BlockStatement node) { }
        }
    }
}
