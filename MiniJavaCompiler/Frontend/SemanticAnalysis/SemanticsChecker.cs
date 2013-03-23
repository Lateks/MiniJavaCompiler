using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.SymbolTable.Types;
using MiniJavaCompiler.Support.SymbolTable.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiniJavaCompiler.Support.SymbolTable.Scopes;

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
        private readonly GlobalScope _symbolTable;
        private readonly Program _programRoot;
        private readonly IErrorReporter _errors;

        public SemanticsChecker(Program program, IErrorReporter errorReporter)
        {
            if (program.Scope == null)
                throw new ArgumentException("Global scope is undefined.");
            _programRoot = program;
            _symbolTable = (GlobalScope)program.Scope;
            _errors = errorReporter;
        }

        // Throws an exception at the end of analysis if there is a problem
        // with types or references.
        public bool RunCheck()
        {
            var success = true;
            success &= new ReferenceChecker(this).RunCheck();
            success &= new TypeChecker(this).RunCheck();
            return success;
        }

    }
}