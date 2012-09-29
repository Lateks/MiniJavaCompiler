using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.SemanticAnalysis;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.AbstractSyntaxTree
{
    public interface ISyntaxTreeNode
    {
        void Accept(INodeVisitor visitor);
    }

    public interface IStatement : ISyntaxTreeNode { }

    public interface IExpression : ISyntaxTreeNode { }

    public abstract class SyntaxElement : ISyntaxTreeNode
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

        protected SyntaxElement(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public abstract void Accept(INodeVisitor visitor);
    }

    public class Program : ISyntaxTreeNode
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

        public void Accept(INodeVisitor visitor)
        {
            visitor.Visit(MainClass);
            foreach (ClassDeclaration aClass in Classes)
            {
                aClass.Accept(visitor);
            }
            visitor.Visit(this);
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

        public override void Accept(INodeVisitor visitor)
        {
            foreach (Declaration decl in Declarations)
            {
                decl.Accept(visitor);
            }
            visitor.Visit(this);
        }
    }

    public class MainClassDeclaration : SyntaxElement
    {
        public string Name
        {
            get;
            private set;
        }
        public List<IStatement> MainMethod
        {
            get;
            private set;
        }

        public MainClassDeclaration(string name, List<IStatement> mainMethod,
            int row, int col)
            : base(row, col)
        {
            Name = name;
            MainMethod = mainMethod;
        }

        public override void Accept(INodeVisitor visitor)
        {
            foreach (IStatement statement in MainMethod)
            {
                statement.Accept(visitor);
            }
            visitor.Visit(this);
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

        protected Declaration(string name, string type, bool isArray,
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
        public List<IStatement> MethodBody
        {
            get;
            private set;
        }

        public MethodDeclaration(string name, string type, bool returnTypeIsArray,
            List<VariableDeclaration> formals, List<IStatement> methodBody,
            int row, int col)
            : base(name, type, returnTypeIsArray, row, col)
        {
            Formals = formals;
            MethodBody = methodBody;
        }

        public override void Accept(INodeVisitor visitor)
        {
            foreach (VariableDeclaration decl in Formals)
            {
                decl.Accept(visitor);
            }
            foreach (IStatement statement in MethodBody)
            {
                statement.Accept(visitor);
            }
            visitor.Visit(this);
        }
    }

    public class VariableDeclaration : Declaration, IStatement
    {
        public VariableDeclaration(string name, string type, bool isArray, int row, int col)
            : base(name, type, isArray, row, col) { }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class PrintStatement : SyntaxElement, IStatement
    {
        public IExpression Expression
        {
            get;
            private set;
        }

        public PrintStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            Expression = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Expression.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class ReturnStatement : SyntaxElement, IStatement
    {
        public IExpression Expression
        {
            get;
            private set;
        }

        public ReturnStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            Expression = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Expression.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class BlockStatement : SyntaxElement, IStatement
    {
        public List<IStatement> Statements
        {
            get;
            private set;
        }

        public BlockStatement(List<IStatement> statements, int row, int col)
            : base(row, col)
        {
            Statements = statements;
        }

        public override void Accept(INodeVisitor visitor)
        {
            foreach (IStatement statement in Statements)
            {
                statement.Accept(visitor);
            }
            visitor.Visit(this);
        }
    }

    public class AssertStatement : SyntaxElement, IStatement
    {
        public IExpression Expression
        {
            get;
            private set;
        }

        public AssertStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            Expression = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Expression.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class AssignmentStatement : SyntaxElement, IStatement
    {
        public IExpression LHS
        {
            get;
            private set;
        }
        public IExpression RHS
        {
            get;
            private set;
        }

        public AssignmentStatement(IExpression lhs, IExpression rhs, int row, int col)
            : base(row, col)
        {
            LHS = lhs;
            RHS = rhs;
        }

        public override void Accept(INodeVisitor visitor)
        {
            RHS.Accept(visitor);
            LHS.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class IfStatement : SyntaxElement, IStatement
    {
        public IExpression BooleanExpression
        {
            get;
            private set;
        }
        public IStatement Then
        {
            get;
            private set;
        }
        public IStatement Else
        {
            get;
            private set;
        }

        public IfStatement(IExpression booleanExp, IStatement thenBranch,
            IStatement elseBranch, int row, int col)
            : base(row, col)
        {
            BooleanExpression = booleanExp;
            Then = thenBranch;
            Else = elseBranch;
        }

        public override void Accept(INodeVisitor visitor)
        {
            BooleanExpression.Accept(visitor);
            Then.Accept(visitor);
            Else.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class WhileStatement : SyntaxElement, IStatement
    {
        public IExpression BooleanExpression
        {
            get;
            private set;
        }
        public IStatement LoopBody
        {
            get;
            private set;
        }

        public WhileStatement(IExpression booleanExp, IStatement loopBody,
            int row, int col)
            : base(row, col)
        {
            BooleanExpression = booleanExp;
            LoopBody = loopBody;
        }

        public override void Accept(INodeVisitor visitor)
        {
            BooleanExpression.Accept(visitor);
            LoopBody.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class MethodInvocation : SyntaxElement, IStatement, IExpression
    {
        public IExpression MethodOwner
        {
            get;
            private set;
        }
        public string MethodName
        {
            get;
            private set;
        }
        public List<IExpression> CallParameters
        {
            get;
            private set;
        }

        public MethodInvocation(IExpression methodOwner, string methodName,
            List<IExpression> callParameters, int row, int col)
            : base(row, col)
        {
            MethodOwner = methodOwner;
            MethodName = methodName;
            CallParameters = callParameters;
        }

        public override void Accept(INodeVisitor visitor)
        {
            MethodOwner.Accept(visitor);
            foreach (IExpression expr in CallParameters)
            {
                expr.Accept(visitor);
            }
            visitor.Visit(this);
        }
    }

    public class InstanceCreationExpression : SyntaxElement, IExpression
    {
        public string Type
        {
            get;
            private set;
        }
        public IExpression ArraySize
        {
            get;
            private set;
        }

        public InstanceCreationExpression(string type, int row, int col, IExpression arraySize = null)
            : base(row, col)
        {
            Type = type;
            ArraySize = arraySize;
        }

        public override void Accept(INodeVisitor visitor)
        {
            ArraySize.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class UnaryNotExpression : SyntaxElement, IExpression
    {
        public IExpression BooleanExpression
        {
            get;
            private set;
        }

        public UnaryNotExpression(IExpression booleanExp, int row, int col)
            : base(row, col)
        {
            BooleanExpression = booleanExp;
        }

        public override void Accept(INodeVisitor visitor)
        {
            BooleanExpression.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class BinaryOpExpression : SyntaxElement, IExpression
    {
        public string Operator
        {
            get;
            private set;
        }
        public IExpression LeftOperand
        {
            get;
            private set;
        }
        public IExpression RightOperand
        {
            get;
            private set;
        }

        public BinaryOpExpression(string opsymbol, IExpression lhs, IExpression rhs,
            int row, int col)
            : base(row, col)
        {
            Operator = opsymbol;
            LeftOperand = lhs;
            RightOperand = rhs;
        }

        public override void Accept(INodeVisitor visitor)
        {
            RightOperand.Accept(visitor);
            LeftOperand.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class BooleanLiteralExpression : SyntaxElement, IExpression
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

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class ThisExpression : SyntaxElement, IExpression
    {
        public ThisExpression(int row, int col)
            : base(row, col) { }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class ArrayIndexingExpression : SyntaxElement, IExpression
    {
        public IExpression Array
        {
            get;
            private set;
        }
        public IExpression Index
        {
            get;
            private set;
        }

        public ArrayIndexingExpression(IExpression arrayReference,
            IExpression arrayIndex, int row, int col)
            : base(row, col)
        {
            Array = arrayReference;
            Index = arrayIndex;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Index.Accept(visitor);
            Array.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class VariableReferenceExpression : SyntaxElement, IExpression
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

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class IntegerLiteralExpression : SyntaxElement, IExpression
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

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
