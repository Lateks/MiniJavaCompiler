﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.Errors.Compilation;
using System.IO;

namespace MiniJavaCompiler
{
    namespace LexicalAnalysis
    {
        class ScannerInputReader
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
                    char temp = (char) buffer;
                    return temp;
                }
                else
                    return (char) input.Peek();
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
                    bool no_comments_skipped = !SkipComments();
                    if ((no_comments_skipped && !NextCharIsWhiteSpace()))
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
                return (InputLeft() && Char.IsWhiteSpace((char) input.Peek()));
            }

            // Returns true if something was skipped and false otherwise.
            private bool SkipComments()
            {
                if (!InputLeft() || !Peek().Equals('/'))
                    return false;

                commentStartRow = Row;
                commentStartCol = Col + 1;
                buffer = (char) input.Read();
                if (InputLeft() && input.Peek().Equals('/'))
                    SkipOneLineComment();
                else if (InputLeft() && input.Peek().Equals('*'))
                    SkipMultilineComment();
                else
                    return false;
                return true;
            }

            private void SkipOneLineComment()
            {
                ReadUntil('\n'); // may also end in EOF
            }

            private void SkipMultilineComment()
            {
                Read(); Read();
                while (true)
                {
                    if (!ReadUntil('*'))
                        throw new EndlessCommentError("Reached end of input while scanning for a comment " +
                        "beginning on line " + commentStartRow + ", column " + commentStartCol + ".",
                        commentStartRow, commentStartCol);
                    if (input.Peek().Equals('/'))
                    {
                        Read();
                        return;
                    }
                }
            }

            private bool ReadUntil(char symbol)
            {
                while (InputLeft() && !input.Peek().Equals(symbol))
                    Read();
                if (!InputLeft()) // reached end of input but did not see symbol
                    return false;
                Read();
                return true;
            }
        }
    }
}
