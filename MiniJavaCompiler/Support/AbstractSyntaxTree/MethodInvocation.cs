﻿using System;
using System.Collections.Generic;
using IType = MiniJavaCompiler.Support.SymbolTable.Types.IType;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class MethodInvocation : SyntaxElement, IStatement, IExpression
    {
        public IExpression MethodOwner { get; private set; }
        public string MethodName { get; private set; }
        public List<IExpression> CallParameters { get; private set; }
        public IType Type { get; set; }
        public MethodDeclaration ReferencedMethod { get; set; }
        public bool UsedAsStatement { get; set; }

        public MethodInvocation(IExpression methodOwner, string methodName,
            List<IExpression> callParameters, int row, int col)
            : base(row, col)
        {
            MethodOwner = methodOwner;
            MethodName = methodName;
            CallParameters = callParameters;
            UsedAsStatement = false;
        }

        public override void Accept(INodeVisitor visitor)
        {
            MethodOwner.Accept(visitor);
            foreach (var expr in CallParameters)
            {
                expr.Accept(visitor);
            }
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "method invocation";
        }
    }
}