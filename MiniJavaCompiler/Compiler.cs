﻿using System;
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
        private const string usage = "Usage: ./MiniJavaCompiler.exe [-no-opt] path/to/source/file [output/file/path]";

        static void Main(string[] args)
        {
            int argn = args.Count();
            if (argn == 0)
            {
                Console.WriteLine(usage);
                return;
            }
            bool disableOptimizations = args[0] == "-no-opt";
            if (disableOptimizations && argn == 1)
            {
                Console.WriteLine(usage);
                return;
            }
            string inputPath = disableOptimizations ? args[1] : args[0];
            
            StreamReader fileStream;
            try
            {
                fileStream = new StreamReader(inputPath);
            }
            catch (ArgumentException)
            {
                Console.WriteLine(usage);
                return;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(String.Format("Could not open file {0}: {1}", inputPath, e.Message));
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

            Program abstractSyntaxTree;
            if (RunFrontEnd(inputPath, fileStream, out abstractSyntaxTree))
            {
                string outputPath = null;
                if (disableOptimizations && argn > 2)
                {
                    outputPath = args[2];
                }
                else if (!disableOptimizations && argn > 1)
                {
                    outputPath = args[1];
                }
                RunBackEnd(abstractSyntaxTree, outputPath, !disableOptimizations);
            }
            else
            {
                Environment.Exit(1);
            }
        }

        private static bool RunFrontEnd(string fileName, StreamReader fileStream, out Program abstractSyntaxTree)
        {
            var frontend = new FrontEnd.FrontEnd(fileStream);
            if (frontend.TryProgramAnalysis(out abstractSyntaxTree))
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
                        PrintError(errorCodeDecl, sourceLine, error.Col);
                    }
                }
                return false;
            }
        }

        private static void PrintError(string errorCodeDecl, string sourceLine, int errorCol)
        {
            string errorLine = sourceLine;
            if (errorLine.Length > 120)
            {
                var splitCodeLines = sourceLine.Split(';');
                if (splitCodeLines.Length > 0)
                {
                    int index = GetIndex(splitCodeLines, errorCol);
                    errorLine = splitCodeLines[index];
                    if (index != splitCodeLines.Length - 1)
                    {
                        errorLine += ";"; // restore the semicolon for printing
                    }
                    errorCol -= splitCodeLines.Take(index).Select<string, int>((s) => s.Length + 1).Sum(); // + 1 for removed semicolons
                }
            }
            var trimmedLine = errorLine.TrimStart().Replace('\t', ' ').Replace('\v', ' ');
            errorCol -= errorLine.Length - trimmedLine.Length;
            Console.WriteLine(errorCodeDecl + trimmedLine);
            Console.WriteLine(new String(' ', errorCodeDecl.Length + errorCol - 1) + '^');
        }

        private static int GetIndex(string[] splitCodeLines, int col)
        {
            int lengthSoFar = 0;
            int index = -1;
            while (lengthSoFar < col && index < splitCodeLines.Length)
            {
                index++;
                lengthSoFar += splitCodeLines[index].Length + 1; // + 1 for removed semicolons
            }
            return index;
        }

        private static void RunBackEnd(Program abstractSyntaxTree, string fileName, bool optimize)
        {
            var backEnd = new CodeGenerator(abstractSyntaxTree, "MainModule$0", optimize);
            if (fileName != null)
            {
                if (fileName.Contains('\\') || fileName.Contains('/'))
                {
                    string[] splitPath = fileName.Split(new char[] { '\\', '/' },
                        StringSplitOptions.RemoveEmptyEntries);
                    var pathName = splitPath.Take<string>(splitPath.Length - 1)
                        .Aggregate("", (acc, pathElem) => acc += pathElem + "\\");
                    try
                    {
                        Directory.SetCurrentDirectory(pathName);
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        Console.WriteLine(String.Format("Could not open directory: {0}", e.Message));
                        return;
                    }
                    fileName = splitPath[splitPath.Length - 1];
                }

                if (fileName.Length <= 4 || fileName.Substring(fileName.Length - 4, 4) != ".exe")
                {
                    fileName += ".exe";
                }
                backEnd.GenerateCode(fileName);
            }
            else
            {
                backEnd.GenerateCode();
            }
        }
    }
}
