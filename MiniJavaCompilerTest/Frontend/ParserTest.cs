﻿using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MiniJavaCompiler.SyntaxAnalysis;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support;

namespace MiniJavaCompilerTest.Frontend
{
    public class StubScanner : ITokenizer
    {
        Queue<IToken> tokens;

        public StubScanner(Queue<IToken> tokens)
        {
            this.tokens = tokens;
        }

        public bool InputLeft()
        {
            return tokens.Count > 0;
        }

        public IToken NextToken()
        {
            if (tokens.Count > 0)
                return tokens.Dequeue();
            else
                return new EndOfFile(0, 0);
        }
    }

    [TestFixture]
    public class ParserUnitTest
    {
      // TODO: Test operator precedences and parenthesised expressions
        Queue<IToken> programTokens;

        [SetUp]
        public void SetUp()
        {
            programTokens = new Queue<IToken>();
        }

        private Program GetProgramTree()
        {
            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            return parser.Parse();
        }

        [Test]
        public void ValidClassDeclarationWithExtension()
        { // class ClassName extends OtherClass { }
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new IdentifierToken("ClassName", 0, 0));
            programTokens.Enqueue(new KeywordToken("extends", 0, 0));
            programTokens.Enqueue(new IdentifierToken("OtherClass", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            programTokens.Enqueue(new PunctuationToken("}", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var classDecl = parser.ClassDeclaration();
            Assert.That(classDecl.InheritedClass, Is.EqualTo("OtherClass"));
            Assert.That(classDecl.Name, Is.EqualTo("ClassName"));
            Assert.NotNull(classDecl.Declarations);
            Assert.That(classDecl.Declarations.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidClassDeclarationWithInternalDeclarations()
        { // class ClassName { int foo; public void bar() { } }
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new IdentifierToken("ClassName", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            programTokens.Enqueue(new MiniJavaTypeToken("int", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            programTokens.Enqueue(new KeywordToken("public", 0, 0));
            programTokens.Enqueue(new MiniJavaTypeToken("void", 0, 0));
            programTokens.Enqueue(new IdentifierToken("bar", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            programTokens.Enqueue(new PunctuationToken("}", 0, 0));
            programTokens.Enqueue(new PunctuationToken("}", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var classDecl = parser.ClassDeclaration();
            Assert.IsNull(classDecl.InheritedClass);
            Assert.NotNull(classDecl.Declarations);
            Assert.That(classDecl.Declarations.Count, Is.EqualTo(2));
        }

        [Test]
        public void BasicTypeVariableDeclaration()
        { // int foo;
            programTokens.Enqueue(new MiniJavaTypeToken("int", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            EndLine();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var variableDecl = parser.VariableDeclaration();
            Assert.False(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("int"));
        }

        [Test]
        public void UserDefinedTypeVariableDeclaration()
        { // someType foo;
            programTokens.Enqueue(new IdentifierToken("SomeType", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            EndLine();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var variableDecl = parser.VariableDeclaration();
            Assert.False(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("SomeType"));
        }

        [Test]
        public void ArrayVariableDeclaration()
        { // int[] foo;
            programTokens.Enqueue(new MiniJavaTypeToken("int", 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            EndLine();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var variableDecl = parser.VariableDeclaration();
            Assert.True(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("int"));
        }

        [Test]
        public void AssertStatement()
        { // assert(true);
            programTokens.Enqueue(new KeywordToken("assert", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            EndLine();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<AssertStatement>());
            Assert.That(((AssertStatement)statement).Expression, Is.InstanceOf<BooleanLiteralExpression>());
        }

        [Test]
        public void PrintStatement()
        { // System.out.println(5);
            programTokens.Enqueue(new KeywordToken("System", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new KeywordToken("out", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new KeywordToken("println", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("5", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<PrintStatement>());
            Assert.That(((PrintStatement)statement).Expression, Is.InstanceOf<IntegerLiteralExpression>());
        }

        [Test]
        public void WhileStatement()
        { // while (true) assert(false);
            programTokens.Enqueue(new KeywordToken("while", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            programTokens.Enqueue(new KeywordToken("assert", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new KeywordToken("false", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            programTokens.Enqueue(new PunctuationToken("}",0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<WhileStatement>());
            var whileStatement = (WhileStatement)statement;
            Assert.That(whileStatement.BooleanExpression, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.That(whileStatement.LoopBody, Is.InstanceOf<BlockStatement>());
            Assert.That(whileStatement.LoopBody.Statements[0], Is.InstanceOf<AssertStatement>());
        }

        [Test]
        public void ReturnStatement()
        { // return foo;
            programTokens.Enqueue(new KeywordToken("return", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<ReturnStatement>());
            Assert.That(((ReturnStatement)statement).Expression, Is.InstanceOf<VariableReferenceExpression>());
        }

        [Test]
        public void MethodInvocationStatement()
        { // foo.bar();
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new IdentifierToken("bar", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<MethodInvocation>());
            var invocation = (MethodInvocation)statement;
            Assert.That(invocation.MethodName, Is.EqualTo("bar"));
            Assert.That(invocation.MethodOwner, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(((VariableReferenceExpression)invocation.MethodOwner).Name, Is.EqualTo("foo"));
            Assert.NotNull(invocation.CallParameters);
            Assert.That(invocation.CallParameters.Count, Is.EqualTo(0));
        }

        [Test]
        public void VariableDeclarationStatement()
        { // foo[] bar;
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new IdentifierToken("bar", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<VariableDeclaration>());
            Assert.True(((VariableDeclaration)statement).IsArray);
        }

        [Test]
        public void AssignmentToArrayStatement()
        { // foo[5] = true;
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("5", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<AssignmentStatement>());
            var assignment = (AssignmentStatement)statement;
            Assert.That(assignment.RightHandSide, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.That(assignment.LeftHandSide, Is.InstanceOf<ArrayIndexingExpression>());
        }

        [Test]
        public void TryingToAssignToArrayWithoutAnIndexExpression()
        { // foo[] = 42;
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("42", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            Assert.Null(parser.Statement());
            Assert.That(errorReporter.Errors(), Is.Not.Empty);
        }

        [Test]
        public void AllExpressionsDoNotQualifyAsAStatements()
        { // 42;
            programTokens.Enqueue(new IntegerLiteralToken("42", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            Assert.Null(parser.Statement());
            Assert.That(errorReporter.Errors(), Is.Not.Empty);
        }

        [Test]
        public void BinaryOperatorExpression()
        { // 7 % foo == 0
            programTokens.Enqueue(new IntegerLiteralToken("7", 0, 0));
            programTokens.Enqueue(new OperatorToken("%", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new OperatorToken("==", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("0", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var expression = parser.Expression();
            Assert.That(expression, Is.InstanceOf<BinaryOpExpression>());
            var logicalOp = (BinaryOpExpression)expression;
            Assert.That(logicalOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)logicalOp.RightOperand).Value, Is.EqualTo("0"));
            Assert.That(logicalOp.LeftOperand, Is.InstanceOf<BinaryOpExpression>());
            var arithmetic = (BinaryOpExpression)logicalOp.LeftOperand;
            Assert.That(arithmetic.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)arithmetic.LeftOperand).Value, Is.EqualTo("7"));
            Assert.That(arithmetic.RightOperand, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(((VariableReferenceExpression)arithmetic.RightOperand).Name, Is.EqualTo("foo"));
        }

        [Test]
        public void OperatorPrecedences()
        { // 4 + 9 * (7 - 2 % 3) - 2
            programTokens.Enqueue(new IntegerLiteralToken("4", 0, 0));
            programTokens.Enqueue(new OperatorToken("+", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("9", 0, 0));
            programTokens.Enqueue(new OperatorToken("*", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("7", 0, 0));
            programTokens.Enqueue(new OperatorToken("-", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("2", 0, 0));
            programTokens.Enqueue(new OperatorToken("%", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("3", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new OperatorToken("-", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("2", 0, 0));

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(new StubScanner(programTokens), errorReporter), errorReporter);
            var expression = parser.Expression();
            Assert.That(expression, Is.InstanceOf<BinaryOpExpression>());
            var minusOp = (BinaryOpExpression)expression;
            Assert.That(minusOp.Operator, Is.EqualTo("-"));
            Assert.That(minusOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(minusOp.LeftOperand, Is.InstanceOf<BinaryOpExpression>());
            var plusOp = (BinaryOpExpression)minusOp.LeftOperand;
            Assert.That(plusOp.Operator, Is.EqualTo("+"));
            Assert.That(plusOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(plusOp.RightOperand, Is.InstanceOf<BinaryOpExpression>());
            var timesOp = (BinaryOpExpression)plusOp.RightOperand;
            Assert.That(timesOp.Operator, Is.EqualTo("*"));
            Assert.That(timesOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(timesOp.RightOperand, Is.InstanceOf<BinaryOpExpression>());
            var parenthesisedMinusOp = (BinaryOpExpression)timesOp.RightOperand;
            Assert.That(parenthesisedMinusOp.Operator, Is.EqualTo("-"));
            Assert.That(parenthesisedMinusOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(parenthesisedMinusOp.RightOperand, Is.InstanceOf<BinaryOpExpression>());
            var moduloOp = (BinaryOpExpression)parenthesisedMinusOp.RightOperand;
            Assert.That(moduloOp.Operator, Is.EqualTo("%"));
            Assert.That(moduloOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(moduloOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());
        }

        [Test]
        public void SimpleMainClassWithEmptyMainMethod()
        {   /* class ThisIsTheMainClass {
             *     public static void main() { }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("ThisIsTheMainClass");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            Program programTree = GetProgramTree();

            Assert.That(programTree.Classes.Count, Is.EqualTo(0));
            Assert.That(programTree.MainClass.Name, Is.EqualTo("ThisIsTheMainClass"));
            Assert.That(programTree.MainClass.MainMethod.MethodBody.Count, Is.EqualTo(0));
        }

        [Test]
        public void SimpleMainClassWithArithmeticAndPrinting()
        {   /* class ThisIsTheMainClass {
             *     public static void main() {
             *         int foo;
             *         foo = 42;
             *         System.out.println(foo);
             *     }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("ThisIsTheMainClass");
            DeclareBasicVariable("foo", "int");
            AssignIntegerToVariable("foo", "42");
            PrintVariableValue("foo");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            Assert.That(programTree.Classes.Count, Is.EqualTo(0));
            Assert.That(programTree.MainClass.Name, Is.EqualTo("ThisIsTheMainClass"));
            Assert.That(programTree.MainClass.MainMethod.MethodBody.Count, Is.EqualTo(3));

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.MethodBody[0], Is.InstanceOf<VariableDeclaration>());

            var fooDecl = (VariableDeclaration)mainMethod.MethodBody[0];
            Assert.That(fooDecl.Name, Is.EqualTo("foo"));
            Assert.That(fooDecl.IsArray, Is.EqualTo(false));

            Assert.That(mainMethod.MethodBody[1], Is.InstanceOf<AssignmentStatement>());

            var assignment = (AssignmentStatement)mainMethod.MethodBody[1];
            Assert.That(assignment.LeftHandSide, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(((VariableReferenceExpression)assignment.LeftHandSide).Name, Is.EqualTo("foo"));
            Assert.That(assignment.RightHandSide, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)assignment.RightHandSide).Value, Is.EqualTo("42"));
            Assert.That(mainMethod.MethodBody[2], Is.InstanceOf<PrintStatement>());
            Assert.That(((PrintStatement)mainMethod.MethodBody[2]).Expression, Is.InstanceOf<VariableReferenceExpression>());
        }

        [Test]
        public void CreatingABaseTypeArray()
        {   /* class ThisIsTheMainClass {
             *     public static void main() {
             *         int[] foo;
             *         foo = new int[10];
             *     }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("ThisIsTheMainClass");
            DeclareBasicArrayVariable("foo", "int");
            AssignNewArrayToVariable("foo", "int", "10");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            Assert.That(programTree.Classes.Count, Is.EqualTo(0));

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.MethodBody.Count, Is.EqualTo(2));
            Assert.That(mainMethod.MethodBody[0], Is.InstanceOf<VariableDeclaration>());

            var decl = (VariableDeclaration)mainMethod.MethodBody[0];
            Assert.That(decl.Name, Is.EqualTo("foo"));
            Assert.That(decl.Type, Is.EqualTo("int"));
            Assert.True(decl.IsArray);
            Assert.That(mainMethod.MethodBody[1], Is.InstanceOf<AssignmentStatement>());

            var assignment = (AssignmentStatement)mainMethod.MethodBody[1];
            Assert.That(assignment.LeftHandSide, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(assignment.RightHandSide, Is.InstanceOf<InstanceCreationExpression>());

            var newinstance = (InstanceCreationExpression)assignment.RightHandSide;
            Assert.That(newinstance.Type, Is.EqualTo("int"));
            Assert.That(newinstance.ArraySize, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)newinstance.ArraySize).Value, Is.EqualTo("10"));
        }

        [Test]
        public void MainMethodWithFormalParametersCausesSyntaxError()
        {   /* class MainClass {
             *     public static void main(int foo) { }
             * }
             * <EndOfFile>
             */
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new IdentifierToken("MainClass", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            programTokens.Enqueue(new KeywordToken("public", 0, 0));
            programTokens.Enqueue(new KeywordToken("static", 0, 0));
            programTokens.Enqueue(new MiniJavaTypeToken("void", 0, 0));
            programTokens.Enqueue(new KeywordToken("main", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new MiniJavaTypeToken("int", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            EndFile();

            Assert.Throws<SyntaxAnalysisFailed>(() => GetProgramTree());
        }

        [Test]
        public void MethodWithoutParameters()
        {   /* class MainClass {
             *     public static void main() { }
             * }
             * class someClass {
             *     public int someMethod() { }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            ClosingCurlyBrace(); ClosingCurlyBrace();

            BeginClassDeclaration("someClass");
            BeginMethodDeclaration("someMethod", "int");
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            Assert.That(programTree.Classes.Count, Is.EqualTo(1));
            var testClass = (ClassDeclaration)programTree.Classes[0];
            Assert.That(testClass.Declarations.Count, Is.EqualTo(1));
            var declaration = (Declaration)testClass.Declarations[0];
            Assert.That(declaration, Is.InstanceOf<MethodDeclaration>());
            Assert.That(((MethodDeclaration)declaration).Formals.Count, Is.EqualTo(0));
        }

        [Test]
        public void MethodInvocationWithParameters()
        {   /* class MainClass {
             *     public static void main() {
             *         someClass.someMethod(42, parameterVariable);
             *     }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            MakeMethodInvocationWithoutParentheses("someClass", "someMethod");
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("42", 0, 0));
            programTokens.Enqueue(new PunctuationToken(",", 0, 0));
            programTokens.Enqueue(new IdentifierToken("parameterVariable", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            EndLine();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.MethodBody.Count, Is.EqualTo(1));
            Assert.That(mainMethod.MethodBody[0], Is.InstanceOf<MethodInvocation>());
            var methodInvocation = (MethodInvocation)mainMethod.MethodBody[0];
            Assert.That(methodInvocation.MethodOwner, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(((VariableReferenceExpression)methodInvocation.MethodOwner).Name, Is.EqualTo("someClass"));
            Assert.That(methodInvocation.MethodName, Is.EqualTo("someMethod"));
            Assert.That(methodInvocation.CallParameters.Count, Is.EqualTo(2));
        }

        [Test]
        public void MethodInvocationWithoutParameters()
        {   /* class MainClass {
             *     public static void main() {
             *         someClass.someMethod();
             *     }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            MakeMethodInvocationWithoutParentheses("someClass", "someMethod");
            EmptyMethodInvocationParentheses();
            EndLine();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.MethodBody.Count, Is.EqualTo(1));
            Assert.That(mainMethod.MethodBody[0], Is.InstanceOf<MethodInvocation>());
            var methodInvocation = (MethodInvocation)mainMethod.MethodBody[0];
            Assert.That(methodInvocation.MethodOwner, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(((VariableReferenceExpression)methodInvocation.MethodOwner).Name, Is.EqualTo("someClass"));
            Assert.That(methodInvocation.MethodName, Is.EqualTo("someMethod"));
            Assert.That(methodInvocation.CallParameters.Count, Is.EqualTo(0));
        }

        [Test]
        public void MethodWithFormalParameters()
        {   /* class MainClass {
             *     public static void main() { }
             * }
             * class anotherClass {
             *     public int someMethod(int foo, myOwnType bar) { }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            ClosingCurlyBrace(); ClosingCurlyBrace();

            BeginClassDeclaration("anotherClass");
            BeginMethodDeclaration("someMethod", "int");
            DefineBasicParameter("foo", "int");
            programTokens.Enqueue(new PunctuationToken(",", 0, 0));
            DefineOwnTypeParameter("bar", "myOwnType");
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            Assert.That(programTree.Classes.Count, Is.EqualTo(1));
            var testClass = (ClassDeclaration)programTree.Classes[0];
            Assert.IsNull(testClass.InheritedClass);
            Assert.NotNull(testClass.Declarations);
            Assert.That(testClass.Declarations.Count, Is.EqualTo(1));
            var methodDeclaration = (MethodDeclaration)testClass.Declarations[0];
            Assert.That(methodDeclaration.Formals.Count, Is.EqualTo(2));
            
            var formal1 = methodDeclaration.Formals[0];
            Assert.That(formal1.Name, Is.EqualTo("foo"));
            Assert.That(formal1.Type, Is.EqualTo("int"));
            Assert.False(formal1.IsArray);

            var formal2 = methodDeclaration.Formals[1];
            Assert.That(formal2.Name, Is.EqualTo("bar"));
            Assert.That(formal2.Type, Is.EqualTo("myOwnType"));
            Assert.False(formal2.IsArray);
        }

        [Test]
        public void ChainedMethodInvocation()
        {   /* class MainClass {
             *     public static void main() {
             *         someObject.someMethod().anotherMethod().length();
             *     }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            MakeMethodInvocationWithoutParentheses("someObject", "someMethod");
            EmptyMethodInvocationParentheses();
            InvokeMethod("anotherMethod");
            EmptyMethodInvocationParentheses();
            InvokeMethod("length");
            EmptyMethodInvocationParentheses();
            EndLine();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.MethodBody.Count, Is.EqualTo(1));
            Assert.That(mainMethod.MethodBody[0], Is.InstanceOf<MethodInvocation>());
            var lengthMethodInvocation = (MethodInvocation)mainMethod.MethodBody[0];
            Assert.That(lengthMethodInvocation.MethodName, Is.EqualTo("length"));
            Assert.That(lengthMethodInvocation.MethodOwner, Is.InstanceOf<MethodInvocation>());
            var anotherMethodInvocation = (MethodInvocation)lengthMethodInvocation.MethodOwner;
            Assert.That(anotherMethodInvocation.MethodName, Is.EqualTo("anotherMethod"));
            Assert.That(anotherMethodInvocation.MethodOwner, Is.InstanceOf<MethodInvocation>());
            var someMethodInvocation = (MethodInvocation)anotherMethodInvocation.MethodOwner;
            Assert.That(someMethodInvocation.MethodName, Is.EqualTo("someMethod"));
            Assert.That(someMethodInvocation.MethodOwner, Is.InstanceOf<VariableReferenceExpression>());
        }

        [Test]
        public void IfStatementWithoutElseBranch()
        {   /* class MainClass {
             *     public static void main() {
             *         if (true && false)
             *             foo = 42;
             *     }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new KeywordToken("if", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new OperatorToken("&&", 0, 0));
            programTokens.Enqueue(new KeywordToken("false", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            AssignIntegerToVariable("foo", "42");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var mainMethod = GetProgramTree().MainClass.MainMethod.MethodBody;
            Assert.That(mainMethod.Count, Is.EqualTo(1));
            Assert.That(mainMethod[0], Is.InstanceOf<IfStatement>());
            var ifStatement = (IfStatement)mainMethod[0];
            Assert.That(ifStatement.ThenBranch, Is.InstanceOf<BlockStatement>());
            Assert.IsNull(ifStatement.ElseBranch);
            Assert.That(ifStatement.BooleanExpression, Is.InstanceOf<BinaryOpExpression>());
            var boolExpression = (BinaryOpExpression)ifStatement.BooleanExpression;
            Assert.That(boolExpression.LeftOperand, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.IsTrue(((BooleanLiteralExpression)boolExpression.LeftOperand).Value);
            Assert.That(boolExpression.RightOperand, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.IsFalse(((BooleanLiteralExpression)boolExpression.RightOperand).Value);
        }

        [Test]
        public void IfStatementWithElseBranch()
        {   /* class MainClass {
             *     public static void main() {
             *         if (true)
             *             foo.bar();
             *         else
             *             foo = 42;
             *     }
             * }
             * <EndOfFile>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new KeywordToken("if", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new IdentifierToken("bar", 0, 0));
            EmptyMethodInvocationParentheses();
            EndLine();
            programTokens.Enqueue(new KeywordToken("else", 0, 0));
            AssignIntegerToVariable("foo", "42");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var mainMethod = GetProgramTree().MainClass.MainMethod.MethodBody;
            Assert.That(mainMethod.Count, Is.EqualTo(1));
            Assert.That(mainMethod[0], Is.InstanceOf<IfStatement>());
            var ifStatement = (IfStatement)mainMethod[0];
            Assert.That(ifStatement.ThenBranch, Is.InstanceOf<BlockStatement>());
            Assert.That(((BlockStatement) ifStatement.ThenBranch).Statements[0], Is.InstanceOf<MethodInvocation>());
            Assert.That(ifStatement.ElseBranch, Is.InstanceOf<BlockStatement>());
            Assert.That(((BlockStatement)ifStatement.ElseBranch).Statements[0], Is.InstanceOf<AssignmentStatement>());

        }

        private void EndLine()
        {
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
        }

        private void InvokeMethod(string methodName)
        {
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new IdentifierToken(methodName, 0, 0));
        }

        private void EmptyMethodInvocationParentheses()
        {
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
        }

        private void MakeMethodInvocationWithoutParentheses(string className, string methodName)
        {
            programTokens.Enqueue(new IdentifierToken(className, 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new IdentifierToken(methodName, 0, 0));
        }

        private void DefineOwnTypeParameter(string name, string type)
        {
            programTokens.Enqueue(new IdentifierToken(type, 0, 0));
            programTokens.Enqueue(new IdentifierToken(name, 0, 0));
        }

        private void DefineBasicParameter(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaTypeToken(type, 0, 0));
            programTokens.Enqueue(new IdentifierToken(name, 0, 0));
        }

        private void BeginMethodDeclaration(string methodName, string type)
        {
            programTokens.Enqueue(new KeywordToken("public", 0, 0));
            programTokens.Enqueue(new MiniJavaTypeToken(type, 0, 0));
            programTokens.Enqueue(new IdentifierToken(methodName, 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
        }

        private void BeginClassDeclaration(string className)
        {
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new IdentifierToken(className, 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
        }

        private void ClosingCurlyBrace()
        {
            programTokens.Enqueue(new PunctuationToken("}", 0, 0));
        }

        private void EndFile()
        {
            programTokens.Enqueue(new EndOfFile(0, 0));
        }

        private void PrintVariableValue(string variableName)
        {
            programTokens.Enqueue(new KeywordToken("System", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new KeywordToken("out", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new KeywordToken("println", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new IdentifierToken(variableName, 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            EndLine();
        }

        private void AssignIntegerToVariable(string variableName, string integerValue)
        {
            programTokens.Enqueue(new IdentifierToken(variableName, 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken(integerValue, 0, 0));
            EndLine();
        }

        private void DeclareMainClassUntilMainMethod(string className)
        {
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new IdentifierToken(className, 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            programTokens.Enqueue(new KeywordToken("public", 0, 0));
            programTokens.Enqueue(new KeywordToken("static", 0, 0));
            programTokens.Enqueue(new MiniJavaTypeToken("void", 0, 0));
            programTokens.Enqueue(new KeywordToken("main", 0, 0));
            EmptyMethodInvocationParentheses();
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
        }

        private void DeclareBasicVariable(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaTypeToken(type, 0, 0));
            programTokens.Enqueue(new IdentifierToken(name, 0, 0));
            EndLine();
        }

        private void AssignNewArrayToVariable(string variableName, string arrayType, string arraySize)
        {
            programTokens.Enqueue(new IdentifierToken(variableName, 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
            programTokens.Enqueue(new KeywordToken("new", 0, 0));
            programTokens.Enqueue(new MiniJavaTypeToken(arrayType, 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken(arraySize, 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            EndLine();
        }

        private void DeclareBasicArrayVariable(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaTypeToken(type, 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new IdentifierToken(name, 0, 0));
            EndLine();
        }
    }

    [TestFixture]
    public class RecoveryTest
    {
        private ErrorLogger _errorLog;
        private IParser _parser;

        public void SetUpParser(string program)
        {
            var scanner = new MiniJavaScanner(new StringReader(program));
            _errorLog = new ErrorLogger();
            _parser = new Parser(new ParserInputReader(scanner, _errorLog), _errorLog);
        }

        [Test]
        public void RecoveryFromClassMatchingWhenParenthesesNotBalanced()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { }\n" + // Class Foo is not closed: recovery consumes the class keyword (assumed to be }) from
                                                                    // the next class declaration and discards tokens until the next class keyword (on the last row).
                             "class Bar { pblic int foo() { }}\n" + // Typo in keyword: missed due to recovery.
                             "class Baz { public int bar(+ foo) { } }\n"; // There should be an identifier or a type token in place of +. This error is caught.
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Expected '}' but got keyword 'class'"));
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Expected type name but got operator '+'"));
        }

        [Test]
        public void RecoveryFromClassMatchingWhenLexicalErrors()
        {
            string program = "class Foo_Bar$ { }\n" + // there is a lexical error on this row
                             "class Bar { public Foo_Bar bar(, int foo) { } }"; // should detect the error here
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Expected type name but got punctuation token ','"));
        }

        [Test]
        public void ErrorsWhenEndlessCommentEncountered()
        {
            string program = "class Foo { /* public static void main() { } }\n" +
                             "class Bar { public Foo bar(int foo) { return new Foo(); } }";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(1));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Reached end of input while scanning for a comment"));
        }

        [Test]
        public void MissingClosingParenthesisForClassAtTheEndOfProgram()
        {
            string program = "class Foo { public static void main() { System.out.println(42); }";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Reached end of file while parsing for '}'")); // encountered end of file instead of the expected token
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Reached end of file while parsing")); // scanner is out of input
        }

        [Test]
        public void MissingClosingParenthesisForAMethod()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t public void foo() {\n" +
                             "\t\t int foo;\n" +
                             "\t\t\n" + // missing closing parenthesis
                             "\t int bar;\n" +
                             "}" + // closes class A
                             "class B { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(3));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Invalid token 'class' of type keyword starting a declaration"));
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Reached end of file while parsing a declaration"));
            Assert.That(_errorLog.Errors()[2].Message, Is.StringContaining("Reached end of file")); // scanner is out of input
        }

        [Test]
        public void LexicalErrorInADeclaration()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t public void foo() {\n" +
                             "\t\t int foo$;?\n" +
                             "\t}\n" +
                             "\t int bar;\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(6));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Unexpected token '?'"));
            Assert.That(_errorLog.Errors()[2].Message, Is.StringContaining("Encountered a lexical error while parsing an expression")); // recovery ends after the line "int bar;"
            Assert.That(_errorLog.Errors()[3].Message, Is.StringContaining("Invalid token 'class' of type keyword starting a declaration")); // expecting a method declaration but found "class B"
            Assert.That(_errorLog.Errors()[4].Message, Is.StringContaining("Reached end of file while parsing a declaration")); // attempted to recover but recovery ended at end of file
            Assert.That(_errorLog.Errors()[5].Message, Is.StringContaining("Reached end of file")); // scanner is out of input
        }

        [Test]
        public void MissingSemicolonInAVariableDeclaration()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t int bar\n" +
                             "\t int foo;\n" +
                             "\t public foo;\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(5));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Expected ';' but got builtin type 'int'")); // recovers until the end of the statement "int foo;"
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Expected identifier but got punctuation token ';'")); // invalid method declaration caught, recovers until the next }
            Assert.That(_errorLog.Errors()[2].Message, Is.StringContaining("Invalid token 'class' of type keyword starting a declaration"));
            Assert.That(_errorLog.Errors()[3].Message, Is.StringContaining("Reached end of file while parsing a declaration")); // recovery ended by end of file
            Assert.That(_errorLog.Errors()[4].Message, Is.StringContaining("Reached end of file while parsing")); // scanner is out of input
        }

        [Test]
        public void MissingSemicolonInALocalVariableDeclaration()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t public int foo() {\n" +
                             "\t\t int bar\n" +
                             "\t\t bar = @$#;\n" + // this error (invalid assignment) will be missed because of recovery but the lexical errors are still reported
                             "\t\t return bar;\n" +
                             "\t }\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(4));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Expected ';' but got identifier 'bar'")); // recovers until the end of the statement "bar = @;"
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Unexpected token '@'"));
            Assert.That(_errorLog.Errors()[2].Message, Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors()[3].Message, Is.StringContaining("Unexpected token '#'"));
        }

        [Test]
        public void MissingSemicolonsInStatements()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t int max;\n" +
                             "\t public int foo(int bar) {\n" +
                             "\t\t max = 99999999;\n" +
                             "\t\t assert(bar > 0)\n" + // missing semicolon
                             "\t\t assert(bar < max);\n" +
                             "\t\t max = max + 1\n" + // missing semicolon
                             "\t\t return bar;\n" +
                             "\t }\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Expected ';' but got keyword 'assert'")); // recovers until the end of the second assertion
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Expected ';' but got keyword 'return'")); // recovers until the end of the return statement
        }

        [Test]
        public void MultipleLexicalErrorsInAnExpression()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t int max;\n" +
                             "\t public int foo(int bar) {\n" +
                             "\t\t max = 1 + @$ - #0;\n" + // multiple lexical errors
                             "\t\t return max;" +
                             "\t }\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(5));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Unexpected token '@'"));
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Encountered a lexical error while parsing an expression"));
            Assert.That(_errorLog.Errors()[2].Message, Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors()[3].Message, Is.StringContaining("Unexpected token '#'"));
            Assert.That(_errorLog.Errors()[4].Message, Is.StringContaining("Encountered a lexical error while parsing an expression"));
        }

        [Test]
        public void InvalidTokenAsIdentifier()
        { // Just reports the lexical errors.
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t int $;\n" +
                             "\t int foo;\n" +
                             "}" +
                             "class ^ { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Unexpected token '^'"));
        }

        [Test]
        public void ExtraSemicolonAfterMethodDeclaration()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t public void foo() { };\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(4));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Invalid token ';' of type punctuation token starting a declaration"));
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Invalid token 'class' of type keyword starting a declaration")); // recovered until the end of the class declaration
            Assert.That(_errorLog.Errors()[2].Message, Is.StringContaining("Reached end of file while parsing a declaration"));
            Assert.That(_errorLog.Errors()[3].Message, Is.StringContaining("Reached end of file while parsing"));
        }

        [Test]
        public void ExtraSemicolonAfterBlock()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t public void foo() { { int foo; foo = 42; }; }\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Assert.Throws<SyntaxAnalysisFailed>(() => _parser.Parse());
            Assert.That(_errorLog.Errors().Count, Is.EqualTo(3));
            Assert.That(_errorLog.Errors()[0].Message, Is.StringContaining("Invalid start token ';' for a term in an expression")); // Expected an expression because a punctuation token other than {
                                                                                                                              // cannot start another kind of statement.
            Assert.That(_errorLog.Errors()[1].Message, Is.StringContaining("Reached end of file while parsing an expression")); // Trying to parse another statement, beginning with an expression,
                                                                                                                                // but recovery has ended up at the end of file.
            Assert.That(_errorLog.Errors()[2].Message, Is.StringContaining("Reached end of file while parsing"));

        }

        [Test]
        public void SingleLineCommentCanEndInEndOfFileWithoutErrors()
        {
            string program = "class Foo {\n" +
                            "\t public static void main() {\n" +
                            "\t\t System.out.println(42);\n" +
                            "\t }\n" +
                            "} // this is a comment ending in EOF";
            SetUpParser(program);
            Assert.DoesNotThrow(() => _parser.Parse());
        }

        // TODO: test invalid keyword starting an expression
        // TODO: -"- a statement
        // TODO: lexical errors in expression terms
        // TODO: lexical error in place of an operator
        // TODO: valid non-operator token in place of an operator
        // TODO: a unary operator token in place of a binary operator token
        // TODO: syntax error in class declaration
        // TODO: error token starting a declaration
        // TODO: lexical error in a class declaration
        // TODO: syntax or lexical error in a method's parameter list
    }
}
