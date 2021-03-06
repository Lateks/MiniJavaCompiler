﻿using MiniJavaCompiler.Support.SymbolTable.Types;
using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    // The base class for method and variable declarations.
    public abstract class Declaration : SyntaxElement
    {
        public string Name { get; private set; }
        public string TypeName { get; private set; }
        public bool IsArray { get; private set; }
        public IType Type { get; set; }

        protected Declaration(string name, string type, bool isArray,
            int row, int col)
            : base(row, col)
        {
            Name = name;
            TypeName = type;
            IsArray = isArray;
        }
    }
}
