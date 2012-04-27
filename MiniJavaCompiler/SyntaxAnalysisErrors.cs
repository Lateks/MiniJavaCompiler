﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler
{
    namespace SyntaxAnalysis
    {
        public class SyntaxError : Exception
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

            public SyntaxError(string message, int row, int col)
                : base(message) { }
        }

        public class LexicalErrorEncountered : Exception
        {
            public LexicalErrorEncountered() { }
        }

        public class BackEndError : Exception
        {
            public List<ErrorMessage> ErrorMsgs
            {
                get;
                private set;
            }

            public BackEndError(List<ErrorMessage> messages)
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
}