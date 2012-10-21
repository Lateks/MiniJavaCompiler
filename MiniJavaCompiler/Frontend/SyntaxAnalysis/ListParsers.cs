using System;
using System.Collections.Generic;
using MiniJavaCompiler.Frontend.LexicalAnalysis;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;

namespace MiniJavaCompiler.Frontend.SyntaxAnalysis
{
    public interface IListEndingInStringTokenParser
    {
        List<TNodeType> ParseList<TNodeType, TFollowToken>(Func<TNodeType> parseNode, string followTokenValue)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken;
    }

    public interface IListEndingInEndOfFileParser
    {
        List<TNodeType> ParseList<TNodeType>(Func<TNodeType> parseNode)
            where TNodeType : ISyntaxTreeNode;
    }

    internal class ListParser : ParserBase, IListEndingInEndOfFileParser, IListEndingInStringTokenParser
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
            return Parse(parseNode, () => Input.NextTokenIs<TFollowToken>(followTokenValue));
        }

        private static List<TNodeType> Parse<TNodeType>(Func<TNodeType> parseNode, Func<bool> nextTokenIsFollowToken)
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

    internal class CommaSeparatedListParser : ParserBase, IListEndingInStringTokenParser
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
            if (!(Input.NextTokenIs<TFollowToken>(followTokenValue)))
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
