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

        [Test]
        public void SimpleMainClassWithEmptyMainMethod()
        {
            DeclareMainClassUntilMainMethod("ThisIsTheMainClass");
            CloseMainMethodAndClass();

            var scanner = new StubScanner(programTokens);
            var parser = new Parser(scanner);
            Program programTree = parser.Parse();
            Assert.That(programTree.Classes.Count, Is.EqualTo(0));
            Assert.That(programTree.MainClass.Name, Is.EqualTo("ThisIsTheMainClass"));
            Assert.That(programTree.MainClass.MainMethod.Count, Is.EqualTo(0));
        }

        [Test]
        public void SimpleMainClassWithArithmeticAndPrinting()
        {
            DeclareMainClassUntilMainMethod("ThisIsTheMainClass");
            DeclareBasicVariable("foo", "int");
            AssignIntegerToVariable("foo", "42");
            PrintVariableValue("foo");
            CloseMainMethodAndClass();

            var scanner = new StubScanner(programTokens);
            var programTree = new Parser(scanner).Parse();
            Assert.That(programTree.Classes.Count, Is.EqualTo(0));
            Assert.That(programTree.MainClass.Name, Is.EqualTo("ThisIsTheMainClass"));
            Assert.That(programTree.MainClass.MainMethod.Count, Is.EqualTo(3));
            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod[0], Is.InstanceOf<VariableDeclaration>());
            var fooDecl = (VariableDeclaration)mainMethod[0];
            Assert.That(fooDecl.Name, Is.EqualTo("foo"));
            Assert.That(fooDecl.IsArray, Is.EqualTo(false));
            Assert.That(mainMethod[1], Is.InstanceOf<AssignmentStatement>());
            var assignment = (AssignmentStatement)mainMethod[1];
            Assert.That(assignment.LHS, Is.InstanceOf<VariableReference>());
            Assert.That(((VariableReference)assignment.LHS).Name, Is.EqualTo("foo"));
            Assert.That(assignment.RHS, Is.InstanceOf<IntegerLiteral>());
            Assert.That(((IntegerLiteral)assignment.RHS).Value, Is.EqualTo("42"));
            Assert.That(mainMethod[2], Is.InstanceOf<PrintStatement>());
            Assert.That(((PrintStatement)mainMethod[2]).Expression, Is.InstanceOf<VariableReference>());
        }

        [Test]
        public void CreatingABaseTypeArray()
        {
            DeclareMainClassUntilMainMethod("ThisIsTheMainClass");
            DeclareBasicArrayVariable("foo", "int");
            AssignNewArrayToVariable("foo", "int", "10");
            CloseMainMethodAndClass();

            var scanner = new StubScanner(programTokens);
            var programTree = new Parser(scanner).Parse();

            Assert.That(programTree.Classes.Count, Is.EqualTo(0));
            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.Count, Is.EqualTo(2));
            Assert.That(mainMethod[0], Is.InstanceOf<VariableDeclaration>());
            var decl = (VariableDeclaration)mainMethod[0];
            Assert.That(decl.Name, Is.EqualTo("foo"));
            Assert.That(decl.Type, Is.EqualTo("int"));
            Assert.That(decl.IsArray, Is.EqualTo(true));
            Assert.That(mainMethod[1], Is.InstanceOf<AssignmentStatement>());
            var assignment = (AssignmentStatement)mainMethod[1];
            Assert.That(assignment.LHS, Is.InstanceOf<VariableReference>());
            Assert.That(assignment.RHS, Is.InstanceOf<InstanceCreation>());
            var newinstance = (InstanceCreation)assignment.RHS;
            Assert.That(newinstance.Type, Is.EqualTo("int"));
            Assert.That(newinstance.ArraySize, Is.InstanceOf<IntegerLiteral>());
            Assert.That(((IntegerLiteral)newinstance.ArraySize).Value, Is.EqualTo("10"));
        }

        private void CloseMainMethodAndClass()
        {
            programTokens.Enqueue(new RightCurlyBrace(0, 0));
            programTokens.Enqueue(new RightCurlyBrace(0, 0));
            programTokens.Enqueue(new EOF(0, 0));
        }

        private void PrintVariableValue(string variableName)
        {
            programTokens.Enqueue(new KeywordToken("System", 0, 0));
            programTokens.Enqueue(new MethodInvocationToken(0, 0));
            programTokens.Enqueue(new KeywordToken("out", 0, 0));
            programTokens.Enqueue(new MethodInvocationToken(0, 0));
            programTokens.Enqueue(new KeywordToken("println", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new Identifier(variableName, 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new EndLine(0, 0));
        }

        private void AssignIntegerToVariable(string variableName, string integerValue)
        {
            programTokens.Enqueue(new Identifier(variableName, 0, 0));
            programTokens.Enqueue(new AssignmentToken(0, 0));
            programTokens.Enqueue(new IntegerLiteralToken(integerValue, 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));
        }

        private void DeclareMainClassUntilMainMethod(string className)
        {
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new Identifier(className, 0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
            programTokens.Enqueue(new KeywordToken("public", 0, 0));
            programTokens.Enqueue(new KeywordToken("static", 0, 0));
            programTokens.Enqueue(new MiniJavaType("void", 0, 0));
            programTokens.Enqueue(new KeywordToken("main", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
        }

        private void DeclareBasicVariable(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaType(type, 0, 0));
            programTokens.Enqueue(new Identifier(name, 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));
        }

        private void AssignNewArrayToVariable(string variableName, string arrayType, string arraySize)
        {
            programTokens.Enqueue(new Identifier(variableName, 0, 0));
            programTokens.Enqueue(new AssignmentToken(0, 0));
            programTokens.Enqueue(new KeywordToken("new", 0, 0));
            programTokens.Enqueue(new MiniJavaType(arrayType, 0, 0));
            programTokens.Enqueue(new LeftBracket(0, 0));
            programTokens.Enqueue(new IntegerLiteralToken(arraySize, 0, 0));
            programTokens.Enqueue(new RightBracket(0, 0));
            programTokens.Enqueue(new EndLine(0, 0));
        }

        private void DeclareBasicArrayVariable(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaType(type, 0, 0));
            programTokens.Enqueue(new LeftBracket(0, 0));
            programTokens.Enqueue(new RightBracket(0, 0));
            programTokens.Enqueue(new Identifier(name, 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));
        }
    }
}
