using System;
using System.Diagnostics;
using System.IO;

namespace MiniJavaCompiler.FrontEnd.LexicalAnalysis
{
    // This exception is thrown if a multiline comment is ended by end of file.
    public class EndlessCommentError : Exception
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

    public class OutOfScannerInput : Exception { }

    // Handles reading input from the given TextReader, skipping comments
    // and whitespace, keeping track of row and column numbers in the
    // source code and buffering input when a look-ahead of more than
    // one character is needed (that is, when trying to tell division
    // symbols from comment starter symbols).
    public class ScannerInputReader
    {
        private readonly TextReader _input;
        private char? _buffer;
        private int _commentStartRow; // the starting row of the current comment, for error messages
        private int _commentStartCol; // the starting column of the current comment, for error messages
        public int Row { get; private set; }
        public int Col { get; private set; }

        public ScannerInputReader(TextReader input)
        {
            _input = input;
            _buffer = null;
            Row = 1; Col = 0; // The first character in a file will be marked as being on row 1, column 1.
        }

        // Returns the next character in input without consuming it.
        public char Peek()
        {
            if (!InputLeft())
            {
                throw new OutOfScannerInput();
            }

            if (_buffer != null)
            {
                return (char)_buffer;
            }
            else
            {
                return (char)_input.Peek();
            }
        }

        public bool InputLeft()
        {
            return _input.Peek() >= 0 || _buffer != null;
        }

        // Reads the next character from input as a string.
        public string Read()
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

        public void SkipWhiteSpaceAndComments()
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
            if (currentSymbol == '\n')
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
            if (!InputLeft() || !(Peek() == '/'))
                return false;
            Debug.Assert(_buffer == null);

            _commentStartRow = Row;
            _commentStartCol = Col + 1;
            _buffer = (char)_input.Read(); // May be a division symbol, not a comment starter.
            if (_input.Peek() == '/')
            {
                SkipOneLineComment();
            }
            else if (_input.Peek() == '*')
            {
                Read(); Read(); // discard the starting characters of the comment (/*)
                SkipMultilineComment();
            }
            else
                return false;
            return true;
        }

        private void SkipOneLineComment()
        {
            ReadUntil('\n'); // may also end in EndOfFile
        }

        // Skip nested comments recursively.
        private void SkipMultilineComment()
        {
            do
            {
                int lastReadChar;
                do
                {
                    lastReadChar = ReadUntil('*', '/');
                    if (lastReadChar == '/')
                    {
                        if (InputLeft() && Peek() == '*')
                        {
                            Read(); // discard '*'
                            SkipMultilineComment(); // skip a nested comment
                        }
                    }
                } while (InputLeft() && lastReadChar != '*');
            } while (InputLeft() && !(Peek() == '/'));

            if (!InputLeft())
                throw new EndlessCommentError("Reached end of input while scanning for a comment.",
                                               _commentStartRow, _commentStartCol);
            Read(); // discard the comment ending '/' symbol
        }

        // Reads until one of the specified symbols or end of file,
        // whichever comes first. The symbol itself is also consumed.
        //
        // Returns the value of the character or -1 if end of file was reached.
        private int ReadUntil(params char[] symbols)
        {
            while (InputLeft() && Array.Find(symbols, (val) => val == _input.Peek()) == '\0')
                Read();
            if (InputLeft())
                 return (int) Read()[0];
            return -1;
        }
    }
}
