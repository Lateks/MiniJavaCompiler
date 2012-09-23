using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace MiniJavaCompiler.LexicalAnalysis
{
    public interface IScanner
    {
        IToken NextToken();
    }

    public class MiniJavaScanner : IScanner
    {
        public static HashSet<char>
            symbols = new HashSet<char>(new char[] { ';', '(', ')', '[', ']', '.', '{', '}', ',' }),
            singleCharOperators = new HashSet<char>(new char[] { '/', '+', '-', '*', '<', '>', '!', '%' }),
            multiCharOperatorSymbols = new HashSet<char>(new char[] { '&', '=', '|' });
        public static HashSet<string>
            keywords = new HashSet<string>(new string[] { "this", "true", "false", "new",
                                                        "length", "System", "out", "println",
                                                        "if", "else", "while", "return", "assert",
                                                        "public", "static", "main", "class", "extends" }),
            types = new HashSet<string>(new string[] { "int", "boolean", "void" });

        private ScannerInputReader input;
        private Queue<IToken> tokens;
        private int startRow;
        private int startCol;

        public MiniJavaScanner(TextReader input)
        {
            this.input = new ScannerInputReader(input);
            tokens = new Queue<IToken>();
            BuildTokenList();
        }

        // If called after EndOfFile, will return null.
        public IToken NextToken()
        {
            if (tokens.Count > 0)
                return tokens.Dequeue();
            return new EndOfFile(input.Row, input.Col);
        }

        // Passes through the code once and builds a queue of tokens.
        private void BuildTokenList()
        {
            IToken token;
            do
            {
                token = MatchNextToken();
                tokens.Enqueue(token);
            } while (!(token is EndOfFile));
        }

        private IToken MatchNextToken()
        {
            try
            {
                input.SkipWhiteSpaceAndComments();
            }
            catch (EndlessCommentError e)
            {
                return new ErrorToken("", e.Message, e.Row, e.Col);
            }
            if (!input.InputLeft())
                return new EndOfFile(input.Row, input.Col);

            startRow = input.Row;     // store starting row and column for the token object 
            startCol = input.Col + 1;

            char inputChar = input.Peek();
            if (singleCharOperators.Contains(inputChar))
                return MakeSingleCharOperatorToken();
            else if (multiCharOperatorSymbols.Contains(inputChar))
                return MakeAssignmentOrMultiCharOperatorToken();
            else if (symbols.Contains(inputChar))
                return MakeSymbolToken();
            else if (Char.IsDigit(inputChar))
                return MakeIntegerLiteralToken();
            else if (Char.IsLetter(inputChar))
                return MakeIdentifierOrKeywordToken();
            else
            {
                string token = input.Read();
                return new ErrorToken(token, "Invalid token \"" + token + ".",
                    startRow, startCol);
            }
        }

        private IToken MakeSingleCharOperatorToken()
        {
            if (input.Peek().Equals('!'))
            {
                input.Read();
                return new UnaryNotToken(startRow, startCol);
            }
            else if (input.Peek().Equals('<') || input.Peek().Equals('>'))
                return new LogicalOperatorToken(input.Read(), startRow, startCol);
            return new ArithmeticOperatorToken(input.Read(), startRow, startCol);
        }

        private IToken MakeAssignmentOrMultiCharOperatorToken()
        {
            string symbol = input.Read();
            if (input.InputLeft() && input.Peek().ToString().Equals(symbol))
            {
                symbol += input.Read();
                return new LogicalOperatorToken(symbol, startRow, startCol);
            }
            else if (symbol.Equals("="))
                return new AssignmentToken(startRow, startCol);
            else
                return new ErrorToken(symbol, "Unexpected token " + symbol + ".",
                    startRow, startCol);
        }

        private IToken MakeSymbolToken()
        {
            string token = input.Read();
            switch (token)
            {
                case "(":
                    return new LeftParenthesis(startRow, startCol);
                case ")":
                    return new RightParenthesis(startRow, startCol);
                case "[":
                    return new LeftBracket(startRow, startCol);
                case "]":
                    return new RightBracket(startRow, startCol);
                case "{":
                    return new LeftCurlyBrace(startRow, startCol);
                case "}":
                    return new RightCurlyBrace(startRow, startCol);
                case ".":
                    return new MethodInvocationToken(startRow, startCol);
                case ",":
                    return new ParameterSeparator(startRow, startCol);
                default:
                    return new EndLine(startRow, startCol);
            }
        }

        private IntegerLiteralToken MakeIntegerLiteralToken()
        {
            string token = "";
            while (input.InputLeft() && Char.IsDigit(input.Peek()))
                token += input.Read();
            return new IntegerLiteralToken(token, startRow, startCol);
        }

        private IToken MakeIdentifierOrKeywordToken()
        {
            string token = "";
            while (input.InputLeft() && (Char.IsLetterOrDigit(input.Peek()) ||
                                         input.Peek().Equals('_')))
                token += input.Read();
            if (types.Contains(token))
                return new MiniJavaType(token, startRow, startCol);
            if (keywords.Contains(token))
                return new KeywordToken(token, startRow, startCol);
            return new Identifier(token, startRow, startCol);
        }
    }
}