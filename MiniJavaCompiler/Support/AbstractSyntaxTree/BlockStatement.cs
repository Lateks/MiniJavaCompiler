﻿using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class BlockStatement : SyntaxElement, IStatement
    {
        public List<IStatement> Statements { get; private set; }
        public System.Reflection.Emit.Label? Label { get; set; }

        public BlockStatement(List<IStatement> statements, int row, int col)
            : base(row, col)
        {
            Statements = statements;
            Label = null;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
            foreach (var statement in Statements)
            {
                statement.Accept(visitor);
            }
            visitor.Exit(this);
        }
    }
}