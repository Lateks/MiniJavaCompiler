using System.IO;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.SyntaxAnalysis;
using NUnit.Framework;

namespace MiniJavaCompilerTest.Frontend.Parsing
{
    [TestFixture]
    class OperatorPrecedenceTest
    {
        public Program ParseProgram(string program)
        {
            var scanner = new MiniJavaScanner(new StringReader(program));
            var errorLog = new ErrorLogger();
            var parser = new Parser(new ParserInputReader(scanner, errorLog), errorLog, true);
            return parser.Parse();
        }

        [Test]
        public void TestArithmeticExpressions()
        {
            var program = "class Main {\n" +
                          "\t public static void main() {\n" +
                          "\t\t int foo;\n" +
                          "\t\t foo = (4 + 5 % 2 / 2) * 10 + new A().foo() * (2 + 4);" +
                          "\t }\n" +
                          "}\n" +
                          "class A { public int foo() { return 7; } }\n";
            var tree = ParseProgram(program);
            var arithmeticExpression = (BinaryOpExpression) ((AssignmentStatement) tree.MainClass.MainMethod.MethodBody[1]).RightHandSide;

            // ... 10 + new ...
            Assert.That(arithmeticExpression.Operator, Is.EqualTo("+"));
            Assert.That(arithmeticExpression.LeftOperand, Is.InstanceOf<BinaryOpExpression>());
            Assert.That(arithmeticExpression.RightOperand, Is.InstanceOf<BinaryOpExpression>());

            // new ... foo() * 2
            var rightmostOp = (BinaryOpExpression) arithmeticExpression.RightOperand;
            Assert.That(rightmostOp.Operator, Is.EqualTo("*"));
            Assert.That(rightmostOp.LeftOperand, Is.InstanceOf<MethodInvocation>());
            Assert.That((rightmostOp.LeftOperand as MethodInvocation).MethodOwner,
                        Is.InstanceOf<InstanceCreationExpression>());
            Assert.That(rightmostOp.RightOperand, Is.InstanceOf<BinaryOpExpression>());

            // 2 + 4
            var sum = (BinaryOpExpression) rightmostOp.RightOperand;
            Assert.That(sum.Operator, Is.EqualTo("+"));
            Assert.That(sum.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(sum.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());

            // (...) * 10
            var leftmostOp = (BinaryOpExpression) arithmeticExpression.LeftOperand;
            Assert.That(leftmostOp.Operator, Is.EqualTo("*"));
            Assert.That(leftmostOp.LeftOperand, Is.InstanceOf<BinaryOpExpression>());
            Assert.That(leftmostOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());

            // (4 + ...)
            var parenthesisedExpression = (BinaryOpExpression) leftmostOp.LeftOperand;
            Assert.That(parenthesisedExpression.Operator, Is.EqualTo("+"));
            Assert.That(parenthesisedExpression.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(parenthesisedExpression.RightOperand, Is.InstanceOf<BinaryOpExpression>());

            // ... / 2
            var multLevelArithmeticExpression = (BinaryOpExpression) parenthesisedExpression.RightOperand;
            Assert.That(multLevelArithmeticExpression.Operator, Is.EqualTo("/"));
            Assert.That(multLevelArithmeticExpression.LeftOperand, Is.InstanceOf<BinaryOpExpression>());
            Assert.That(multLevelArithmeticExpression.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());

            // 5 % 2
            var lastExpr = (BinaryOpExpression) multLevelArithmeticExpression.LeftOperand;
            Assert.That(lastExpr.Operator, Is.EqualTo("%"));
            Assert.That(lastExpr.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(lastExpr.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());
        }

        [Test]
        public void TestBooleanExpressions()
        {
            var program = "class Main {\n" +
                          "\t public static void main() {\n" +
                          "\t\t A bar;\n" +
                          "\t\t bar = new A();\n" +
                          "\t\t boolean foo;\n" +
                          "\t\t foo = true;\n" +
                          "\t\t foo = (bar == new A() || bar.foo() > 5) && bar == 10 || 10 < bar.foo() && foo;" +
                          "\t }\n" +
                          "}\n" +
                          "class A { public int foo() { return 10; } }\n";
            var tree = ParseProgram(program);
            var booleanExpression = (BinaryOpExpression)((AssignmentStatement)tree.MainClass.MainMethod.MethodBody[4]).RightHandSide;
            
            // ... 10 || 10 ...
            Assert.That(booleanExpression.Operator, Is.EqualTo("||"));
            Assert.That(booleanExpression.LeftOperand, Is.InstanceOf<BinaryOpExpression>());
            Assert.That(booleanExpression.RightOperand, Is.InstanceOf<BinaryOpExpression>());

            // ...foo() && foo
            var rightmostOperand = (BinaryOpExpression) booleanExpression.RightOperand;
            Assert.That(rightmostOperand.Operator, Is.EqualTo("&&"));
            Assert.That(rightmostOperand.RightOperand, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(rightmostOperand.LeftOperand, Is.InstanceOf<BinaryOpExpression>());

            // 10 < bar.foo()
            var smallerThanOp = (BinaryOpExpression) rightmostOperand.LeftOperand;
            Assert.That(smallerThanOp.Operator, Is.EqualTo("<"));
            Assert.That(smallerThanOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(smallerThanOp.RightOperand, Is.InstanceOf<MethodInvocation>());

            // (...) && bar == 10
            var andOp = (BinaryOpExpression) booleanExpression.LeftOperand;
            Assert.That(andOp.Operator, Is.EqualTo("&&"));
            Assert.That(andOp.LeftOperand, Is.InstanceOf<BinaryOpExpression>());
            Assert.That(andOp.RightOperand, Is.InstanceOf<BinaryOpExpression>());

            // bar == 10
            var eqOp = (BinaryOpExpression) andOp.RightOperand;
            Assert.That(eqOp.Operator, Is.EqualTo("=="));
            Assert.That(eqOp.LeftOperand, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(eqOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());

            // ... new A() || bar.foo() ...
            var orOp = (BinaryOpExpression) andOp.LeftOperand;
            Assert.That(orOp.Operator, Is.EqualTo("||"));
            Assert.That(orOp.LeftOperand, Is.InstanceOf<BinaryOpExpression>());
            Assert.That(orOp.RightOperand, Is.InstanceOf<BinaryOpExpression>());

            // bar == new A()
            var instanceComparison = (BinaryOpExpression) orOp.LeftOperand;
            Assert.That(instanceComparison.Operator, Is.EqualTo("=="));
            Assert.That(instanceComparison.LeftOperand, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(instanceComparison.RightOperand, Is.InstanceOf<InstanceCreationExpression>());

            // bar.foo() > 5
            var comparisonOp = (BinaryOpExpression) orOp.RightOperand;
            Assert.That(comparisonOp.Operator, Is.EqualTo(">"));
            Assert.That(comparisonOp.LeftOperand, Is.InstanceOf<MethodInvocation>());
            Assert.That(comparisonOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());
        }
    }
}
