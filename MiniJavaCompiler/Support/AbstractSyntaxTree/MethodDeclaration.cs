using MiniJavaCompiler.Support.SymbolTable.Symbols;
using MiniJavaCompiler.Support.SymbolTable.Types;
using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class MethodDeclaration : Declaration
    {
        public bool IsEntryPoint { get; private set; }
        public List<VariableDeclaration> Formals { get; private set; }
        public List<IStatement> MethodBody { get; private set; }
        public bool IsStatic { get; private set; }
        public TypeSymbol DeclaringType { get; set; }
        public MethodSymbol Symbol { get; set; }

        public MethodDeclaration(string name, string type, bool returnTypeIsArray,
            List<VariableDeclaration> formals, List<IStatement> methodBody,
            int row, int col, bool isStatic = false)
            : base(name, type, returnTypeIsArray, row, col)
        {
            Formals = formals;
            MethodBody = methodBody;
            IsStatic = isStatic;
        }

        public static MethodDeclaration CreateMainMethodDeclaration(List<IStatement> methodBody, int row, int col)
        {
            var method = new MethodDeclaration(MiniJavaInfo.MainMethodIdent, MiniJavaInfo.VoidType, false,
              new List<VariableDeclaration>(), methodBody, row, col, true);
            method.IsEntryPoint = true;
            return method;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
            foreach (var decl in Formals)
            {
                decl.Accept(visitor);
            }
            foreach (var statement in MethodBody)
            {
                statement.Accept(visitor);
            }
            visitor.Exit(this);
        }
    }
}
