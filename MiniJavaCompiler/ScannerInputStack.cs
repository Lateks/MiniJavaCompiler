using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.Support.Errors.Compilation;

namespace MiniJavaCompiler
{
    namespace LexicalAnalysis
    {
        class ScannerInputStack
        {
            private Stack<char> input;
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

            internal ScannerInputStack(string input)
            {
                var inputchars = input.ToArray();
                Array.Reverse(inputchars);
                this.input = new Stack<char>(inputchars);
                Row = 1; Col = 0;
            }

            internal char Peek()
            {
                return input.Peek();
            }

            internal bool InputLeft()
            {
                return input.Count > 0;
            }

            internal string Pop()
            {
                char symbol = input.Pop();
                if (symbol.Equals('\n'))
                {
                    Row++;
                    Col = 0;
                }
                else
                    Col++;
                return symbol.ToString();
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
                    Pop();
            }

            private bool NextCharIsWhiteSpace()
            {
                return (InputLeft() && Char.IsWhiteSpace(input.Peek()));
            }

            // Returns true if something was skipped and false otherwise.
            private bool SkipComments()
            {
                try
                {
                    if (!InputLeft() || !input.Peek().Equals('/'))
                        return false;

                    char symbol = input.Pop();
                    if (InputLeft() && input.Peek().Equals('/'))
                        SkipOneLineComment();
                    else if (InputLeft() && input.Peek().Equals('*'))
                        SkipMultilineComment();
                    else
                    { // The character was a division symbol => return it on top of the stack.
                        input.Push(symbol);
                        return false;
                    }
                    return true;
                }
                catch (InvalidOperationException)
                {
                    throw new LexicalError("Reached end of input while scanning for a comment.");
                }
            }

            private void SkipOneLineComment()
            {
                ReadUntil('\n');
            }

            private void SkipMultilineComment()
            {
                Pop();
                while (true)
                {
                    ReadUntil('*');
                    if (input.Peek().Equals('/'))
                    {
                        Pop();
                        return;
                    }
                }
            }

            private void ReadUntil(char symbol)
            {
                while (!input.Peek().Equals(symbol))
                    Pop();
                Pop();
            }
        }
    }
}
