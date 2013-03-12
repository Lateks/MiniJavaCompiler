using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MiniJavaCompiler.FrontEnd.SemanticAnalysis
{
    /* This class checks that types, references (variables, methods etc.),
     * return statements and method invocation arguments are acceptable.
     * 
     * Does not produce any output except for error message output to
     * the error reporter (if applicable).
     */ 
    public partial class SemanticsChecker
    {
        private readonly SymbolTable _symbolTable;
        private readonly Program _programRoot;
        private readonly IErrorReporter _errors;

        public SemanticsChecker(Program program, SymbolTable symbolTable, IErrorReporter errorReporter)
        {
            _programRoot = program;
            _symbolTable = symbolTable;
            _errors = errorReporter;
        }

        // Throws an exception at the end of analysis if there is a problem
        // with types or references.
        public void RunCheck()
        {
            var success = true;
            success &= new ReferenceChecker(this).RunCheck();
            success &= new TypeChecker(this).RunCheck();
            if (!success)
            {
                throw new CompilationError();
            }
        }

    }
}