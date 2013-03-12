using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MiniJavaCompiler.FrontEnd;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using MiniJavaCompiler.BackEnd;

namespace MiniJavaCompiler
{
    public class Compiler
    {
        private const string usage = "Usage: ./MiniJavaCompiler.exe path/to/source/file";

        static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine(usage);
                return;
            }
            StreamReader fileStream;
            try
            {
                fileStream = new StreamReader(args[0]);
            }
            catch (ArgumentException)
            {
                Console.WriteLine(usage);
                return;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(String.Format("Could not open file {0}: {1}.", args[0], e.Message));
                return;
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(String.Format("Could not open directory: {0}", e.Message));
                return;
            }
            catch (IOException e)
            {
                Console.WriteLine(String.Format("Problem reading file: {0}", e.Message));
                return;
            }

            SymbolTable symbolTable;
            Program abstractSyntaxTree;
            if (RunFrontEnd(args[0], fileStream, out symbolTable, out abstractSyntaxTree))
            {
                RunBackEnd(symbolTable, abstractSyntaxTree);
            }
        }

        private static bool RunFrontEnd(string fileName, StreamReader fileStream,
            out SymbolTable symbolTable, out Program abstractSyntaxTree)
        {
            var frontend = new FrontEnd.FrontEnd(fileStream);
            if (frontend.TryProgramAnalysis(out abstractSyntaxTree, out symbolTable))
            {
                return true;
            }
            else
            {
                Console.WriteLine("Compilation failed.");
                var sourceCode = File.ReadAllLines(fileName);
                var sourceLines = sourceCode.Count();
                string errorCodeDecl = "Code near error source: ";
                var errors = frontend.GetErrors();
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ToString());
                    if (error.Row > 0 && error.Row <= sourceLines)
                    {
                        var sourceLine = sourceCode[error.Row - 1];
                        var trimmedCode = sourceLine.TrimStart().Replace('\t', ' ');
                        var whitespace = sourceLine.Length - trimmedCode.Length;
                        Console.WriteLine(errorCodeDecl + trimmedCode);
                        Console.WriteLine(new String(' ', errorCodeDecl.Length + error.Col - whitespace - 1) + '^');
                    }
                }
                return false;
            }
        }

        private static void RunBackEnd(SymbolTable symbolTable, Program abstractSyntaxTree)
        {
            var backEnd = new CodeGenerator(symbolTable, abstractSyntaxTree, "MainModule");
            backEnd.GenerateCode();
        }
    }
}
