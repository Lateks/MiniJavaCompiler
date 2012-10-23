using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.Support
{
    // This error can be used in all stages of compilation
    // to indicate failure.
    public class CompilationFailed : Exception { }

    public interface IErrorReporter
    {
        void ReportError(string message, int row, int col);
        List<ErrorMessage> Errors { get; }
        int Count { get; }
    }

    public class ErrorLogger : IErrorReporter
    {
        public List<ErrorMessage> Errors { get; private set; }

        public ErrorLogger()
        {
            Errors = new List<ErrorMessage>();
        }

        public void ReportError(string message, int row, int col)
        {
            Errors.Add(new ErrorMessage(message, row, col));
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

        public ErrorMessage(string message, int row, int col)
        {
            Content = message;
            Row = row;
            Col = col;
        }

        public override string ToString()
        {
            return String.Format("Row {0}, column {1}: {2}", Row, Col, Content);
        }
    }
}