using System.IO;
using MiniJavaCompiler.FrontEnd.LexicalAnalysis;
using MiniJavaCompiler.FrontEnd.SyntaxAnalysis;
using MiniJavaCompiler.Support;
using NUnit.Framework;
using MiniJavaCompiler.Support.AbstractSyntaxTree;

namespace MiniJavaCompilerTest.FrontEndTest.Parsing
{
    [TestFixture]
    public class ParserRecoveryTest
    {
        private ErrorLogger _errorLog;
        private IParser _parser;

        public void SetUpParser(string program)
        {
            var scanner = new MiniJavaScanner(new StringReader(program));
            _errorLog = new ErrorLogger();
            _parser = new Parser(scanner, _errorLog);
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Expected '}' but got keyword 'class'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected type name but got operator '+'"));
        }

        [Test]
        public void RecoveryFromClassMatchingWhenLexicalErrors()
        {
            string program = "class Foo_Bar$ { }\n" + // there is a lexical error on this row
                             "class Bar { public Foo_Bar bar(, int foo) { } }"; // should detect the error here
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected type name but got punctuation token ','"));
        }

        [Test]
        public void ErrorsWhenEndlessCommentEncountered()
        {
            string program = "class Foo { /* public static void main() { } }\n" +
                             "class Bar { public Foo bar(int foo) { return new Foo(); } }";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(1));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Reached end of input while scanning for a comment"));
        }

        [Test]
        public void MissingClosingParenthesisForClassAtTheEndOfProgram()
        {
            string program = "class Foo { public static void main() { System.out.println(42); }";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(1));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Reached end of file while parsing for '}'")); // encountered end of file instead of the expected token
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Invalid token 'class' of type keyword starting a declaration"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Reached end of file while parsing for '}'"));
        }

        [Test]
        public void LexicalErrorInADeclaration()
        {
            string program = "class Foo { public static void main() { System.out.println(42); } }\n" +
                             "class A {\n" +
                             "\t int #;\n" +
                             "\t public $ foo() { }\n" +
                             "\t public void foo() {\n" +
                             "\t\t int foo$;?\n" +
                             "\t}\n" +
                             "\t int bar;\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(6));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '#'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[3].ToString(), Is.StringContaining("Unexpected token '?'"));
            Assert.That(_errorLog.Errors[4].ToString(), Is.StringContaining("Invalid token 'class' of type keyword starting a declaration")); // expecting a method declaration but found "class B"
            Assert.That(_errorLog.Errors[5].ToString(), Is.StringContaining("Reached end of file while parsing for '}'")); // attempted to recover from declaration matching but recovery ended at end of file
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(4));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Expected ';' but got built-in type 'int'")); // recovers until the end of the statement "int foo;"
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected identifier but got punctuation token ';'")); // invalid method declaration caught, recovers until the next }
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Invalid token 'class' of type keyword starting a declaration")); // did not expect a new class
            Assert.That(_errorLog.Errors[3].ToString(), Is.StringContaining("Reached end of file while parsing for '}'")); // recovery ended by end of file
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(4));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Expected ';' but got identifier 'bar'")); // recovers until the end of the statement "bar = @;"
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Unexpected token '@'"));
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[3].ToString(), Is.StringContaining("Unexpected token '#'"));
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Expected ';' but got keyword 'assert'")); // recovers until the end of the second assertion
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected ';' but got keyword 'return'")); // recovers until the end of the return statement
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(3));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '@'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Unexpected token '#'"));
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Unexpected token '^'"));
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(3));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Invalid token ';' of type punctuation token starting a declaration"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Invalid token 'class' of type keyword starting a declaration")); // recovered until the end of the class declaration
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Reached end of file while parsing for '}'")); // recovery from declaration matching ended up at the end of file
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
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(),
                Is.StringContaining("Invalid start token ';' of type punctuation token for an expression")); // Expected an expression because a punctuation token other than
                                                                                                             // '{' cannot start another kind of statement.
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Reached end of file while parsing for '}'")); // Recovered from method body parsing and trying to parse
                                                                                                                           // the rest of this method declaration but recovery has ended
                                                                                                                           // up at the end of file. (Statement parsing recovers until
                                                                                                                           // the next ';').
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
            Program ast;
            Assert.True(_parser.TryParse(out ast));
        }

        [Test]
        public void InvalidStartTokensForExpressions()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() {\n" +
                             "\t\t boolean foo;\n" +
                             "\t\t foo = assert(true);\n" +
                             "\t\t int bar;\n" +
                             "\t\t bar = { };\n" +
                             "\t\t return if foo 1 else 0;\n" +
                             "\t }\n" +
                             "}";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(4));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Invalid keyword 'assert' starting an expression"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected ';' but got punctuation token ')'")); // recovery was done until ), so we get this extra error
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Invalid start token '{' of type punctuation token for an expression"));
            Assert.That(_errorLog.Errors[3].ToString(), Is.StringContaining("Invalid keyword 'if' starting an expression"));
        }

        [Test]
        public void LexicalErrorsInPlaceOfOperators()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() {\n" +
                             "\t\t int foo;\n" +
                             "\t\t foo = ~42;\n" +
                             "\t\t int bar;\n" +
                             "\t\t bar = 10 $ 45 ~ @;\n" +
                             "\t }\n" +
                             "}";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(4));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '~'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Unexpected token '$'")); // Was expecting an operator but found this. => Returns the left hand side of the expression (the numeric literal).
                                                                                                      // The error is reported when trying to match end of statement (;).
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Unexpected token '~'")); // The recovery routine then runs until the next statement end symbol and reports other lexical errors.
            Assert.That(_errorLog.Errors[3].ToString(), Is.StringContaining("Unexpected token '@'"));
        }

        [Test]
        public void ValidNonOperatorTokenInPlaceOfAnOperator()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() {\n" +
                             "\t\t int foo;\n" +
                             "\t\t foo = 10 ; 45;\n" +
                             "\t\t foo = foo = 1;\n" +
                             "\t }\n" +
                             "}";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Expression of type integer literal cannot form a statement"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected ';' but got operator '='"));
        }

        [Test]
        public void UnaryOperatorTokenInPlaceOfABinaryOperatorToken()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() {\n" +
                             "\t\t int foo;\n" +
                             "\t\t foo = 10 ! 45;\n" +
                             "\t\t foo = foo = 1;\n" +
                             "\t }\n" +
                             "}";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Expected ';' but got operator '!'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected ';' but got operator '='"));
        }

        [Test]
        public void RecoversWhenThereIsASyntaxErrorInAClassDeclaration()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { }\n" +
                             "}" +
                             "class public { }\n" +
                             "class B extends int { }\n";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Expected identifier but got keyword 'public'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected identifier but got built-in type 'int'"));
        }

        [Test]
        public void LexicalErrorStartsADeclaration()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { }\n" +
                             "}" +
                             "class B { � foo; $ int foo() { } }\n";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '�'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Unexpected token '$'"));
        }

        [Test]
        public void LexicalErrorInAClassDeclaration()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { }\n" +
                             "}" +
                             "class B & { }\n" +
                             "class $ { }\n";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(2));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '&'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Unexpected token '$'"));
        }


        [Test]
        public void SyntaxOrLexicalErrorInAParameterList()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { }\n" +
                             "}" +
                             "class B {\n" +
                             "\t public int foo($ foo, boolean boolean) { return 0; }\n" +
                             "\t public int bar() { boolean public; public = false; return this.foo($, public); }\n" +
                             "}\n";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(6));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected identifier but got built-in type 'boolean'"));
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Expected identifier but got keyword 'public'"));
            Assert.That(_errorLog.Errors[3].ToString(), Is.StringContaining("Invalid keyword 'public' starting an expression"));
            Assert.That(_errorLog.Errors[4].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[5].ToString(), Is.StringContaining("Invalid keyword 'public' starting an expression"));
        }

        [Test]
        public void ErrorInAKeywordExpression()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { int foo; foo = new int(); B bar; bar = new $[10]; }\n" +
                             "}" +
                             "class B { }\n";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(4));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Expected '[' but got punctuation token '('"));
            Assert.That(_errorLog.Errors[1].ToString(), Is.StringContaining("Expected ';' but got punctuation token ')'")); // incomplete recovery due to large follow set
            Assert.That(_errorLog.Errors[2].ToString(), Is.StringContaining("Unexpected token '$'"));
            Assert.That(_errorLog.Errors[3].ToString(), Is.StringContaining("Expected ';' but got punctuation token '['")); // same as above
        }

        [Test]
        public void EndOfFileInTheMiddleOfAnExpression()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { foo = true && !";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(1));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Reached end of file while parsing an expression"));
        }

        [Test]
        public void LexicalErrorInsideAnExpression()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { int foo; foo = 1 + #; }}\n";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(1));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Unexpected token '#'"));
        }

        [Test]
        public void EndOfFileInTheMiddleOfAVariableDeclaration()
        {
            string program = "class Foo {\n" +
                             "\t public static void main() { A[";
            SetUpParser(program);
            Program ast;
            Assert.False(_parser.TryParse(out ast));
            Assert.That(_errorLog.Errors.Count, Is.EqualTo(1));
            Assert.That(_errorLog.Errors[0].ToString(), Is.StringContaining("Reached end of file"));
        }
    }
}
