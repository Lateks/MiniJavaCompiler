using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.Support
{
    public interface IErrorReporter
    {
        void ReportError(string message, int row, int col);
        List<ErrorMessage> Errors();
    }

    public class ErrorLogger : IErrorReporter
    {
        private readonly List<ErrorMessage> errorMessages;

        public ErrorLogger()
        {
            errorMessages = new List<ErrorMessage>();
        }

        public void ReportError(string message, int row, int col)
        {
            errorMessages.Add(new ErrorMessage(message, row, col));
        }

        public List<ErrorMessage> Errors()
        {
            return errorMessages;
        }
    }

    public class ErrorReport : Exception
    {
        public List<ErrorMessage> ErrorMsgs
        {
            get;
            private set;
        }

        public ErrorReport(List<ErrorMessage> messages)
        {
            ErrorMsgs = messages;
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
    }
}