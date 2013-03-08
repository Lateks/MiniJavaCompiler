using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public class ClassDeclaration : SyntaxElement
    {
        public bool IsMainClass { get; private set; }
        public string Name { get; private set; }
        public string InheritedClass { get; private set; }
        public List<Declaration> Declarations { get; private set; }

        public ClassDeclaration(string name, string inherited,
            List<Declaration> declarations, int row, int col)
            : base(row, col)
        {
            Name = name;
            InheritedClass = inherited;
            Declarations = declarations;
        }

        public static ClassDeclaration CreateMainClassDeclaration(string name,
            MethodDeclaration mainMethod, int row, int col)
        {
            if (!mainMethod.IsEntryPoint)
            {
                throw new ArgumentException("Illegal main method declaration, the program has no entry point.");
            }
            var mainClass = new ClassDeclaration(name, null,
                new List<Declaration> { mainMethod }, row, col);
            mainClass.IsMainClass = true;
            return mainClass;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
            foreach (Declaration decl in Declarations)
            {
                decl.Accept(visitor);
            }
            visitor.Exit(this);
        }
    }
}