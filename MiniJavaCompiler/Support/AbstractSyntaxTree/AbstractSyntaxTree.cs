using System.Collections.Generic;

namespace MiniJavaCompiler.Support.AbstractSyntaxTree
{
    public interface ISyntaxTreeNode
    {
        void Accept(INodeVisitor visitor);
    }

    public interface IStatement : ISyntaxTreeNode { }

    public interface IExpression : ISyntaxTreeNode
    {
        string Describe();
    }

    public abstract class SyntaxElement : ISyntaxTreeNode
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        protected SyntaxElement(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public abstract void Accept(INodeVisitor visitor);
    }

    public class Program : ISyntaxTreeNode
    {
        public MainClassDeclaration MainClass { get; private set; }
        public List<ClassDeclaration> Classes { get; private set; }

        public Program(MainClassDeclaration mainClass,
                       List<ClassDeclaration> classDeclarations)
        {
            MainClass = mainClass;
            Classes = classDeclarations;
        }

        public void Accept(INodeVisitor visitor)
        {
            MainClass.Accept(visitor);
            foreach (var aClass in Classes)
            {
                aClass.Accept(visitor);
            }
            visitor.Visit(this);
        }
    }

    public class ClassDeclaration : SyntaxElement
    {
        public string Name { get; private set; }
        public string InheritedClass { get; private set; }
        public List<Declaration> Declarations { get; private set; }

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
            visitor.Visit(this);
            foreach (Declaration decl in Declarations)
            {
                decl.Accept(visitor);
            }
            visitor.Exit(this);
        }
    }

    public class MainClassDeclaration : SyntaxElement
    {
        public string Name { get; private set; }
        public MethodDeclaration MainMethod { get; private set; }

        public MainClassDeclaration(string name, List<IStatement> mainMethod,
            int row, int col, int mainMethodRow, int mainMethodCol)
            : base(row, col)
        {
            Name = name;
            MainMethod = new MethodDeclaration("main", "void", false,
                new List<VariableDeclaration>(), mainMethod, mainMethodRow, mainMethodCol, true);
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
            MainMethod.Accept(visitor);
            visitor.Exit(this);
        }
    }

    public abstract class Declaration : SyntaxElement
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public bool IsArray { get; private set; }

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
        public List<VariableDeclaration> Formals { get; private set; }
        public List<IStatement> MethodBody { get; private set; }
        public bool IsStatic { get; private set; }

        public MethodDeclaration(string name, string type, bool returnTypeIsArray,
            List<VariableDeclaration> formals, List<IStatement> methodBody,
            int row, int col, bool isStatic = false)
            : base(name, type, returnTypeIsArray, row, col)
        {
            Formals = formals;
            MethodBody = methodBody;
            IsStatic = isStatic;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
            foreach (var decl in Formals)
            {
                decl.Accept(visitor);
            }
            foreach (var statement in MethodBody)
            {
                statement.Accept(visitor);
            }
            visitor.Exit(this);
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
        public IExpression Argument { get; private set; }

        public PrintStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            Argument = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Argument.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class ReturnStatement : SyntaxElement, IStatement
    {
        public IExpression ReturnValue { get; private set; }

        public ReturnStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            ReturnValue = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            ReturnValue.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class BlockStatement : SyntaxElement, IStatement
    {
        public List<IStatement> Statements { get; private set; }

        public BlockStatement(List<IStatement> statements, int row, int col)
            : base(row, col)
        {
            Statements = statements;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
            foreach (var statement in Statements)
            {
                statement.Accept(visitor);
            }
            visitor.Exit(this);
        }
    }

    public class AssertStatement : SyntaxElement, IStatement
    {
        public IExpression Condition { get; private set; }

        public AssertStatement(IExpression expression, int row, int col)
            : base(row, col)
        {
            Condition = expression;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Condition.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class AssignmentStatement : SyntaxElement, IStatement
    {
        public IExpression LeftHandSide { get; private set; }
        public IExpression RightHandSide { get; private set; }

        public AssignmentStatement(IExpression lhs, IExpression rhs, int row, int col)
            : base(row, col)
        {
            LeftHandSide = lhs;
            RightHandSide = rhs;
        }

        public override void Accept(INodeVisitor visitor)
        {
            RightHandSide.Accept(visitor);
            LeftHandSide.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class IfStatement : SyntaxElement, IStatement
    {
        public IExpression Condition { get; private set; }
        public BlockStatement ThenBranch { get; private set; }
        public BlockStatement ElseBranch { get; private set; }

        public IfStatement(IExpression booleanExp, IStatement thenBranch,
            IStatement elseBranch, int row, int col)
            : base(row, col)
        {
            Condition = booleanExp;
            ThenBranch = WrapInBlock(thenBranch);
            ElseBranch = WrapInBlock(elseBranch);
        }

        private BlockStatement WrapInBlock(IStatement statement)
        {
            if (statement == null) // Can be null if errors are encountered in the parsing phase.
            {
                return null;
            }
            if (statement is BlockStatement)
            {
                return statement as BlockStatement;
            }
            var statementNode = (SyntaxElement) statement;
            return new BlockStatement(new List<IStatement>() { statement },
                statementNode.Row, statementNode.Col);
        }

        public override void Accept(INodeVisitor visitor)
        {
            Condition.Accept(visitor);
            ThenBranch.Accept(visitor);
            if (ElseBranch != null)
            {
                ElseBranch.Accept(visitor);
            }
            visitor.Visit(this);
        }
    }

    public class WhileStatement : SyntaxElement, IStatement
    {
        public IExpression LoopCondition { get; private set; }
        public BlockStatement LoopBody { get; private set; }

        public WhileStatement(IExpression booleanExp, IStatement loopBody,
            int row, int col)
            : base(row, col)
        {
            LoopCondition = booleanExp;
            if (loopBody == null)
            {
                LoopBody = null;
            }
            else
            {
                LoopBody = loopBody is BlockStatement
                               ? loopBody as BlockStatement
                               : new BlockStatement(new List<IStatement>() { loopBody }, row, col);
            }
        }

        public override void Accept(INodeVisitor visitor)
        {
            LoopCondition.Accept(visitor);
            LoopBody.Accept(visitor);
            visitor.Visit(this);
        }
    }

    public class MethodInvocation : SyntaxElement, IStatement, IExpression
    {
        public IExpression MethodOwner { get; private set; }
        public string MethodName { get; private set; }
        public List<IExpression> CallParameters { get; private set; }

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
            foreach (var expr in CallParameters)
            {
                expr.Accept(visitor);
            }
            MethodOwner.Accept(visitor);
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "method invocation";
        }
    }

    public class InstanceCreationExpression : SyntaxElement, IExpression
    {
        public string Type { get;  private set; }
        public bool IsArrayCreation { get; private set; }
        public IExpression ArraySize { get; private set; }

        public InstanceCreationExpression(string type, int row, int col, IExpression arraySize = null)
            : base(row, col)
        {
            Type = type;
            ArraySize = arraySize;
            IsArrayCreation = arraySize != null;
        }

        public override void Accept(INodeVisitor visitor)
        {
            if (ArraySize != null)
            {
                ArraySize.Accept(visitor);
            }
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "instance creation expression";
        }
    }

    public class UnaryOperatorExpression : SyntaxElement, IExpression
    {
        public IExpression Operand { get; private set; }
        public string Operator { get; private set; }

        public UnaryOperatorExpression(string opSymbol, IExpression operand, int row, int col)
            : base(row, col)
        {
            Operator = opSymbol;
            Operand = operand;
        }

        public override void Accept(INodeVisitor visitor)
        {
            Operand.Accept(visitor);
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "unary operator expression";
        }
    }

    public class BinaryOperatorExpression : SyntaxElement, IExpression
    {
        public string Operator { get; private set; }
        public IExpression LeftOperand { get; private set; }
        public IExpression RightOperand { get; private set; }

        public BinaryOperatorExpression(string opsymbol, IExpression lhs, IExpression rhs,
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

        public string Describe()
        {
            return "binary operator expression";
        }
    }

    public class BooleanLiteralExpression : SyntaxElement, IExpression
    {
        public bool Value { get; private set; }

        public BooleanLiteralExpression(bool value, int row, int col)
            : base(row, col)
        {
            Value = value;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "boolean literal";
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

        public string Describe()
        {
            return "self reference (this)";
        }
    }

    public class ArrayIndexingExpression : SyntaxElement, IExpression
    {
        public IExpression Array { get; private set; }
        public IExpression Index { get; private set; }

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

        public string Describe()
        {
            return "array indexing expression";
        }
    }

    public class VariableReferenceExpression : SyntaxElement, IExpression
    {
        public string Name { get; private set; }

        public VariableReferenceExpression(string name, int row, int col)
            : base(row, col)
        {
            Name = name;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "variable reference";
        }
    }

    public class IntegerLiteralExpression : SyntaxElement, IExpression
    {
        public string Value { get; private set; }

        public IntegerLiteralExpression(string value, int row, int col)
            : base(row, col)
        {
            Value = value;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Describe()
        {
            return "integer literal";
        }
    }
}
