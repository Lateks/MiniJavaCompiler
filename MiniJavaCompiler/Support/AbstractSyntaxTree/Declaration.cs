using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    // The base class for method and variable declarations.
    public abstract class Declaration : SyntaxElement
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public bool IsArray { get; private set; }

        protected Declaration(string name, string type, bool isArray,
            int row, int col)
            : base(row, col)
        {
            Name = name;
            Type = type;
            IsArray = isArray;
        }
    }
}
