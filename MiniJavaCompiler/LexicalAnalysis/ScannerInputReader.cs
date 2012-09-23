using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MiniJavaCompiler.LexicalAnalysis
{
    public class EndlessCommentError : Exception
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

        public EndlessCommentError(string message, int row, int col)
            : base(message) { }
    }

    // Handles reading input from the given TextReader, skipping comments
    // and whitespace, keeping track of row and column numbers in the
    // source code and buffering input when a look-ahead of more than
    // one character is needed (that is, when trying to tell division
    // symbols from comment starter symbols).
    internal class ScannerInputReader
    {
        private TextReader input;
        private char? buffer;
        private int commentStartRow;
        private int commentStartCol;
        internal int Row
        {
            get;
            private set;
        }
        internal int Col
        {
            get;
            private set;
        }

        internal ScannerInputReader(TextReader input)
        {
            this.input = input;
            buffer = null;
            Row = 1; Col = 0;
        }

        internal char Peek()
        {
            if (buffer != null)
            {
                char temp = (char)buffer;
                return temp;
            }
            else
                return (char)input.Peek();
        }

        internal bool InputLeft()
        {
            return !(input.Peek() < 0 && buffer == null);
        }

        internal string Read()
        {
            char symbol;
            if (buffer != null)
            {
                symbol = (char)buffer;
                buffer = null;
            }
            else
                symbol = (char)input.Read();

            AdvanceScannerPosition(symbol);

            return symbol.ToString();
        }

        private void AdvanceScannerPosition(char currentSymbol)
        {
            if (currentSymbol.Equals('\n'))
            {
                Row++;
                Col = 0;
            }
            else
                Col++;
        }

        internal void SkipWhiteSpaceAndComments()
        {
            while (InputLeft())
            {
                SkipWhiteSpace();
                bool no_comments_found = !SkipComments();
                if ((no_comments_found && !NextCharIsWhiteSpace()))
                    return;
            }
        }

        private void SkipWhiteSpace()
        {
            while (NextCharIsWhiteSpace())
                Read();
        }

        private bool NextCharIsWhiteSpace()
        {
            return (InputLeft() && Char.IsWhiteSpace(Peek()));
        }

        // Returns true if something was skipped and false otherwise.
        private bool SkipComments()
        {
            if (!InputLeft() || !Peek().Equals('/'))
                return false;

            commentStartRow = Row;
            commentStartCol = Col + 1;
            buffer = (char)input.Read(); // may be a division symbol, not a comment starter
            if (input.Peek().Equals('/'))
                SkipOneLineComment();
            else if (input.Peek().Equals('*'))
                SkipMultilineComment();
            else
                return false;
            return true;
        }

        private void SkipOneLineComment()
        {
            ReadUntil('\n'); // may also end in EndOfFile
        }

        private void SkipMultilineComment()
        {
            Read(); Read();
            while (true)
            {
                if (!ReadUntil('*'))
                    throw new EndlessCommentError("Reached end of input while scanning for a comment.",
                    commentStartRow, commentStartCol);
                if (Peek().Equals('/'))
                {
                    Read();
                    return;
                }
            }
        }

        private bool ReadUntil(char symbol)
        {
            while (InputLeft() && !Peek().Equals(symbol))
                Read();
            if (!InputLeft()) // reached end of input but did not see symbol
                return false;
            Read();
            return true;
        }
    }
}
