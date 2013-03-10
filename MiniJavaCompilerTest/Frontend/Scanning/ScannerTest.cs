using System.IO;
using MiniJavaCompiler.FrontEnd.LexicalAnalysis;
using NUnit.Framework;

namespace MiniJavaCompilerTest.FrontEndTest.Scanning
{
    [TestFixture]
    public class KeywordTest
    {
        [Datapoints]
        public string[] Keywords = {"this", "true", "false", "new", "length",
                                       "System", "out", "println", "if", "else", "while",
                                       "return", "assert", "public", "static", "main",
                                       "class", "extends" };

        [Theory]
        public void KeywordsAreRecognised(string @keyword)
        {
            var reader = new StringReader(@keyword);
            var lexer = new MiniJavaScanner(reader);
            IToken next = lexer.NextToken();
            Assert.That(next, Is.InstanceOf<KeywordToken>());
            Assert.That(((KeywordToken)next).Lexeme, Is.EqualTo(@keyword));
            reader.Close();
        }
    }

    [TestFixture]
    public class ArithmeticOperatorTest
    {
        [Datapoints]
        public string[] ArithmeticOperators = { "+", "-", "*", "/", "%" };

        [Theory]
        public void ArithmeticOperatorsAreRecognised(string @operator)
        {
            var reader = new StringReader(@operator);
            var lexer = new MiniJavaScanner(reader);
            IToken token = lexer.NextToken();
            Assert.That(token, Is.InstanceOf<OperatorToken>());
            Assert.That(((OperatorToken)token).Lexeme, Is.EqualTo(@operator));
            reader.Close();
        }
    }

    [TestFixture]
    public class LogicalOperatorTest
    {
        [Datapoints]
        public string[] LogicalOperators = { "<", ">", "&&", "==", "||", "!" };

        [Theory]
        public void LogicalOperatorsAreRecognised(string @operator)
        {
            var reader = new StringReader(@operator);
            var lexer = new MiniJavaScanner(reader);
            IToken token = lexer.NextToken();
            Assert.That(token, Is.InstanceOf<OperatorToken>());
            Assert.That(((OperatorToken)token).Lexeme, Is.EqualTo(@operator));
            reader.Close();
        }
    }

    [TestFixture]
    public class TypeTests
    {
        [Datapoints]
        public string[] Types = { "int", "boolean", "void" };

        [Theory]
        public void SimpleTypes(string @type)
        {
            var reader = new StringReader(@type);
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<MiniJavaTypeToken>());
            reader.Close();
        }

        [Theory]
        public void ArrayType(string @type)
        {
            var reader = new StringReader(@type + "[]");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<MiniJavaTypeToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            reader.Close();
        }
    }

    [TestFixture]
    public class LexerTests
    {
        [Test]
        public void ParameterSeparator()
        {
            var reader = new StringReader(",");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            reader.Close();
        }

        [Test]
        public void CurlyBraces()
        {
            var reader = new StringReader("{}");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            reader.Close();
        }

        [Test]
        public void MethodInvocation()
        {
            var reader = new StringReader("foo.bar()");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<IdentifierToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<IdentifierToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<PunctuationToken>());
            Assert.That(lexer.NextToken(), Is.InstanceOf<EndOfFile>());
            reader.Close();
        }

        [Test]
        public void IntegerConstants()
        {
            var reader = new StringReader("123");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Lexeme, Is.EqualTo("123"));
            reader.Close();

            reader = new StringReader("1 23");
            lexer = new MiniJavaScanner(reader);
            var token = (IntegerLiteralToken)lexer.NextToken();
            Assert.That(token.Lexeme, Is.EqualTo("1"));
            Assert.That(token.Row, Is.EqualTo(1));
            Assert.That(token.Col, Is.EqualTo(1));
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Lexeme, Is.EqualTo("23"));
            reader.Close();
        }

        [Test]
        public void TestEndOfFile()
        {
            var reader = new StringReader("");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<EndOfFile>());
            reader.Close();

            reader = new StringReader("123");
            lexer = new MiniJavaScanner(reader);
            lexer.NextToken();
            Assert.That(lexer.NextToken(), Is.InstanceOf<EndOfFile>());
            Assert.Throws<OutOfInput>(() => lexer.NextToken());
            reader.Close();
        }

        [Test]
        public void Identifiers()
        {
            var reader = new StringReader("42foo");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(((IntegerLiteralToken)lexer.NextToken()).Lexeme, Is.EqualTo("42"));
            IToken next = lexer.NextToken();
            Assert.That(next, Is.InstanceOf<IdentifierToken>());
            Assert.That(((IdentifierToken)next).Lexeme, Is.EqualTo("foo"));
            lexer = new MiniJavaScanner(new StringReader("f_o12a"));
            Assert.That(((IdentifierToken)lexer.NextToken()).Lexeme, Is.EqualTo("f_o12a"));
            reader.Close();
        }

        [Test]
        public void WhiteSpaceIsSkipped()
        {
            var reader = new StringReader("\n\t\v\n  foo");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(((IdentifierToken)lexer.NextToken()).Lexeme, Is.EqualTo("foo"));
            reader.Close();
        }

        [Test]
        public void CommentsAreSkipped()
        {
            var reader = new StringReader("// ... \n // ... \n foo");
            var lexer = new MiniJavaScanner(reader);
            var token = (IdentifierToken)lexer.NextToken();
            Assert.That(token.Lexeme, Is.EqualTo("foo"));
            Assert.That(token.Row, Is.EqualTo(3));
            Assert.That(token.Col, Is.EqualTo(2));
            reader.Close();

            reader = new StringReader("/* ... \n\n*/ \tfoo");
            lexer = new MiniJavaScanner(reader);
            token = (IdentifierToken)lexer.NextToken();
            Assert.That(token.Lexeme, Is.EqualTo("foo"));
            Assert.That(token.Row, Is.EqualTo(3));
            Assert.That(token.Col, Is.EqualTo(5));
            reader.Close();

            reader = new StringReader("\n\n// ...//\n// ... \n\n/* ... */ foo");
            lexer = new MiniJavaScanner(reader);
            Assert.That(((IdentifierToken)lexer.NextToken()).Lexeme, Is.EqualTo("foo"));
            reader.Close();
        }

        [Test]
        public void CommentsMayBeNested()
        {
            var reader = new StringReader("\n\n// ...//\n// ... \n\n/* ... /* ... */ ... */ foo");
            var lexer = new MiniJavaScanner(reader);
            Assert.That((lexer.NextToken()).Lexeme, Is.EqualTo("foo"));
            reader.Close();
        }

        [Test]
        public void CombinedWhiteSpaceAndComments()
        {
            var reader = new StringReader("\n\t\t// ... \n // ... \n     foo");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(((IdentifierToken)lexer.NextToken()).Lexeme, Is.EqualTo("foo"));
            reader.Close();
        }

        [Test]
        public void InputConsistingOfWhitespaceOnly()
        {
            var reader = new StringReader("\n   \t\t\v\r\n  ");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<EndOfFile>());
            reader.Close();
        }

        [Test]
        public void DivisionSymbolIsNotConfusedWithAComment()
        {
            var reader = new StringReader("/");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<OperatorToken>());
            reader.Close();

            reader = new StringReader("// .. / ..\n /");
            lexer = new MiniJavaScanner(reader);
            Assert.That(((OperatorToken)lexer.NextToken()).Lexeme, Is.EqualTo("/"));
            reader.Close();
        }

        [Test]
        public void AssignmentToken()
        {
            var reader = new StringReader("=");
            var lexer = new MiniJavaScanner(reader);
            Assert.That(lexer.NextToken(), Is.InstanceOf<OperatorToken>());
            reader.Close();
        }

        [Test]
        public void ShouldBeInvalid()
        {
            var reader = new StringReader("$");
            var scanner = new MiniJavaScanner(reader);
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            reader.Close();

            reader = new StringReader("&|");
            scanner = new MiniJavaScanner(reader);
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            reader.Close();
        }

        [Test]
        public void EndlessComment()
        {
            var reader = new StringReader("/* ... ");
            var scanner = new MiniJavaScanner(reader);
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            reader.Close();

            reader = new StringReader("/* ... /");
            scanner = new MiniJavaScanner(reader);
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            reader.Close();

            reader = new StringReader("/* ... *");
            scanner = new MiniJavaScanner(reader);
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            reader.Close();

            reader = new StringReader("/* ... /* ... /*");
            scanner = new MiniJavaScanner(reader);
            Assert.That(scanner.NextToken(), Is.InstanceOf<ErrorToken>());
            reader.Close();
        }

        [Test]
        public void ReportsRowsAndColumnsRightForTokens()
        {
            string program = "class Factorial {\n" +
                             "\t public static void main () {\n";
            var reader = new StringReader(program);
            var scanner = new MiniJavaScanner(reader);
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
            reader.Close();
        }

        [Test]
        public void ThrowsAnExceptionIfInputExhausted()
        {
            var reader = new StringReader("");
            var scanner = new MiniJavaScanner(reader);
            Assert.That(scanner.NextToken(), Is.InstanceOf<EndOfFile>());
            Assert.Throws<OutOfInput>(() => scanner.NextToken());
            reader.Close();
        }
    }
}