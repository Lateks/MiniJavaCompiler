using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJavaCompiler.AbstractSyntaxTree;
using MiniJavaCompiler.LexicalAnalysis;
using MiniJavaCompiler.Support;

namespace MiniJavaCompiler.SyntaxAnalysis
{
    public interface IListParser
    {
        List<TNodeType> ParseList<TNodeType, TFollowToken>(Func<TNodeType> parseNode)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken;
    }

    internal class ListParser : Parser, IListParser
    {
        public ListParser(IParserInputReader input, IErrorReporter errorReporter)
            : base(input, errorReporter) { }

        public List<TNodeType> ParseList<TNodeType, TFollowToken>(Func<TNodeType> parseNode)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken
        {
            var nodeList = new List<TNodeType>();
            if (!(Input.NextTokenIs<TFollowToken>()))
            {
                nodeList.Add(parseNode());
                nodeList.AddRange(ParseList<TNodeType, TFollowToken>(parseNode));
            }
            return nodeList;
        }
    }

    internal class CommaSeparatedListParser : Parser, IListParser
    {
        public CommaSeparatedListParser(IParserInputReader input, IErrorReporter errorReporter)
            : base(input, errorReporter) { }

        public List<TNodeType> ParseList<TNodeType, TFollowToken>(Func<TNodeType> parseNode)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken
        {
            return ParseList<TNodeType, TFollowToken>(parseNode, false);
        }

        private List<TNodeType> ParseList<TNodeType, TFollowToken>
            (Func<TNodeType> parseNode, bool isListTail)
            where TNodeType : ISyntaxTreeNode
            where TFollowToken : IToken
        {
            var list = new List<TNodeType>();
            if (!(Input.NextTokenIs<TFollowToken>()))
            {
                if (isListTail) Input.MatchAndConsume<ParameterSeparator>();
                list.Add(parseNode());
                list.AddRange(ParseList<TNodeType, TFollowToken>(
                    parseNode, true));
            }
            return list;
        }
    }
}
