using System;
using System.Collections.Generic;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;

namespace MiniJavaCompiler.Frontend.SyntaxAnalysis
{
    public interface IValueListParser
    {
        // Parses a list of syntax tree nodes where each individual node can be parsed with the
        // function given as a parameter. Parsing ends at the end of file or a token that matches
        // the type and value of follow token given as a parameter.
        List<TNodeType> ParseList<TNodeType, TFollowToken>(Func<TNodeType> parseNode, string followTokenValue)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken;
    }

    public interface IListParser
    {
        // Parses a list of syntax tree nodes where each individual node can be parsed with the
        // function given as a parameter. Parsing ends at the end of file.
        List<TNodeType> ParseList<TNodeType>(Func<TNodeType> parseNode)
            where TNodeType : ISyntaxTreeNode;
    }

    // Parses a list with no separators.
    public class ListParser : ParserBase, IListParser, IValueListParser
    {
        public ListParser(IParserInputReader input, IErrorReporter errorReporter)
            : base(input, errorReporter) { }

        public List<TNodeType> ParseList<TNodeType>(Func<TNodeType> parseNode)
            where TNodeType : ISyntaxTreeNode
        {
            return Parse(parseNode, Input.NextTokenIs<EndOfFile>);
        }

        public List<TNodeType> ParseList<TNodeType, TFollowToken>(Func<TNodeType> parseNode, string followTokenValue)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken
        {
            return Parse(parseNode, () => Input.NextTokenIs<EndOfFile>() || Input.NextTokenIs<TFollowToken>(followTokenValue));
        }

        private List<TNodeType> Parse<TNodeType>(Func<TNodeType> parseNode, Func<bool> nextTokenIsFollowToken)
            where TNodeType : ISyntaxTreeNode
        {
            var nodeList = new List<TNodeType>();
            if (!nextTokenIsFollowToken())
            {
                nodeList.Add(parseNode());
                nodeList.AddRange(Parse(parseNode, nextTokenIsFollowToken));
            }
            return nodeList;
        }
    }

    // Parses comma separated lists such as formal parameter lists and
    // lists of arguments to method invocations.
    public class CommaSeparatedListParser : ParserBase, IValueListParser
    {
        public CommaSeparatedListParser(IParserInputReader input, IErrorReporter errorReporter)
            : base(input, errorReporter) { }

        public List<TNodeType> ParseList<TNodeType, TFollowToken>(Func<TNodeType> parseNode, string followTokenValue)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken
        {
            return ParseList<TNodeType, TFollowToken>(parseNode, followTokenValue, false);
        }

        private List<TNodeType> ParseList<TNodeType, TFollowToken>
            (Func<TNodeType> parseNode, string followTokenValue, bool isListTail)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken
        {
            var list = new List<TNodeType>();
            if (!(Input.NextTokenIs<EndOfFile>() || Input.NextTokenIs<TFollowToken>(followTokenValue)))
            {
                if (isListTail) Input.MatchAndConsume<PunctuationToken>(",");
                list.Add(parseNode());
                list.AddRange(ParseList<TNodeType, TFollowToken>(
                    parseNode, followTokenValue, true));
            }
            return list;
        }
    }
}
