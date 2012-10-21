using System;
using System.IO;

namespace MiniJavaCompiler.Frontend.LexicalAnalysis
{
    internal class EndlessCommentError : Exception
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        public EndlessCommentError(string message, int row, int col)
            : base(message)
        {
            Row = row;
            Col = col;
        }
    }

    // Handles reading input from the given TextReader, skipping comments
    // and whitespace, keeping track of row and column numbers in the
    // source code and buffering input when a look-ahead of more than
    // one character is needed (that is, when trying to tell division
    // symbols from comment starter symbols).
    internal class ScannerInputReader
    {
        private readonly TextReader _input;
        private char? _buffer;
        private int _commentStartRow;
        private int _commentStartCol;
        internal int Row { get; private set; }
        internal int Col { get; private set; }

        internal ScannerInputReader(TextReader input)
        {
            _input = input;
            _buffer = null;
            Row = 1; Col = 0;
        }

        internal char Peek()
        {
            if (_buffer != null)
            {
                var temp = (char)_buffer;
                return temp;
            }
            else
                return (char)_input.Peek();
        }

        internal bool InputLeft()
        {
            return !(_input.Peek() < 0 && _buffer == null);
        }

        internal string Read()
        {
            char symbol;
            if (_buffer != null)
            {
                symbol = (char)_buffer;
                _buffer = null;
            }
            else
                symbol = (char)_input.Read();

            AdvanceScannerPosition(symbol);

            return symbol.ToString();
        }

        internal void SkipWhiteSpaceAndComments()
        {
            while (InputLeft())
            {
                SkipWhiteSpace();
                bool noCommentsFound = !SkipComments();
                if ((noCommentsFound && !NextCharIsWhiteSpace()))
                    return;
            }
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

            _commentStartRow = Row;
            _commentStartCol = Col + 1;
            _buffer = (char)_input.Read(); // may be a division symbol, not a comment starter
            if (_input.Peek().Equals('/'))
                SkipOneLineComment();
            else if (_input.Peek().Equals('*'))
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
            Read(); Read(); // discard the starting characters of the comment
            do
            {
                if (!ReadUntil('*'))
                    throw new EndlessCommentError("Reached end of input while scanning for a comment.",
                                                  _commentStartRow, _commentStartCol);
            } while (!Peek().Equals('/'));
            Read();
        }

        private bool ReadUntil(char symbol)
        {
            while (InputLeft() && !Peek().Equals(symbol))
                Read();
            if (!InputLeft()) // reached end of input but did not see symbol
                return false;
            Read(); // discard symbol
            return true;
        }
    }
}
