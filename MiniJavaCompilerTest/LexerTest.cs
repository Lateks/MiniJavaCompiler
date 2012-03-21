using System;
using NUnit.Framework;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support.TokenTypes;
using MiniJavaCompiler.Support.Errors.Compilation;
using System.IO;

namespace LexerTest
{
    [TestFixture]
    public class KeywordTest
    {
        [Datapoints]
        public string[] keywords = {"this", "true", "false", "new", "length",
                                       "System", "out", "println", "if", "else", "while",
                                       "return", "assert", "public", "static", "main",
                                       "class", "extends" };

        [Theory]
        public void Keywords(string keyword)
        {
            var lexer = new Scanner(new StringReader(keyword));
            Token next = lexer.NextToken();
            Assert.That(next, Is.InstanceOf<KeywordToken>());
            Assert.That(((KeywordToken)next).Value, Is.EqualTo(keyword));
        }
    }

    [TestFixture]
    public class OperatorTest
    {
        [Datapoints]
        public string[] binaryOperators = { "+", "-", "*", "/", "<", ">", "&&", "==", "||", "%" };

        [Theory]
        public void BinaryOperators(string binop)
        {
            var lexer = new Scanner(new StringReader(binop));
            Token token = lexer.NextToken();
            Assert.That(token, Is.InstanceOf<BinaryOperator>());
            Assert.That(((BinaryOperator)token).Value, Is.EqualTo(binop));
        }

        [Test]
        public void UnaryNot()
        {
            var lexer = new Scanner(new StringReader("!"));
            Assert.That(lexer.NextToken(), Is.InstanceOf<UnaryNotToken>());
        }
    }

    [TestFixture]
    public class TypeTests
    {
        [Datapoints]
        public string[] types = { "int", "boolean" };

        [Theory]
        public void SimpleTypes(string type)
        {
            var lexer = new Scanner(new StringReader(type));
            Assert.That(lexer.NextToken(), Is.InstanceOf<MiniJavaType>());
        }

        [Theory]
        public void ArrayType(string type)
        {
            var lexer = new Scanner(new StringReader(type + "[]"));
            Assert.That(lexer.NextToken(), Is.InstanceOf<MiniJavaType>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<LeftBracket>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<RightBracket>());
        }
    }

    [TestFixture]
    public class LexerTests
    {
        [Test]
        public void MethodInvocation()
        {
            var lexer = new Scanner(new StringReader("foo.bar()"));
            Assert.That(lexer.NextToken(), Is.InstanceOf<Identifier>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<MethodInvocationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<Identifier>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<LeftParenthesis>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<RightParenthesis>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<EOF>());
        }

        [Test]
        public void IntegerConstants()
        {
            var lexer = new Scanner(new StringReader("123"));
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Value, Is.EqualTo("123"));
            lexer = new Scanner(new StringReader("1 23"));
            var token = (IntegerLiteralToken)lexer.NextToken();
            Assert.That(token.Value, Is.EqualTo("1"));
            Assert.That(token.Row, Is.EqualTo(1));
            Assert.That(token.Col, Is.EqualTo(1));
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Value, Is.EqualTo("23"));
        }

        [Test]
        public void TestEOF()
        {
            var lexer = new Scanner(new StringReader(""));
            Assert.That(lexer.NextToken(), Is.InstanceOf<EOF>());
            lexer = new Scanner(new StringReader("123"));
            lexer.NextToken();
            Assert.That(lexer.NextToken(), Is.InstanceOf<EOF>());
        }

        [Test]
        public void Identifiers()
        {
            var lexer = new Scanner(new StringReader("42foo"));
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Value, Is.EqualTo("42"));
            Token next = lexer.NextToken();
            Assert.That(next, Is.InstanceOf<Identifier>());
            Assert.That(((Identifier)next).Value, Is.EqualTo("foo"));
            lexer = new Scanner(new StringReader("f_o12a"));
            Assert.That(((Identifier)lexer.NextToken()).Value, Is.EqualTo("f_o12a"));
        }

        [Test]
        public void WhiteSpaceIsSkipped()
        {
            var lexer = new Scanner(new StringReader("\n\t\v\n  foo"));
            Assert.That(((Identifier)lexer.NextToken()).Value, Is.EqualTo("foo"));
        }

        [Test]
        public void CommentsAreSkipped()
        {
            var lexer = new Scanner(new StringReader("// ... \n // ... \n foo"));
            var token = (Identifier)lexer.NextToken();
            Assert.That(token.Value, Is.EqualTo("foo"));
            Assert.That(token.Row, Is.EqualTo(3));
            Assert.That(token.Col, Is.EqualTo(2));
            lexer = new Scanner(new StringReader("/* ... \n\n*/ \tfoo"));
            token = (Identifier)lexer.NextToken();
            Assert.That(token.Value, Is.EqualTo("foo"));
            Assert.That(token.Row, Is.EqualTo(3));
            Assert.That(token.Col, Is.EqualTo(5));
            lexer = new Scanner(new StringReader("\n\n// ...//\n// ... \n\n/* ... */ foo"));
            Assert.That(((Identifier)lexer.NextToken()).Value, Is.EqualTo("foo"));
        }

        [Test]
        public void CombinedWhiteSpaceAndComments()
        {
            var lexer = new Scanner(new StringReader("\n\t\t// ... \n // ... \n     foo"));
            Assert.That(((Identifier)lexer.NextToken()).Value, Is.EqualTo("foo"));
        }

        [Test]
        public void InputConsistingOfWhitespaceOnly()
        {
            var lexer = new Scanner(new StringReader("\n   "));
            Assert.That(lexer.NextToken(), Is.InstanceOf<EOF>());
        }

        [Test]
        public void DivisionSymbolIsNotConfusedWithAComment()
        {
            var lexer = new Scanner(new StringReader("/"));
            Assert.That(lexer.NextToken(), Is.InstanceOf<BinaryOperator>());
            lexer = new Scanner(new StringReader("// .. / ..\n /"));
            Assert.That(((BinaryOperator)lexer.NextToken()).Value, Is.EqualTo("/"));
        }

        [Test]
        public void AssignmentToken()
        {
            var lexer = new Scanner(new StringReader("="));
            Assert.That(lexer.NextToken(), Is.InstanceOf<AssignmentToken>());
        }

        [Test]
        public void ShouldBeInvalid()
        {
            var scanner = new Scanner(new StringReader("$"));
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            scanner = new Scanner(new StringReader("&|"));
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
        }

        [Test]
        public void EndlessComment()
        {
            var scanner = new Scanner(new StringReader("/* ... "));
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            scanner = new Scanner(new StringReader("/* ... /"));
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
        }
    }
}