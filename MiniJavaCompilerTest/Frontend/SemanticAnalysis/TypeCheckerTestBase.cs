using System.IO;
using MiniJavaCompiler.FrontEnd.LexicalAnalysis;
using MiniJavaCompiler.FrontEnd.SemanticAnalysis;
using MiniJavaCompiler.FrontEnd.SyntaxAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.Support.SymbolTable;
using NUnit.Framework;

namespace MiniJavaCompilerTest.FrontEndTest.SemanticAnalysis
{
    public partial class TypeCheckerTest
    {
        public static SemanticsChecker SetUpTypeAndReferenceChecker(string program, out IErrorReporter errorLog)
        {
            var reader = new StringReader(program);
            var scanner = new MiniJavaScanner(reader);
            var errors = new ErrorLogger();
            var parser = new Parser(scanner, errors, true);
            Program syntaxTree = parser.Parse();
            reader.Close();
            Assert.That(errors.Errors, Is.Empty);

            var symbolTableBuilder = new SymbolTableBuilder(syntaxTree, errors);
            Assert.That(errors.Errors, Is.Empty);

            SymbolTable symbolTable = null;
            Assert.DoesNotThrow(() => symbolTable = symbolTableBuilder.BuildSymbolTable());
            errorLog = new ErrorLogger();

            return new SemanticsChecker(syntaxTree, symbolTable, errorLog);
        }
    }
}
