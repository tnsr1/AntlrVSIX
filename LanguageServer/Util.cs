﻿namespace LanguageServer
{
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;
    using Workspaces;

    public class Util
    {

        public static IParseTree Find(int index, Document document)
        {
            var pd = ParserDetailsFactory.Create(document);
            foreach (var node in DFSVisitor.DFS(pd.ParseTree as ParserRuleContext))
            {
                if (node as TerminalNodeImpl == null)
                    continue;
                var leaf = node as TerminalNodeImpl;
                if (leaf.Symbol.StartIndex <= index && index <= leaf.Symbol.StopIndex)
                    return leaf;
            }
            return null;
        }
    }
}