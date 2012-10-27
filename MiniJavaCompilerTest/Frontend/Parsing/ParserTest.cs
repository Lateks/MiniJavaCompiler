using System.Collections.Generic;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Frontend.SyntaxAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using NUnit.Framework;

namespace MiniJavaCompilerTest.Frontend.Parsing
{
    public class StubScanner : ITokenizer
    {
        readonly Queue<IToken> _tokens;

        public StubScanner(Queue<IToken> tokens)
        {
            this._tokens = tokens;
        }

        public IToken NextToken()
        {
            if (_tokens.Count > 0)
                return _tokens.Dequeue();
            else
                throw new OutOfInput("Ran out of input tokens.");
        }
    }

    [TestFixture]
    public class ParserUnitTest
    {
        Queue<IToken> programTokens;

        [SetUp]
        public void SetUp()
        {
            programTokens = new Queue<IToken>();
        }

        private Program GetProgramTree()
        {
            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            return parser.Parse();
        }

        [Test]
        public void TestPeekForward()
        {
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new IdentifierToken("ClassName", 0, 0));
            programTokens.Enqueue(new KeywordToken("extends", 0, 0));
            programTokens.Enqueue(new IdentifierToken("OtherClass", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            programTokens.Enqueue(new PunctuationToken("}", 0, 0));
            EndFile();
            var parserInputReader = new ParserInputReader(new StubScanner(programTokens), new ErrorLogger());
            Assert.That(parserInputReader.PeekForward(1).Lexeme, Is.EqualTo("ClassName"));
            Assert.That(parserInputReader.PeekForward(2).Lexeme, Is.EqualTo("extends"));
            Assert.That(parserInputReader.Consume<IToken>().Lexeme, Is.EqualTo("class"));
            Assert.That(parserInputReader.Consume<IToken>().Lexeme, Is.EqualTo("ClassName"));
            Assert.That(parserInputReader.Consume<IToken>().Lexeme, Is.EqualTo("extends"));
            Assert.That(parserInputReader.Consume<IToken>().Lexeme, Is.EqualTo("OtherClass"));
            Assert.Throws<OutOfInput>(() => parserInputReader.PeekForward(3));
        }

        [Test]
        public void ValidClassDeclarationWithExtension()
        { // class ClassName extends OtherClass { }
            DeclareMainClass("MainClass");
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new IdentifierToken("ClassName", 0, 0));
            programTokens.Enqueue(new KeywordToken("extends", 0, 0));
            programTokens.Enqueue(new IdentifierToken("OtherClass", 0, 0));
            programTokens.Enqueue(new PunctuationToken("{", 0, 0));
            programTokens.Enqueue(new PunctuationToken("}", 0, 0));
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var classDecl = parser.Parse().Classes[0];
            Assert.That(classDecl.InheritedClass, Is.EqualTo("OtherClass"));
            Assert.That(classDecl.Name, Is.EqualTo("ClassName"));
            Assert.NotNull(classDecl.Declarations);
            Assert.That(classDecl.Declarations.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidClassDeclarationWithInternalDeclarations()
        { // class ClassName { int foo; public void bar() { } }
            DeclareMainClass("MainClass");
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
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var classDecl = parser.Parse().Classes[0];
            Assert.IsNull(classDecl.InheritedClass);
            Assert.NotNull(classDecl.Declarations);
            Assert.That(classDecl.Declarations.Count, Is.EqualTo(2));
        }

        [Test]
        public void BasicTypeVariableDeclaration()
        { // int foo;
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new MiniJavaTypeToken("int", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            EndStatement();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var variableDecl = (VariableDeclaration) parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.False(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("int"));
        }

        [Test]
        public void UserDefinedTypeVariableDeclaration()
        { // SomeType foo;
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IdentifierToken("SomeType", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            EndStatement();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var variableDecl = (VariableDeclaration) parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.False(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("SomeType"));
        }

        [Test]
        public void ArrayVariableDeclaration()
        { // int[] foo;
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new MiniJavaTypeToken("int", 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            EndStatement();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var variableDecl = (VariableDeclaration)parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.True(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("int"));
        }

        [Test]
        public void AssertStatement()
        { // assert(true);
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new KeywordToken("assert", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            EndStatement();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var statement = parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.That(statement, Is.InstanceOf<AssertStatement>());
            Assert.That(((AssertStatement)statement).Condition, Is.InstanceOf<BooleanLiteralExpression>());
        }

        [Test]
        public void PrintStatement()
        { // System.out.println(5);
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new KeywordToken("System", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new KeywordToken("out", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new KeywordToken("println", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("5", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var statement = parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.That(statement, Is.InstanceOf<PrintStatement>());
            Assert.That(((PrintStatement)statement).Argument, Is.InstanceOf<IntegerLiteralExpression>());
        }

        [Test]
        public void WhileStatement()
        { // while (true) assert(false);
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new KeywordToken("while", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new KeywordToken("assert", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new KeywordToken("false", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var statement = parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.That(statement, Is.InstanceOf<WhileStatement>());
            var whileStatement = (WhileStatement)statement;
            Assert.That(whileStatement.LoopCondition, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.That(whileStatement.LoopBody, Is.InstanceOf<BlockStatement>());
            Assert.That(whileStatement.LoopBody.Statements[0], Is.InstanceOf<AssertStatement>());
        }

        [Test]
        public void ReturnStatement()
        { // return foo;
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new KeywordToken("return", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var statement = parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.That(statement, Is.InstanceOf<ReturnStatement>());
            Assert.That(((ReturnStatement)statement).ReturnValue, Is.InstanceOf<VariableReferenceExpression>());
        }

        [Test]
        public void MethodInvocationStatement()
        { // foo.bar();
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken(".", 0, 0));
            programTokens.Enqueue(new IdentifierToken("bar", 0, 0));
            programTokens.Enqueue(new PunctuationToken("(", 0, 0));
            programTokens.Enqueue(new PunctuationToken(")", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var statement = parser.Parse().MainClass.MainMethod.MethodBody[0];
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
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new IdentifierToken("bar", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var statement = parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.That(statement, Is.InstanceOf<VariableDeclaration>());
            Assert.True(((VariableDeclaration)statement).IsArray);
        }

        [Test]
        public void AssignmentToArrayStatement()
        { // foo[5] = true;
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("5", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var statement = parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.That(statement, Is.InstanceOf<AssignmentStatement>());
            var assignment = (AssignmentStatement)statement;
            Assert.That(assignment.RightHandSide, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.That(assignment.LeftHandSide, Is.InstanceOf<ArrayIndexingExpression>());
        }

        [Test]
        public void TryingToAssignToArrayWithoutAnIndexExpression()
        { // foo[] = 42;
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("42", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            Assert.Throws<CompilationFailed>(() => parser.Parse());
            Assert.That(errorReporter.Errors, Is.Not.Empty);
        }

        [Test]
        public void AllExpressionsDoNotQualifyAsAStatements()
        { // 42;
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IntegerLiteralToken("42", 0, 0));
            programTokens.Enqueue(new PunctuationToken(";", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            Assert.Throws<CompilationFailed>(() => parser.Parse());
            Assert.That(errorReporter.Errors, Is.Not.Empty);
        }

        [Test]
        public void BinaryOperatorExpression()
        { // 7 % foo == 0
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("7", 0, 0));
            programTokens.Enqueue(new OperatorToken("%", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new OperatorToken("==", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("0", 0, 0));
            EndStatement();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var expression = parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.That(expression, Is.InstanceOf<AssignmentStatement>());
            var assignment = (AssignmentStatement)expression;
            Assert.That(assignment.RightHandSide, Is.InstanceOf<BinaryOperatorExpression>());
            var logicalOp = (BinaryOperatorExpression)assignment.RightHandSide;
            Assert.That(logicalOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)logicalOp.RightOperand).Value, Is.EqualTo("0"));
            Assert.That(logicalOp.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());
            var arithmetic = (BinaryOperatorExpression)logicalOp.LeftOperand;
            Assert.That(arithmetic.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)arithmetic.LeftOperand).Value, Is.EqualTo("7"));
            Assert.That(arithmetic.RightOperand, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(((VariableReferenceExpression)arithmetic.RightOperand).Name, Is.EqualTo("foo"));
        }

        [Test]
        public void InvalidExpressionStatement()
        {
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IntegerLiteralToken("7", 0, 0));
            programTokens.Enqueue(new OperatorToken("%", 0, 0));
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new OperatorToken("==", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("0", 0, 0));
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            Assert.Throws<CompilationFailed>(() => parser.Parse());
            Assert.That(errorReporter.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OperatorPrecedences()
        { // 4 + 9 * (7 - 2 % 3) - 2
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new IdentifierToken("foo", 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
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
            EndStatement();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var errorReporter = new ErrorLogger();
            var parser = new Parser(new StubScanner(programTokens), errorReporter);
            var expression = parser.Parse().MainClass.MainMethod.MethodBody[0];
            Assert.That(expression, Is.InstanceOf<AssignmentStatement>());
            var assignment = (AssignmentStatement)expression;
            Assert.That(assignment.RightHandSide, Is.InstanceOf<BinaryOperatorExpression>());
            var minusOp = (BinaryOperatorExpression)assignment.RightHandSide;
            Assert.That(minusOp.Operator, Is.EqualTo("-"));
            Assert.That(minusOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(minusOp.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());
            var plusOp = (BinaryOperatorExpression)minusOp.LeftOperand;
            Assert.That(plusOp.Operator, Is.EqualTo("+"));
            Assert.That(plusOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(plusOp.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());
            var timesOp = (BinaryOperatorExpression)plusOp.RightOperand;
            Assert.That(timesOp.Operator, Is.EqualTo("*"));
            Assert.That(timesOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(timesOp.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());
            var parenthesisedMinusOp = (BinaryOperatorExpression)timesOp.RightOperand;
            Assert.That(parenthesisedMinusOp.Operator, Is.EqualTo("-"));
            Assert.That(parenthesisedMinusOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(parenthesisedMinusOp.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());
            var moduloOp = (BinaryOperatorExpression)parenthesisedMinusOp.RightOperand;
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
            Assert.That(((PrintStatement)mainMethod.MethodBody[2]).Argument, Is.InstanceOf<VariableReferenceExpression>());
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

            Assert.Throws<CompilationFailed>(() => GetProgramTree());
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
            EndStatement();
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
            EndStatement();
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
            EndStatement();
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
            Assert.That(ifStatement.Condition, Is.InstanceOf<BinaryOperatorExpression>());
            var boolExpression = (BinaryOperatorExpression)ifStatement.Condition;
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
            EndStatement();
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

        private void EndStatement()
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
            EndStatement();
        }

        private void AssignIntegerToVariable(string variableName, string integerValue)
        {
            programTokens.Enqueue(new IdentifierToken(variableName, 0, 0));
            programTokens.Enqueue(new OperatorToken("=", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken(integerValue, 0, 0));
            EndStatement();
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

        private void DeclareMainClass(string className)
        {
            DeclareMainClassUntilMainMethod(className);
            programTokens.Enqueue(new PunctuationToken("}", 0 ,0));
            programTokens.Enqueue(new PunctuationToken("}", 0, 0));
        }

        private void DeclareBasicVariable(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaTypeToken(type, 0, 0));
            programTokens.Enqueue(new IdentifierToken(name, 0, 0));
            EndStatement();
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
            EndStatement();
        }

        private void DeclareBasicArrayVariable(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaTypeToken(type, 0, 0));
            programTokens.Enqueue(new PunctuationToken("[", 0, 0));
            programTokens.Enqueue(new PunctuationToken("]", 0, 0));
            programTokens.Enqueue(new IdentifierToken(name, 0, 0));
            EndStatement();
        }
    }
}
