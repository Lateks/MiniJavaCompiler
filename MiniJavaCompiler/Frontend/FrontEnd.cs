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
            _errorLog = ErrorReporterFactory.CreateErrorLogger();
            _program = programText;
        }

        // In case of analysis failure, this can be used to get a hold of the
        // list of error messages. Errors are returned in order of appearance
        // in code.
        public List<ErrorMessage> GetErrors()
        {
            _errorLog.Errors.Sort(ErrorMessage.CompareByLocation);
            return _errorLog.Errors;
        }

        // Returns a boolean value indicating program analysis success (true) or failure (false).
        public bool TryProgramAnalysis(out Program abstractSyntaxTree)
        {
            abstractSyntaxTree = ConstructAbstractSyntaxTree();
            if (abstractSyntaxTree == null)
            {
                return false;
            }
            return ConstructSymbolTableAndCheckProgram(abstractSyntaxTree);
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
            Program program;
            if (parser.TryParse(out program))
            {
                return program;
            }
            else
            {
                return null;
            }
        }

        /* Performs semantic analysis on the program. Returns null if any of the phases
         * fails. If a fatal error is encountered in one phase, analysis will not continue,
         * but internal recovery is attempted within each phase of analysis.
         * 
         * The phases are:
         * 1. Building a symbol table. This checks that there are no name clashes in
         *    type declarations or cyclic dependencies. Cyclic dependencies are treated
         *    as fatal errors and will lead to type and reference checks not being run.
         *    If name clashes are found, the compiler will not even proceed to check
         *    cyclic dependencies. This is also treated as a fatal error.
         * 2. Semantic checks. This phase checks first the validity of name references
         *    and then performs type checks on all expressions. References to possibly
         *    uninitialized variables are also detected.
         * 
         * All errors are logged in the error log.
         */
        private bool ConstructSymbolTableAndCheckProgram(Program abstractSyntaxTree)
        {
            var symbolTableSuccess = new SymbolTableBuilder(abstractSyntaxTree, _errorLog).BuildSymbolTable();
            bool semanticCheckSuccess = true;
            if (symbolTableSuccess != SymbolTableBuilder.ExitCode.FatalError)
            {
                semanticCheckSuccess = new SemanticsChecker(abstractSyntaxTree, _errorLog).RunCheck();
            }
            return (symbolTableSuccess == SymbolTableBuilder.ExitCode.Success) && semanticCheckSuccess;
        }
    }
}
