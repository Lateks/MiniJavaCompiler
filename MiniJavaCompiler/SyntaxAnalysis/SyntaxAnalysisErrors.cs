using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.SyntaxAnalysis
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
}