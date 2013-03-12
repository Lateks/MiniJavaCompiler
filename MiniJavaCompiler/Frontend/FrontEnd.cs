using System;
using System.Collections.Generic;
using System.IO;
using MiniJavaCompiler.FrontEnd.LexicalAnalysis;
using MiniJavaCompiler.FrontEnd.SemanticAnalysis;
using MiniJavaCompiler.FrontEnd.SyntaxAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;

namespace MiniJavaCompiler.FrontEnd
{
    // Runs the whole front end pipeline. If program analysis fails, the
    // caller can ask for a list of errors.
    public class FrontEnd
    {
        private readonly IErrorReporter _errorLog;
        private readonly TextReader _program;

        public FrontEnd(TextReader programText)
        {
            _errorLog = new ErrorLogger();
            _program = programText;
        }

        // In case of analysis failure, this can be used to get a hold of the
        // list of error messages.
        public List<ErrorMessage> GetErrors()
        {
            return _errorLog.Errors;
        }

        // Returns a boolean value indicating program analysis success (true) or failure (false).
        public bool TryProgramAnalysis(out Program abstractSyntaxTree, out SymbolTable symbolTable)
        {
            abstractSyntaxTree = ConstructAbstractSyntaxTree();
            if (abstractSyntaxTree == null)
            {
                symbolTable = null;
                return false;
            }
            symbolTable = ConstructSymbolTableAndCheckProgram(abstractSyntaxTree);
            return symbolTable != null;
        }

        /* Performs lexical and semantic analysis. Returns null if either phase fails.
         * 
         * Syntax analysis is always attempted regardless of lexical errors.
         * All errors are logged in the error log.
         */
        private Program ConstructAbstractSyntaxTree()
        {
            var scanner = new MiniJavaScanner(_program);
            var parser = new Parser(scanner, _errorLog);
            try
            {
                return parser.Parse();
            }
            catch (CompilationError)
            {
                return null;
            }
        }

        /* Performs semantic analysis on the program. Returns null if any of the phases
         * fails. If one phase fails, analysis will not continue, but internal recovery
         * is attempted within each phase of analysis.
         * 
         * The phases are:
         * 1. Passing through class declarations once to get a list of possible types.
         * 2. Building a symbol table.
         * 3. Checking that types and references are valid.
         * 
         * All errors are logged in the error log.
         */
        private SymbolTable ConstructSymbolTableAndCheckProgram(Program abstractSyntaxTree)
        {
            try
            {
                var symbolTable = new SymbolTableBuilder(abstractSyntaxTree, _errorLog).BuildSymbolTable();
                new SemanticsChecker(abstractSyntaxTree, symbolTable, _errorLog).RunCheck();
                return symbolTable;
            }
            catch (CompilationError)
            {
                return null;
            }
        }
    }
}
