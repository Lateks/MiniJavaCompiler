using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.Support
{
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
        public string Message
        {
            get { return String.Format("{0} (near row {1}, column {2})", Content, Row, Col); }
        }

        public ErrorMessage(string message, int row, int col)
        {
            Content = message;
            Row = row;
            Col = col;
        }
    }
}