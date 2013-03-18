using MiniJavaCompiler.Support.AbstractSyntaxTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.Support
{
    public enum ErrorTypes
    {
        Lexical,
        Syntax,
        InvalidOverride,
        CyclicInheritance,
        MethodReference,
        LvalueReference,
        TypeReference,
        ConflictingDefinitions,
        TypeError,
        UninitializedLocal
    }

    // This error can be used in all stages of compilation
    // to indicate failure.
    public class CompilationError : Exception { }

    // And exception with row and column information.
    // Used as a base class for different frontend exceptions
    // that require row and column information.
    public abstract class CodeError : Exception
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        public CodeError(string message, int row, int col)
            : base(message)
        {
            Row = row;
            Col = col;
        }
    }

    public interface IErrorReporter
    {
        void ReportError(ErrorTypes type, string message, int row, int col);
        void ReportError(ErrorTypes type, string message, SyntaxElement node);
        void ReportError(ErrorTypes type, string message, SyntaxElement node, SyntaxElement referredNode);
        bool HasErrorReportForNode(ErrorTypes type, SyntaxElement node);
        bool HasErrorReportForReferenceTo(ErrorTypes type, Declaration node);
        List<ErrorMessage> Errors { get; }
        int Count { get; }
    }

    public class ErrorReporterFactory
    {
        public static IErrorReporter CreateErrorLogger()
        {
            return new ErrorLogger();
        }
    }

    public class ErrorLogger : IErrorReporter
    {
        public List<ErrorMessage> Errors
        {
            get;
            private set;
        }

        public ErrorLogger()
        {
            Errors = new List<ErrorMessage>();
        }

        public void ReportError(ErrorTypes type, string message, int row, int col)
        {
            Errors.Add(new ErrorMessage(type, message, row, col));
        }

        public void ReportError(ErrorTypes type, string message,
            SyntaxElement node)
        {
            Errors.Add(new ErrorMessage(type, message, node));
        }

        public void ReportError(ErrorTypes type, string message,
            SyntaxElement node, SyntaxElement referredNode)
        {
            Errors.Add(new ErrorMessage(type, message, node, referredNode));
        }

        public bool HasErrorReportForNode(ErrorTypes type, SyntaxElement node)
        {
            return Errors.FindIndex((errMsg) => errMsg.ErrorType == type && errMsg.ProblemNode == node) > -1;
        }

        public bool HasErrorReportForReferenceTo(ErrorTypes type, Declaration node)
        {
            return Errors.FindIndex((errMsg) => errMsg.ErrorType == type && errMsg.ReferencedNode == node) > -1;
        }

        public int Count
        {
            get { return Errors.Count; }
        }
    }

    public class ErrorMessage
    {
        public string Content
        {
            get;
            private set;
        }

        // The node where the problem was detected.
        public SyntaxElement ProblemNode
        {
            get;
            private set;
        }

        // Used with reference errors (Method, Lvalue, UninitializedLocal).
        public SyntaxElement ReferencedNode
        {
            get;
            set;
        }

        public ErrorTypes ErrorType
        {
            get;
            private set;
        }

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

        private ErrorMessage(ErrorTypes type, string message, SyntaxElement node,
            SyntaxElement referencedNode, int row, int col)
        {
            ErrorType = type;
            Content = message;
            ProblemNode = node;
            ReferencedNode = referencedNode;
            Row = row;
            Col = col;
        }

        public ErrorMessage(ErrorTypes type, string message, SyntaxElement node)
            : this(type, message, node, null, node.Row, node.Col) { }

        public ErrorMessage(ErrorTypes type, string message, SyntaxElement node,
            SyntaxElement referencedNode)
            : this(type, message, node, referencedNode, node.Row, node.Col) { }

        public ErrorMessage(ErrorTypes type, string message, int row, int col)
            : this(type, message, null, null, row, col)
        {
            if (type != ErrorTypes.Lexical && type != ErrorTypes.Syntax)
            {
                throw new ArgumentException("Invalid constructor for this type of message." +
                    " Are you using the correct overload?");
            }
        }

        public override string ToString()
        {
            return String.Format("Error: ({0},{1}) {2}", Row, Col, Content);
        }

        public static int CompareByLocation(ErrorMessage x, ErrorMessage y)
        {
            if (x.Row.CompareTo(y.Row) != 0)
            {
                return x.Row.CompareTo(y.Row);
            }
            return x.Col.CompareTo(y.Col);
        }
    }
}