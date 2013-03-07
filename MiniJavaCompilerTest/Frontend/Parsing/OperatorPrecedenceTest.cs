using System.IO;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Frontend.SyntaxAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
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
            var parser = new Parser(scanner, errorLog, true);
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
            var arithmeticExpression = (BinaryOperatorExpression) ((AssignmentStatement)
                ((MethodDeclaration)tree.MainClass.Declarations[0]).MethodBody[1]).RightHandSide;

            // ... 10 + new ...
            Assert.That(arithmeticExpression.Operator, Is.EqualTo(MiniJavaInfo.Operator.Add));
            Assert.That(arithmeticExpression.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());
            Assert.That(arithmeticExpression.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());

            // new ... foo() * 2
            var rightmostOp = (BinaryOperatorExpression) arithmeticExpression.RightOperand;
            Assert.That(rightmostOp.Operator, Is.EqualTo(MiniJavaInfo.Operator.Mul));
            Assert.That(rightmostOp.LeftOperand, Is.InstanceOf<MethodInvocation>());
            Assert.That((rightmostOp.LeftOperand as MethodInvocation).MethodOwner,
                        Is.InstanceOf<InstanceCreationExpression>());
            Assert.That(rightmostOp.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());

            // 2 + 4
            var sum = (BinaryOperatorExpression) rightmostOp.RightOperand;
            Assert.That(sum.Operator, Is.EqualTo(MiniJavaInfo.Operator.Add));
            Assert.That(sum.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(sum.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());

            // (...) * 10
            var leftmostOp = (BinaryOperatorExpression) arithmeticExpression.LeftOperand;
            Assert.That(leftmostOp.Operator, Is.EqualTo(MiniJavaInfo.Operator.Mul));
            Assert.That(leftmostOp.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());
            Assert.That(leftmostOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());

            // (4 + ...)
            var parenthesisedExpression = (BinaryOperatorExpression) leftmostOp.LeftOperand;
            Assert.That(parenthesisedExpression.Operator, Is.EqualTo(MiniJavaInfo.Operator.Add));
            Assert.That(parenthesisedExpression.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(parenthesisedExpression.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());

            // ... / 2
            var multLevelArithmeticExpression = (BinaryOperatorExpression) parenthesisedExpression.RightOperand;
            Assert.That(multLevelArithmeticExpression.Operator, Is.EqualTo(MiniJavaInfo.Operator.Div));
            Assert.That(multLevelArithmeticExpression.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());
            Assert.That(multLevelArithmeticExpression.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());

            // 5 % 2
            var lastExpr = (BinaryOperatorExpression) multLevelArithmeticExpression.LeftOperand;
            Assert.That(lastExpr.Operator, Is.EqualTo(MiniJavaInfo.Operator.Mod));
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
            var booleanExpression = (BinaryOperatorExpression)((AssignmentStatement)
                ((MethodDeclaration) tree.MainClass.Declarations[0]).MethodBody[4]).RightHandSide;
            
            // ... 10 || 10 ...
            Assert.That(booleanExpression.Operator, Is.EqualTo(MiniJavaInfo.Operator.Or));
            Assert.That(booleanExpression.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());
            Assert.That(booleanExpression.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());

            // ...foo() && foo
            var rightmostOperand = (BinaryOperatorExpression) booleanExpression.RightOperand;
            Assert.That(rightmostOperand.Operator, Is.EqualTo(MiniJavaInfo.Operator.And));
            Assert.That(rightmostOperand.RightOperand, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(rightmostOperand.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());

            // 10 < bar.foo()
            var smallerThanOp = (BinaryOperatorExpression) rightmostOperand.LeftOperand;
            Assert.That(smallerThanOp.Operator, Is.EqualTo(MiniJavaInfo.Operator.Lt));
            Assert.That(smallerThanOp.LeftOperand, Is.InstanceOf<IntegerLiteralExpression>());
            Assert.That(smallerThanOp.RightOperand, Is.InstanceOf<MethodInvocation>());

            // (...) && bar == 10
            var andOp = (BinaryOperatorExpression) booleanExpression.LeftOperand;
            Assert.That(andOp.Operator, Is.EqualTo(MiniJavaInfo.Operator.And));
            Assert.That(andOp.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());
            Assert.That(andOp.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());

            // bar == 10
            var eqOp = (BinaryOperatorExpression) andOp.RightOperand;
            Assert.That(eqOp.Operator, Is.EqualTo(MiniJavaInfo.Operator.Eq));
            Assert.That(eqOp.LeftOperand, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(eqOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());

            // ... new A() || bar.foo() ...
            var orOp = (BinaryOperatorExpression) andOp.LeftOperand;
            Assert.That(orOp.Operator, Is.EqualTo(MiniJavaInfo.Operator.Or));
            Assert.That(orOp.LeftOperand, Is.InstanceOf<BinaryOperatorExpression>());
            Assert.That(orOp.RightOperand, Is.InstanceOf<BinaryOperatorExpression>());

            // bar == new A()
            var instanceComparison = (BinaryOperatorExpression) orOp.LeftOperand;
            Assert.That(instanceComparison.Operator, Is.EqualTo(MiniJavaInfo.Operator.Eq));
            Assert.That(instanceComparison.LeftOperand, Is.InstanceOf<VariableReferenceExpression>());
            Assert.That(instanceComparison.RightOperand, Is.InstanceOf<InstanceCreationExpression>());

            // bar.foo() > 5
            var comparisonOp = (BinaryOperatorExpression) orOp.RightOperand;
            Assert.That(comparisonOp.Operator, Is.EqualTo(MiniJavaInfo.Operator.Gt));
            Assert.That(comparisonOp.LeftOperand, Is.InstanceOf<MethodInvocation>());
            Assert.That(comparisonOp.RightOperand, Is.InstanceOf<IntegerLiteralExpression>());
        }
    }
}
