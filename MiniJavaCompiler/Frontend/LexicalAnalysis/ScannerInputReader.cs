using System;
using System.Diagnostics;
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

    internal class OutOfScannerInput : Exception { }

    // Handles reading input from the given TextReader, skipping comments
    // and whitespace, keeping track of row and column numbers in the
    // source code and buffering input when a look-ahead of more than
    // one character is needed (that is, when trying to tell division
    // symbols from comment starter symbols).
    internal class ScannerInputReader
    {
        private readonly TextReader _input;
        private char? _buffer;
        private int _commentStartRow; // the starting row of the current comment, for error messages
        private int _commentStartCol; // the starting column of the current comment, for error messages
        internal int Row { get; private set; }
        internal int Col { get; private set; }

        internal ScannerInputReader(TextReader input)
        {
            _input = input;
            _buffer = null;
            Row = 1; Col = 0; // The first character in a file will be marked as being on row 1, column 1.
        }

        // Peeks at input.
        internal char Peek()
        {
            if (_buffer != null)
            {
                return (char)_buffer;
            }
            else
            {
                if (!InputLeft())
                {
                    throw new OutOfScannerInput();
                }
                return (char) _input.Peek();
            }
        }

        internal bool InputLeft()
        {
            return _input.Peek() >= 0 || _buffer != null;
        }

        // Reads the next character from input as a string.
        internal string Read()
        {
            if (!InputLeft())
            {
                throw new OutOfScannerInput();
            }

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
            bool commentsFound = true;
            while (NextCharIsWhiteSpace() || commentsFound)
            {
                SkipWhiteSpace();
                commentsFound = SkipComments();
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
            return InputLeft() && Char.IsWhiteSpace(Peek());
        }

        // Returns true if something was skipped and false otherwise.
        private bool SkipComments()
        {
            if (!InputLeft() || !Peek().Equals('/'))
                return false;
            Debug.Assert(_buffer == null);

            _commentStartRow = Row;
            _commentStartCol = Col + 1;
            _buffer = (char)_input.Read(); // May be a division symbol, not a comment starter.
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
            Read(); // discard the comment ending '/' symbol
        }

        private bool ReadUntil(char symbol)
        {
            while (InputLeft() && !Peek().Equals(symbol))
                Read();
            if (!InputLeft()) // Reached end of input but did not see the symbol.
                return false;
            Read(); // Discard symbol.
            return true;
        }
    }
}
