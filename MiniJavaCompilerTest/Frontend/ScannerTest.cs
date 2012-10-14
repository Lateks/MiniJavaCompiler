using NUnit.Framework;
using MiniJavaCompiler.LexicalAnalysis;
using System.IO;

namespace MiniJavaCompilerTest.Frontend
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
            var lexer = new MiniJavaScanner(new StringReader(keyword));
            IToken next = lexer.NextToken();
            Assert.That(next, Is.InstanceOf<KeywordToken>());
            Assert.That(((KeywordToken)next).Value, Is.EqualTo(keyword));
        }
    }

    [TestFixture]
    public class ArithmeticOperatorTest
    {
        [Datapoints]
        public string[] arithmeticOperators = { "+", "-", "*", "/", "%" };

        [Theory]
        public void ArithmeticOperators(string @operator)
        {
            var lexer = new MiniJavaScanner(new StringReader(@operator));
            IToken token = lexer.NextToken();
            Assert.That(token, Is.InstanceOf<OperatorToken>());
            Assert.That(((OperatorToken)token).Value, Is.EqualTo(@operator));
        }
    }

    [TestFixture]
    public class LogicalOperatorTest
    {
        [Datapoints]
        public string[] logicalOperators = { "<", ">", "&&", "==", "||", "!" };

        [Theory]
        public void LogicalOperators(string @operator)
        {
            var lexer = new MiniJavaScanner(new StringReader(@operator));
            IToken token = lexer.NextToken();
            Assert.That(token, Is.InstanceOf<OperatorToken>());
            Assert.That(((OperatorToken)token).Value, Is.EqualTo(@operator));
        }
    }

    [TestFixture]
    public class TypeTests
    {
        [Datapoints]
        public string[] types = { "int", "boolean", "void" };

        [Theory]
        public void SimpleTypes(string type)
        {
            var lexer = new MiniJavaScanner(new StringReader(type));
            Assert.That(lexer.NextToken(), Is.InstanceOf<MiniJavaTypeToken>());
        }

        [Theory]
        public void ArrayType(string type)
        {
            var lexer = new MiniJavaScanner(new StringReader(type + "[]"));
            Assert.That(lexer.NextToken(), Is.InstanceOf<MiniJavaTypeToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
        }
    }

    [TestFixture]
    public class LexerTests
    {
        [Test]
        public void ParameterSeparator()
        {
            var lexer = new MiniJavaScanner(new StringReader(","));
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
        }

        [Test]
        public void CurlyBraces()
        {
            var lexer = new MiniJavaScanner(new StringReader("{}"));
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
        }

        [Test]
        public void MethodInvocation()
        {
            var lexer = new MiniJavaScanner(new StringReader("foo.bar()"));
            Assert.That(lexer.NextToken(), Is.InstanceOf<IdentifierToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<IdentifierToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<EndOfFile>());
        }

        [Test]
        public void IntegerConstants()
        {
            var lexer = new MiniJavaScanner(new StringReader("123"));
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Value, Is.EqualTo("123"));
            lexer = new MiniJavaScanner(new StringReader("1 23"));
            var token = (IntegerLiteralToken)lexer.NextToken();
            Assert.That(token.Value, Is.EqualTo("1"));
            Assert.That(token.Row, Is.EqualTo(1));
            Assert.That(token.Col, Is.EqualTo(1));
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Value, Is.EqualTo("23"));
        }

        [Test]
        public void TestEndOfFile()
        {
            var lexer = new MiniJavaScanner(new StringReader(""));
            Assert.That(lexer.NextToken(), Is.InstanceOf<EndOfFile>());
            lexer = new MiniJavaScanner(new StringReader("123"));
            lexer.NextToken();
            Assert.That(lexer.NextToken(), Is.InstanceOf<EndOfFile>());
        }

        [Test]
        public void Identifiers()
        {
            var lexer = new MiniJavaScanner(new StringReader("42foo"));
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Value, Is.EqualTo("42"));
            IToken next = lexer.NextToken();
            Assert.That(next, Is.InstanceOf<IdentifierToken>());
            Assert.That(((IdentifierToken)next).Value, Is.EqualTo("foo"));
            lexer = new MiniJavaScanner(new StringReader("f_o12a"));
            Assert.That(((IdentifierToken)lexer.NextToken()).Value, Is.EqualTo("f_o12a"));
        }

        [Test]
        public void WhiteSpaceIsSkipped()
        {
            var lexer = new MiniJavaScanner(new StringReader("\n\t\v\n  foo"));
            Assert.That(((IdentifierToken)lexer.NextToken()).Value, Is.EqualTo("foo"));
        }

        [Test]
        public void CommentsAreSkipped()
        {
            var lexer = new MiniJavaScanner(new StringReader("// ... \n // ... \n foo"));
            var token = (IdentifierToken)lexer.NextToken();
            Assert.That(token.Value, Is.EqualTo("foo"));
            Assert.That(token.Row, Is.EqualTo(3));
            Assert.That(token.Col, Is.EqualTo(2));
            lexer = new MiniJavaScanner(new StringReader("/* ... \n\n*/ \tfoo"));
            token = (IdentifierToken)lexer.NextToken();
            Assert.That(token.Value, Is.EqualTo("foo"));
            Assert.That(token.Row, Is.EqualTo(3));
            Assert.That(token.Col, Is.EqualTo(5));
            lexer = new MiniJavaScanner(new StringReader("\n\n// ...//\n// ... \n\n/* ... */ foo"));
            Assert.That(((IdentifierToken)lexer.NextToken()).Value, Is.EqualTo("foo"));
        }

        [Test]
        public void CombinedWhiteSpaceAndComments()
        {
            var lexer = new MiniJavaScanner(new StringReader("\n\t\t// ... \n // ... \n     foo"));
            Assert.That(((IdentifierToken)lexer.NextToken()).Value, Is.EqualTo("foo"));
        }

        [Test]
        public void InputConsistingOfWhitespaceOnly()
        {
            var lexer = new MiniJavaScanner(new StringReader("\n   "));
            Assert.That(lexer.NextToken(), Is.InstanceOf<EndOfFile>());
        }

        [Test]
        public void DivisionSymbolIsNotConfusedWithAComment()
        {
            var lexer = new MiniJavaScanner(new StringReader("/"));
            Assert.That(lexer.NextToken(), Is.InstanceOf<OperatorToken>());
            lexer = new MiniJavaScanner(new StringReader("// .. / ..\n /"));
            Assert.That(((OperatorToken)lexer.NextToken()).Value, Is.EqualTo("/"));
        }

        [Test]
        public void AssignmentToken()
        {
            var lexer = new MiniJavaScanner(new StringReader("="));
            Assert.That(lexer.NextToken(), Is.InstanceOf<OperatorToken>());
        }

        [Test]
        public void ShouldBeInvalid()
        {
            var scanner = new MiniJavaScanner(new StringReader("$"));
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            scanner = new MiniJavaScanner(new StringReader("&|"));
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
        }

        [Test]
        public void EndlessComment()
        {
            var scanner = new MiniJavaScanner(new StringReader("/* ... "));
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            scanner = new MiniJavaScanner(new StringReader("/* ... /"));
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
        }

        [Test]
        public void ReportsRowsAndColumnsRightForTokens()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n";
            var scanner = new MiniJavaScanner(new StringReader(program));
            var token = scanner.NextToken();
            Assert.AreEqual(token.Row, 1);
            Assert.AreEqual(token.Col, 1);
            token = scanner.NextToken();
            Assert.AreEqual(token.Row, 1);
            Assert.AreEqual(token.Col, 7);
            token = scanner.NextToken();
            Assert.AreEqual(token.Row, 1);
            Assert.AreEqual(token.Col, 17);
            token = scanner.NextToken();
            Assert.AreEqual(token.Row, 2);
            Assert.AreEqual(token.Col, 3);
        }

        [Test]
        public void ThrowsAnExceptionIfInputExhausted()
        {
            var scanner = new MiniJavaScanner(new StringReader(""));
            Assert.That(scanner.NextToken(), Is.InstanceOf<EndOfFile>());
            Assert.Throws<OutOfInput>(() => scanner.NextToken());
        }
    }
}