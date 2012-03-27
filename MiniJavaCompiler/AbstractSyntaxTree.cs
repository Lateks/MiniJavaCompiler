using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler
{
    namespace AbstractSyntaxTree
    {
        public interface SyntaxTreeNode { }

        public interface Statement : SyntaxTreeNode { }

        public interface Expression : SyntaxTreeNode { }

        public class SyntaxElement : SyntaxTreeNode
        {
            public int Row
            {
                get;
                private set;
            }
            public int Col
            {
                get;
                private set;
            }

            public SyntaxElement(int row, int col)
            {
                Row = row;
                Col = col;
            }
        }

        public class Program : SyntaxTreeNode
        {
            public MainClassDeclaration MainClass
            {
                get;
                private set;
            }
            public List<ClassDeclaration> Classes
            {
                get;
                private set;
            }

            public Program(MainClassDeclaration main_class,
                           List<ClassDeclaration> class_declarations)
            {
                MainClass = main_class;
                Classes = class_declarations;
            }
        }

        public class ClassDeclaration : SyntaxElement
        {
            public string Name
            {
                get;
                private set;
            }
            public string InheritedClass
            {
                get;
                private set;
            }
            public List<Declaration> Declarations
            {
                get;
                private set;
            }

            public ClassDeclaration(string name, string inherited,
                List<Declaration> declarations, int row, int col)
                : base(row, col) { }
        }

        public class MainClassDeclaration : SyntaxElement
        {
            public string Name
            {
                get;
                private set;
            }
            public List<Statement> MainMethod
            {
                get;
                private set;
            }

            public MainClassDeclaration(string name, List<Statement> mainMethod,
                int row, int col)
                : base(row, col)
            {
                Name = name;
                MainMethod = mainMethod;
            }
        }

        public abstract class Declaration : SyntaxElement
        {
            public string Name
            {
                get;
                private set;
            }
            public string Type
            {
                get;
                private set;
            }
            public bool IsArray
            {
                get;
                private set;
            }

            public Declaration(string name, string type, bool isArray,
                int row, int col)
                : base(row, col)
            {
                Name = name;
                Type = type;
                IsArray = isArray;
            }
        }

        public class MethodDeclaration : Declaration
        {
            public List<VariableDeclaration> Formals
            {
                get;
                private set;
            }
            public List<Statement> MethodBody
            {
                get;
                private set;
            }

            public MethodDeclaration(string name, string type, bool returnTypeIsArray,
                List<VariableDeclaration> formals, List<Statement> methodBody,
                int row, int col)
                : base(name, type, returnTypeIsArray, row, col)
            {
                Formals = formals;
                MethodBody = methodBody;
            }
        }

        public class VariableDeclaration : Declaration, Statement
        {
            public VariableDeclaration(string name, string type, bool isArray, int row, int col)
                : base(name, type, isArray, row, col) { }
        }

        public class PrintStatement : SyntaxElement, Statement
        {
            public Expression Expression
            {
                get;
                private set;
            }

            public PrintStatement(Expression expression, int row, int col)
                : base(row, col)
            {
                Expression = expression;
            }
        }

        public class ReturnStatement : SyntaxElement, Statement
        {
            public Expression Expression
            {
                get;
                private set;
            }

            public ReturnStatement(Expression expression, int row, int col)
                : base(row, col)
            {
                Expression = expression;
            }
        }

        public class BlockStatement : SyntaxElement, Statement
        {
            public List<Statement> Statements
            {
                get;
                private set;
            }

            public BlockStatement(List<Statement> statements, int row, int col)
                : base(row, col)
            {
                Statements = statements;
            }
        }

        public class AssertStatement : SyntaxElement, Statement
        {
            public Expression Expression
            {
                get;
                private set;
            }

            public AssertStatement(Expression expression, int row, int col)
                : base(row, col)
            {
                Expression = expression;
            }
        }

        public class AssignmentStatement : SyntaxElement, Statement
        {
            public Expression LHS
            {
                get;
                private set;
            }
            public Expression RHS
            {
                get;
                private set;
            }

            public AssignmentStatement(Expression lhs, Expression rhs, int row, int col)
                : base(row, col)
            {
                LHS = lhs;
                RHS = rhs;
            }
        }

        public class IfStatement : SyntaxElement, Statement
        {
            public Expression BooleanExpression
            {
                get;
                private set;
            }
            public Statement Then
            {
                get;
                private set;
            }
            public Statement Else
            {
                get;
                private set;
            }

            public IfStatement(Expression booleanExp, Statement thenBranch,
                Statement elseBranch, int row, int col)
                : base(row, col)
            {
                BooleanExpression = booleanExp;
                Then = thenBranch;
                Else = elseBranch;
            }
        }

        public class WhileStatement : SyntaxElement, Statement
        {
            public Expression BooleanExpression
            {
                get;
                private set;
            }
            public Statement LoopBody
            {
                get;
                private set;
            }

            public WhileStatement(Expression booleanExp, Statement loopBody,
                int row, int col)
                : base(row, col)
            {
                BooleanExpression = booleanExp;
                LoopBody = loopBody;
            }
        }

        public class MethodInvocation : SyntaxElement, Statement, Expression
        {
            public Expression MethodOwner
            {
                get;
                private set;
            }
            public string MethodName
            {
                get;
                private set;
            }
            public List<Expression> CallParameters
            {
                get;
                private set;
            }

            public MethodInvocation(Expression methodOwner, string methodName,
                List<Expression> callParameters, int row, int col)
                : base(row, col)
            {
                MethodOwner = methodOwner;
                MethodName = methodName;
                CallParameters = callParameters;
            }
        }

        public class InstanceCreation : SyntaxElement, Expression
        {
            public string Type
            {
                get;
                private set;
            }
            public Expression ArraySize
            {
                get;
                private set;
            }

            public InstanceCreation(string type, int row, int col, Expression arraySize = null)
                : base(row, col)
            {
                Type = type;
                ArraySize = arraySize; 
            }
        }

        public class UnaryNot : SyntaxElement, Expression
        {
            public Expression BooleanExpression
            {
                get;
                private set;
            }

            public UnaryNot(Expression booleanExp, int row, int col)
                : base(row, col)
            {
                BooleanExpression = booleanExp;
            }
        }

        public abstract class BinaryOperator : SyntaxElement, Expression
        {
            public string Symbol
            {
                get;
                private set;
            }
            public Expression LHS
            {
                get;
                private set;
            }
            public Expression RHS
            {
                get;
                private set;
            }

            public BinaryOperator(string opsymbol, Expression lhs, Expression rhs,
                int row, int col) : base(row, col)
            {
                Symbol = opsymbol;
                LHS = lhs;
                RHS = rhs;
            }
        }

        public class ArithmeticOp : BinaryOperator
        {
            public ArithmeticOp(string opsymbol, Expression lhs, Expression rhs,
                int row, int col)
                : base(opsymbol, lhs, rhs, row, col) { }
        }

        public class LogicalOp : BinaryOperator
        {
            public LogicalOp(string opsymbol, Expression lhs, Expression rhs,
                int row, int col)
                : base(opsymbol, lhs, rhs, row, col) { }
        }

        public class BooleanLiteral : SyntaxElement, Expression
        {
            public bool Value
            {
                get;
                private set;
            }

            public BooleanLiteral(bool value, int row, int col)
                : base(row, col)
            {
                Value = value;
            }
        }

        public class ThisExpression : SyntaxElement, Expression
        {
            public ThisExpression(int row, int col)
                : base(row, col) { }
        }

        public class ArrayIndexExpression : SyntaxElement, Expression
        {
            public Expression Array
            {
                get;
                private set;
            }
            public Expression Index
            {
                get;
                private set;
            }

            public ArrayIndexExpression(Expression arrayReference,
                Expression arrayIndex, int row, int col)
                : base(row, col)
            {
                Array = arrayReference;
                Index = arrayIndex;
            }
        }

        public class VariableReference : SyntaxElement, Expression
        {
            public string Name
            {
                get;
                private set;
            }

            public VariableReference(string name, int row, int col)
                : base(row, col)
            {
                Name = name;
            }
        }

        public class IntegerLiteral : SyntaxElement, Expression
        {
            public string Value
            {
                get;
                private set;
            }

            public IntegerLiteral(string value, int row, int col)
                : base(row, col)
            {
                Value = value;
            }
        }
    }
}
