using MiniJavaCompiler.Support.SymbolTable.Scopes;
using System;
using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    // This is the root node of the syntax tree, representing a MiniJava program.
    public class Program : ISyntaxTreeNode
    {
        public ClassDeclaration MainClass { get; private set; }
        public List<ClassDeclaration> Classes { get; private set; }
        public IScope Scope { get; set; }

        public Program(ClassDeclaration mainClass,
                       List<ClassDeclaration> classDeclarations)
        {
            if (mainClass != null && !mainClass.IsMainClass)
            {
                throw new ArgumentException("Illegal main class declaration, the program has no entry point.");
            }
            MainClass = mainClass;
            Classes = classDeclarations;
        }

        public void Accept(INodeVisitor visitor)
        {
            MainClass.Accept(visitor);
            foreach (var aClass in Classes)
            {
                aClass.Accept(visitor);
            }
            visitor.Visit(this);
        }
    }
}