using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MiniJavaCompiler.SyntaxAnalysis;
using MiniJavaCompiler.AbstractSyntaxTree;
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
                return new EOF(0, 0);
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

        private Program GetProgramTree()
        {
            return new Parser(new StubScanner(programTokens)).Parse();
        }

        [Test]
        public void ValidClassDeclarationWithExtension()
        { // class ClassName extends OtherClass { }
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new Identifier("ClassName", 0, 0));
            programTokens.Enqueue(new KeywordToken("extends", 0, 0));
            programTokens.Enqueue(new Identifier("OtherClass", 0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
            programTokens.Enqueue(new RightCurlyBrace(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
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
            programTokens.Enqueue(new Identifier("ClassName", 0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
            programTokens.Enqueue(new MiniJavaType("int", 0, 0));
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));
            programTokens.Enqueue(new KeywordToken("public", 0, 0));
            programTokens.Enqueue(new MiniJavaType("void", 0, 0));
            programTokens.Enqueue(new Identifier("bar", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
            programTokens.Enqueue(new RightCurlyBrace(0, 0));
            programTokens.Enqueue(new RightCurlyBrace(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            var classDecl = parser.ClassDeclaration();
            Assert.IsNull(classDecl.InheritedClass);
            Assert.NotNull(classDecl.Declarations);
            Assert.That(classDecl.Declarations.Count, Is.EqualTo(2));
        }

        [Test]
        public void BasicTypeVariableDeclaration()
        { // int foo;
            programTokens.Enqueue(new MiniJavaType("int", 0, 0));
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            EndLine();

            var parser = new Parser(new StubScanner(programTokens));
            var variableDecl = parser.VariableDeclaration();
            Assert.False(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("int"));
        }

        [Test]
        public void UserDefinedTypeVariableDeclaration()
        { // someType foo;
            programTokens.Enqueue(new Identifier("SomeType", 0, 0));
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            EndLine();

            var parser = new Parser(new StubScanner(programTokens));
            var variableDecl = parser.VariableDeclaration();
            Assert.False(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("SomeType"));
        }

        [Test]
        public void ArrayVariableDeclaration()
        { // int[] foo;
            programTokens.Enqueue(new MiniJavaType("int", 0, 0));
            programTokens.Enqueue(new LeftBracket(0, 0));
            programTokens.Enqueue(new RightBracket(0, 0));
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            EndLine();

            var parser = new Parser(new StubScanner(programTokens));
            var variableDecl = parser.VariableDeclaration();
            Assert.True(variableDecl.IsArray);
            Assert.That(variableDecl.Name, Is.EqualTo("foo"));
            Assert.That(variableDecl.Type, Is.EqualTo("int"));
        }

        [Test]
        public void AssertStatement()
        { // assert(true);
            programTokens.Enqueue(new KeywordToken("assert", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            EndLine();

            var parser = new Parser(new StubScanner(programTokens));
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<AssertStatement>());
            Assert.That(((AssertStatement)statement).Expression, Is.InstanceOf<BooleanLiteralExpression>());
        }

        [Test]
        public void PrintStatement()
        { // System.out.println(5);
            programTokens.Enqueue(new KeywordToken("System", 0, 0));
            programTokens.Enqueue(new MethodInvocationToken(0, 0));
            programTokens.Enqueue(new KeywordToken("out", 0, 0));
            programTokens.Enqueue(new MethodInvocationToken(0, 0));
            programTokens.Enqueue(new KeywordToken("println", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("5", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new EndLine(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<PrintStatement>());
            Assert.That(((PrintStatement)statement).Expression, Is.InstanceOf<IntegerLiteralExpression>());
        }

        [Test]
        public void WhileStatement()
        { // while (true) assert(false);
            programTokens.Enqueue(new KeywordToken("while", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new KeywordToken("assert", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new KeywordToken("false", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new EndLine(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<WhileStatement>());
            var whileStatement = (WhileStatement)statement;
            Assert.That(whileStatement.BooleanExpression, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.That(whileStatement.LoopBody, Is.InstanceOf<AssertStatement>());
        }

        [Test]
        public void ReturnStatement()
        { // return foo;
            programTokens.Enqueue(new KeywordToken("return", 0, 0));
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<ReturnStatement>());
            Assert.That(((ReturnStatement)statement).Expression, Is.InstanceOf<VariableReferenceExpression>());
        }

        [Test]
        public void MethodInvocationStatement()
        { // foo.bar();
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new MethodInvocationToken(0, 0));
            programTokens.Enqueue(new Identifier("bar", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new EndLine(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
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
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new LeftBracket(0, 0));
            programTokens.Enqueue(new RightBracket(0, 0));
            programTokens.Enqueue(new Identifier("bar", 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<VariableDeclaration>());
            Assert.True(((VariableDeclaration)statement).IsArray);
        }

        [Test]
        public void AssignmentToArrayStatement()
        { // foo[5] = true;
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new LeftBracket(0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("5", 0, 0));
            programTokens.Enqueue(new RightBracket(0, 0));
            programTokens.Enqueue(new AssignmentToken(0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            var statement = parser.Statement();
            Assert.That(statement, Is.InstanceOf<AssignmentStatement>());
            var assignment = (AssignmentStatement)statement;
            Assert.That(assignment.RHS, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.That(assignment.LHS, Is.InstanceOf<ArrayIndexingExpression>());
        }

        [Test]
        public void TryingToAssignToArrayWithoutAnIndexExpression()
        { // foo[] = 42;
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new LeftBracket(0, 0));
            programTokens.Enqueue(new RightBracket(0, 0));
            programTokens.Enqueue(new AssignmentToken(0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("42", 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            Assert.Null(parser.Statement());
            Assert.That(parser.errorMessages.Count, Is.GreaterThan(0));
        }

        [Test]
        public void AnyExpressionDoesNotQualifyAsAStatement()
        { // 42;
            programTokens.Enqueue(new IntegerLiteralToken("42", 0, 0));
            programTokens.Enqueue(new EndLine(0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            Assert.Null(parser.Statement());
            Assert.That(parser.errorMessages.Count, Is.GreaterThan(0));
        }

        [Test]
        public void BinaryOperatorExpression()
        { // 7 % foo == 0
            programTokens.Enqueue(new IntegerLiteralToken("7", 0, 0));
            programTokens.Enqueue(new ArithmeticOperatorToken("%", 0, 0));
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new LogicalOperatorToken("==", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("0", 0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            var expression = parser.Expression();
            Assert.That(expression, Is.InstanceOf<LogicalOpExpression>());
            var logicalOp = (LogicalOpExpression)expression;
            Assert.That(logicalOp.RHS, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)logicalOp.RHS).Value, Is.EqualTo("0"));
            Assert.That(logicalOp.LHS, Is.InstanceOf<ArithmeticOpExpression>());
            var arithmetic = (ArithmeticOpExpression)logicalOp.LHS;
            Assert.That(arithmetic.LHS, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)arithmetic.LHS).Value, Is.EqualTo("7"));
            Assert.That(arithmetic.RHS, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(((VariableReferenceExpression)arithmetic.RHS).Name, Is.EqualTo("foo"));
        }

        [Test]
        public void OperatorPrecedences()
        { // 4 + 9 * (7 - 2 % 3) - 2
            programTokens.Enqueue(new IntegerLiteralToken("4", 0, 0));
            programTokens.Enqueue(new ArithmeticOperatorToken("+", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("9", 0, 0));
            programTokens.Enqueue(new ArithmeticOperatorToken("*", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("7", 0, 0));
            programTokens.Enqueue(new ArithmeticOperatorToken("-", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("2", 0, 0));
            programTokens.Enqueue(new ArithmeticOperatorToken("%", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("3", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new ArithmeticOperatorToken("-", 0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("2", 0, 0));

            var parser = new Parser(new StubScanner(programTokens));
            var expression = parser.Expression();
            Assert.That(expression, Is.InstanceOf<ArithmeticOpExpression>());
            var minusOp = (ArithmeticOpExpression)expression;
            Assert.That(minusOp.Symbol, Is.EqualTo("-"));
            Assert.That(minusOp.RHS, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(minusOp.LHS, Is.InstanceOf<ArithmeticOpExpression>());
            var plusOp = (ArithmeticOpExpression)minusOp.LHS;
            Assert.That(plusOp.Symbol, Is.EqualTo("+"));
            Assert.That(plusOp.LHS, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(plusOp.RHS, Is.InstanceOf<ArithmeticOpExpression>());
            var timesOp = (ArithmeticOpExpression)plusOp.RHS;
            Assert.That(timesOp.Symbol, Is.EqualTo("*"));
            Assert.That(timesOp.LHS, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(timesOp.RHS, Is.InstanceOf<ArithmeticOpExpression>());
            var parenthesisedMinusOp = (ArithmeticOpExpression)timesOp.RHS;
            Assert.That(parenthesisedMinusOp.Symbol, Is.EqualTo("-"));
            Assert.That(parenthesisedMinusOp.LHS, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(parenthesisedMinusOp.RHS, Is.InstanceOf<ArithmeticOpExpression>());
            var moduloOp = (ArithmeticOpExpression)parenthesisedMinusOp.RHS;
            Assert.That(moduloOp.Symbol, Is.EqualTo("%"));
            Assert.That(moduloOp.LHS, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(moduloOp.RHS, Is.InstanceOf<IntegerLiteralExpression>());
        }

        [Test]
        public void SimpleMainClassWithEmptyMainMethod()
        {   /* class ThisIsTheMainClass {
             *     public static void main() { }
             * }
             * <EOF>
             */
            DeclareMainClassUntilMainMethod("ThisIsTheMainClass");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            Program programTree = GetProgramTree();

            Assert.That(programTree.Classes.Count, Is.EqualTo(0));
            Assert.That(programTree.MainClass.Name, Is.EqualTo("ThisIsTheMainClass"));
            Assert.That(programTree.MainClass.MainMethod.Count, Is.EqualTo(0));
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
             * <EOF>
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
            Assert.That(programTree.MainClass.MainMethod.Count, Is.EqualTo(3));

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod[0], Is.InstanceOf<VariableDeclaration>());

            var fooDecl = (VariableDeclaration)mainMethod[0];
            Assert.That(fooDecl.Name, Is.EqualTo("foo"));
            Assert.That(fooDecl.IsArray, Is.EqualTo(false));

            Assert.That(mainMethod[1], Is.InstanceOf<AssignmentStatement>());

            var assignment = (AssignmentStatement)mainMethod[1];
            Assert.That(assignment.LHS, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(((VariableReferenceExpression)assignment.LHS).Name, Is.EqualTo("foo"));
            Assert.That(assignment.RHS, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)assignment.RHS).Value, Is.EqualTo("42"));
            Assert.That(mainMethod[2], Is.InstanceOf<PrintStatement>());
            Assert.That(((PrintStatement)mainMethod[2]).Expression, Is.InstanceOf<VariableReferenceExpression>());
        }

        [Test]
        public void CreatingABaseTypeArray()
        {   /* class ThisIsTheMainClass {
             *     public static void main() {
             *         int[] foo;
             *         foo = new int[10];
             *     }
             * }
             * <EOF>
             */
            DeclareMainClassUntilMainMethod("ThisIsTheMainClass");
            DeclareBasicArrayVariable("foo", "int");
            AssignNewArrayToVariable("foo", "int", "10");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            Assert.That(programTree.Classes.Count, Is.EqualTo(0));

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.Count, Is.EqualTo(2));
            Assert.That(mainMethod[0], Is.InstanceOf<VariableDeclaration>());

            var decl = (VariableDeclaration)mainMethod[0];
            Assert.That(decl.Name, Is.EqualTo("foo"));
            Assert.That(decl.Type, Is.EqualTo("int"));
            Assert.True(decl.IsArray);
            Assert.That(mainMethod[1], Is.InstanceOf<AssignmentStatement>());

            var assignment = (AssignmentStatement)mainMethod[1];
            Assert.That(assignment.LHS, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(assignment.RHS, Is.InstanceOf<InstanceCreationExpression>());

            var newinstance = (InstanceCreationExpression)assignment.RHS;
            Assert.That(newinstance.Type, Is.EqualTo("int"));
            Assert.That(newinstance.ArraySize, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(((IntegerLiteralExpression)newinstance.ArraySize).Value, Is.EqualTo("10"));
        }

        [Test]
        public void MainMethodWithFormalParametersCausesSyntaxError()
        {   /* class MainClass {
             *     public static void main(int foo) { }
             * }
             * <EOF>
             */
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new Identifier("MainClass", 0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
            programTokens.Enqueue(new KeywordToken("public", 0, 0));
            programTokens.Enqueue(new KeywordToken("static", 0, 0));
            programTokens.Enqueue(new MiniJavaType("void", 0, 0));
            programTokens.Enqueue(new KeywordToken("main", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new MiniJavaType("int", 0, 0));
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
            EndFile();

            Assert.Throws<BackEndError>(() => GetProgramTree());
        }

        [Test]
        public void MethodWithoutParameters()
        {   /* class MainClass {
             *     public static void main() { }
             * }
             * class someClass {
             *     public int someMethod() { }
             * }
             * <EOF>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            ClosingCurlyBrace(); ClosingCurlyBrace();

            BeginClassDeclaration("someClass");
            BeginMethodDeclaration("someMethod", "int");
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
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
             * <EOF>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            MakeMethodInvocationWithoutParentheses("someClass", "someMethod");
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new IntegerLiteralToken("42", 0, 0));
            programTokens.Enqueue(new ParameterSeparator(0, 0));
            programTokens.Enqueue(new Identifier("parameterVariable", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            EndLine();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.Count, Is.EqualTo(1));
            Assert.That(mainMethod[0], Is.InstanceOf<MethodInvocation>());
            var methodInvocation = (MethodInvocation)mainMethod[0];
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
             * <EOF>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            MakeMethodInvocationWithoutParentheses("someClass", "someMethod");
            EmptyMethodInvocationParentheses();
            EndLine();
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var programTree = GetProgramTree();

            var mainMethod = programTree.MainClass.MainMethod;
            Assert.That(mainMethod.Count, Is.EqualTo(1));
            Assert.That(mainMethod[0], Is.InstanceOf<MethodInvocation>());
            var methodInvocation = (MethodInvocation)mainMethod[0];
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
             * <EOF>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            ClosingCurlyBrace(); ClosingCurlyBrace();

            BeginClassDeclaration("anotherClass");
            BeginMethodDeclaration("someMethod", "int");
            DefineBasicParameter("foo", "int");
            programTokens.Enqueue(new ParameterSeparator(0, 0));
            DefineOwnTypeParameter("bar", "myOwnType");
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
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
             * <EOF>
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
            Assert.That(mainMethod.Count, Is.EqualTo(1));
            Assert.That(mainMethod[0], Is.InstanceOf<MethodInvocation>());
            var lengthMethodInvocation = (MethodInvocation)mainMethod[0];
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
             * <EOF>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new KeywordToken("if", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new BinaryOperatorToken("&&", 0, 0));
            programTokens.Enqueue(new KeywordToken("false", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            AssignIntegerToVariable("foo", "42");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var mainMethod = GetProgramTree().MainClass.MainMethod;
            Assert.That(mainMethod.Count, Is.EqualTo(1));
            Assert.That(mainMethod[0], Is.InstanceOf<IfStatement>());
            var ifStatement = (IfStatement)mainMethod[0];
            Assert.That(ifStatement.Then, Is.InstanceOf<AssignmentStatement>());
            Assert.IsNull(ifStatement.Else);
            Assert.That(ifStatement.BooleanExpression, Is.InstanceOf<BinaryOpExpression>());
            var boolExpression = (BinaryOpExpression)ifStatement.BooleanExpression;
            Assert.That(boolExpression.LHS, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.IsTrue(((BooleanLiteralExpression)boolExpression.LHS).Value);
            Assert.That(boolExpression.RHS, Is.InstanceOf<BooleanLiteralExpression>());
            Assert.IsFalse(((BooleanLiteralExpression)boolExpression.RHS).Value);
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
             * <EOF>
             */
            DeclareMainClassUntilMainMethod("MainClass");
            programTokens.Enqueue(new KeywordToken("if", 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new KeywordToken("true", 0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
            programTokens.Enqueue(new Identifier("foo", 0, 0));
            programTokens.Enqueue(new MethodInvocationToken(0, 0));
            programTokens.Enqueue(new Identifier("bar", 0, 0));
            EmptyMethodInvocationParentheses();
            EndLine();
            programTokens.Enqueue(new KeywordToken("else", 0, 0));
            AssignIntegerToVariable("foo", "42");
            ClosingCurlyBrace(); ClosingCurlyBrace();
            EndFile();

            var mainMethod = GetProgramTree().MainClass.MainMethod;
            Assert.That(mainMethod.Count, Is.EqualTo(1));
            Assert.That(mainMethod[0], Is.InstanceOf<IfStatement>());
            var ifStatement = (IfStatement)mainMethod[0];
            Assert.That(ifStatement.Then, Is.InstanceOf<MethodInvocation>());
            Assert.That(ifStatement.Else, Is.InstanceOf<AssignmentStatement>());
        }

        private void EndLine()
        {
            programTokens.Enqueue(new EndLine(0, 0));
        }

        private void InvokeMethod(string methodName)
        {
            programTokens.Enqueue(new MethodInvocationToken(0, 0));
            programTokens.Enqueue(new Identifier(methodName, 0, 0));
        }

        private void EmptyMethodInvocationParentheses()
        {
            programTokens.Enqueue(new LeftParenthesis(0, 0));
            programTokens.Enqueue(new RightParenthesis(0, 0));
        }

        private void MakeMethodInvocationWithoutParentheses(string className, string methodName)
        {
            programTokens.Enqueue(new Identifier(className, 0, 0));
            programTokens.Enqueue(new MethodInvocationToken(0, 0));
            programTokens.Enqueue(new Identifier(methodName, 0, 0));
        }

        private void DefineOwnTypeParameter(string name, string type)
        {
            programTokens.Enqueue(new Identifier(type, 0, 0));
            programTokens.Enqueue(new Identifier(name, 0, 0));
        }

        private void DefineBasicParameter(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaType(type, 0, 0));
            programTokens.Enqueue(new Identifier(name, 0, 0));
        }

        private void BeginMethodDeclaration(string methodName, string type)
        {
            programTokens.Enqueue(new KeywordToken("public", 0, 0));
            programTokens.Enqueue(new MiniJavaType(type, 0, 0));
            programTokens.Enqueue(new Identifier(methodName, 0, 0));
            programTokens.Enqueue(new LeftParenthesis(0, 0));
        }

        private void BeginClassDeclaration(string className)
        {
            programTokens.Enqueue(new KeywordToken("class", 0, 0));
            programTokens.Enqueue(new Identifier(className, 0, 0));
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
        }

        private void ClosingCurlyBrace()
        {
            programTokens.Enqueue(new RightCurlyBrace(0, 0));
        }

        private void EndFile()
        {
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
            EndLine();
        }

        private void AssignIntegerToVariable(string variableName, string integerValue)
        {
            programTokens.Enqueue(new Identifier(variableName, 0, 0));
            programTokens.Enqueue(new AssignmentToken(0, 0));
            programTokens.Enqueue(new IntegerLiteralToken(integerValue, 0, 0));
            EndLine();
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
            EmptyMethodInvocationParentheses();
            programTokens.Enqueue(new LeftCurlyBrace(0, 0));
        }

        private void DeclareBasicVariable(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaType(type, 0, 0));
            programTokens.Enqueue(new Identifier(name, 0, 0));
            EndLine();
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
            EndLine();
        }

        private void DeclareBasicArrayVariable(string name, string type)
        {
            programTokens.Enqueue(new MiniJavaType(type, 0, 0));
            programTokens.Enqueue(new LeftBracket(0, 0));
            programTokens.Enqueue(new RightBracket(0, 0));
            programTokens.Enqueue(new Identifier(name, 0, 0));
            EndLine();
        }
    }
}
