using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.SemanticAnalysis;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler
{
    namespace AbstractSyntaxTree
    {
        public interface SyntaxTreeNode
        {
            void accept(NodeVisitor visitor);
        }

        public interface Statement : SyntaxTreeNode { }

        public interface Expression : SyntaxTreeNode { }

        public abstract class SyntaxElement : SyntaxTreeNode
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
            public Symbol Symbol
            {
                get;
                set;
            }

            public SyntaxElement(int row, int col)
            {
                Row = row;
                Col = col;
            }

            public abstract void accept(NodeVisitor visitor);
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

            public void accept(NodeVisitor visitor)
            {
                visitor.visit(MainClass);
                foreach (ClassDeclaration aClass in Classes)
                {
                    aClass.accept(visitor);
                }
                visitor.visit(this);
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
                : base(row, col)
            {
                Name = name;
                InheritedClass = inherited;
                Declarations = declarations;
            }

            public override void accept(NodeVisitor visitor)
            {
                foreach (Declaration decl in Declarations)
                {
                    decl.accept(visitor);
                }
                visitor.visit(this);
            }
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

            public override void accept(NodeVisitor visitor)
            {
                foreach (Statement statement in MainMethod)
                {
                    statement.accept(visitor);
                }
                visitor.visit(this);
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

            public override void accept(NodeVisitor visitor)
            {
                foreach (VariableDeclaration decl in Formals)
                {
                    decl.accept(visitor);
                }
                foreach (Statement statement in MethodBody)
                {
                    statement.accept(visitor);
                }
                visitor.visit(this);
            }
        }

        public class VariableDeclaration : Declaration, Statement
        {
            public VariableDeclaration(string name, string type, bool isArray, int row, int col)
                : base(name, type, isArray, row, col) { }

            public override void accept(NodeVisitor visitor)
            {
                visitor.visit(this);
            }
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

            public override void accept(NodeVisitor visitor)
            {
                Expression.accept(visitor);
                visitor.visit(this);
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

            public override void accept(NodeVisitor visitor)
            {
                Expression.accept(visitor);
                visitor.visit(this);
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

            public override void accept(NodeVisitor visitor)
            {
                foreach (Statement statement in Statements)
                {
                    statement.accept(visitor);
                }
                visitor.visit(this);
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

            public override void accept(NodeVisitor visitor)
            {
                Expression.accept(visitor);
                visitor.visit(this);
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

            public override void accept(NodeVisitor visitor)
            {
                RHS.accept(visitor);
                LHS.accept(visitor);
                visitor.visit(this);
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

            public override void accept(NodeVisitor visitor)
            {
                BooleanExpression.accept(visitor);
                Then.accept(visitor);
                Else.accept(visitor);
                visitor.visit(this);
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

            public override void accept(NodeVisitor visitor)
            {
                BooleanExpression.accept(visitor);
                LoopBody.accept(visitor);
                visitor.visit(this);
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

            public override void accept(NodeVisitor visitor)
            {
                MethodOwner.accept(visitor);
                foreach (Expression expr in CallParameters)
                {
                    expr.accept(visitor);
                }
                visitor.visit(this);
            }
        }

        public class InstanceCreationExpression : SyntaxElement, Expression
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

            public InstanceCreationExpression(string type, int row, int col, Expression arraySize = null)
                : base(row, col)
            {
                Type = type;
                ArraySize = arraySize; 
            }

            public override void accept(NodeVisitor visitor)
            {
                ArraySize.accept(visitor);
                visitor.visit(this);
            }
        }

        public class UnaryNotExpression : SyntaxElement, Expression
        {
            public Expression BooleanExpression
            {
                get;
                private set;
            }

            public UnaryNotExpression(Expression booleanExp, int row, int col)
                : base(row, col)
            {
                BooleanExpression = booleanExp;
            }

            public override void accept(NodeVisitor visitor)
            {
                BooleanExpression.accept(visitor);
                visitor.visit(this);
            }
        }

        public abstract class BinaryOpExpression : SyntaxElement, Expression
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

            public BinaryOpExpression(string opsymbol, Expression lhs, Expression rhs,
                int row, int col) : base(row, col)
            {
                Symbol = opsymbol;
                LHS = lhs;
                RHS = rhs;
            }
        }

        public class ArithmeticOpExpression : BinaryOpExpression
        {
            public ArithmeticOpExpression(string opsymbol, Expression lhs, Expression rhs,
                int row, int col)
                : base(opsymbol, lhs, rhs, row, col) { }

            public override void accept(NodeVisitor visitor)
            {
                RHS.accept(visitor);
                LHS.accept(visitor);
                visitor.visit(this);
            }
        }

        public class LogicalOpExpression : BinaryOpExpression
        {
            public LogicalOpExpression(string opsymbol, Expression lhs, Expression rhs,
                int row, int col)
                : base(opsymbol, lhs, rhs, row, col) { }

            public override void accept(NodeVisitor visitor)
            {
                RHS.accept(visitor);
                LHS.accept(visitor);
                visitor.visit(this);
            }
        }

        public class BooleanLiteralExpression : SyntaxElement, Expression
        {
            public bool Value
            {
                get;
                private set;
            }

            public BooleanLiteralExpression(bool value, int row, int col)
                : base(row, col)
            {
                Value = value;
            }

            public override void accept(NodeVisitor visitor)
            {
                visitor.visit(this);
            }
        }

        public class ThisExpression : SyntaxElement, Expression
        {
            public ThisExpression(int row, int col)
                : base(row, col) { }

            public override void accept(NodeVisitor visitor)
            {
                visitor.visit(this);
            }
        }

        public class ArrayIndexingExpression : SyntaxElement, Expression
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

            public ArrayIndexingExpression(Expression arrayReference,
                Expression arrayIndex, int row, int col)
                : base(row, col)
            {
                Array = arrayReference;
                Index = arrayIndex;
            }

            public override void accept(NodeVisitor visitor)
            {
                Index.accept(visitor);
                Array.accept(visitor);
                visitor.visit(this);
            }
        }

        public class VariableReferenceExpression : SyntaxElement, Expression
        {
            public string Name
            {
                get;
                private set;
            }

            public VariableReferenceExpression(string name, int row, int col)
                : base(row, col)
            {
                Name = name;
            }

            public override void accept(NodeVisitor visitor)
            {
                visitor.visit(this);
            }
        }

        public class IntegerLiteralExpression : SyntaxElement, Expression
        {
            public string Value
            {
                get;
                private set;
            }

            public IntegerLiteralExpression(string value, int row, int col)
                : base(row, col)
            {
                Value = value;
            }

            public override void accept(NodeVisitor visitor)
            {
                visitor.visit(this);
            }
        }
    }
}
