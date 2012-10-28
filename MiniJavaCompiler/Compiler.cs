using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MiniJavaCompiler.Frontend;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.SymbolTable;
using MiniJavaCompiler.Support.AbstractSyntaxTree;

namespace MiniJavaCompiler
{
    class Compiler
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

            var frontend = new FrontEnd(fileStream);
            SymbolTable symbolTable;
            Program abstractSyntaxTree;
            if (frontend.TryProgramAnalysis(out abstractSyntaxTree, out symbolTable))
            {
                Console.WriteLine("Program OK. ");
            }
            else
            {
                var sourceCode = File.ReadAllLines(args[0]);
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
            }
            Console.WriteLine("No back end present, exiting...");
        }
    }
}
