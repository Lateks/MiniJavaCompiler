using MiniJavaCompiler.Support.AbstractSyntaxTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.Support
{
    // This error can be used in all stages of compilation
    // to indicate failure.
    public class CompilationError : Exception { }

    public interface IErrorReporter
    {
        void ReportError(string message, int row, int col);
        void ReportError(string message, SyntaxElement node);
        List<ErrorMessage> Errors { get; }
        int Count { get; }
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

        public void ReportError(string message, int row, int col)
        {
            Errors.Add(new ErrorMessage(message, row, col));
        }

        public void ReportError(string message, SyntaxElement node)
        {
            Errors.Add(new ErrorMessage(message, node));
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
        public SyntaxElement ProblemNode
        {
            get;
            private set;
        }

        private int _row;
        private int _col;
        public int Row
        {
            get
            {
                return _row;
            }
        }
        public int Col
        {
            get
            {
                return _col;
            }
        }

        public ErrorMessage(string message, SyntaxElement node)
        {
            Content = message;
            ProblemNode = node;
            _row = node.Row;
            _col = node.Col;
        }

        public ErrorMessage(string message, int row, int col)
        {
            Content = message;
            ProblemNode = null;
            _row = row;
            _col = col;
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