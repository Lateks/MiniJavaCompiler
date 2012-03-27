using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MiniJavaCompiler.SyntaxAnalysis;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.Support.TokenTypes;
using MiniJavaCompiler.LexicalAnalysis;

namespace MiniJavaCompilerTest
{
    public class StubScanner : Scanner
    {
        Queue<Token> tokens;

        public StubScanner(Queue<Token> tokens)
        {
            this.tokens = tokens;
        }

        public Token NextToken()
        {
            if (tokens.Count > 0)
                return tokens.Dequeue();
            else
                return null;
        }
    }

    [TestFixture]
    public class ParserTest
    {
        Queue<Token> programTokens;

        [SetUp]
        public void SetUp()
        {
            programTokens = new Queue<Token>();
        }
    }
}
