﻿
namespace AntlrVSIX.GrammarDescription.Python
{
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;
    using AntlrVSIX.GrammarDescription;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Media;

    class PythonGrammarDescription : IGrammarDescription
    {
        public IParseTree Parse(string ffn, string code)
        {
            IParseTree _ant_tree = null;

            // Set up Antlr to parse input grammar.
            byte[] byteArray = Encoding.UTF8.GetBytes(code);
            CommonTokenStream cts = new CommonTokenStream(
                new Python3Lexer(
                    new AntlrInputStream(
                        new StreamReader(
                            new MemoryStream(byteArray)).ReadToEnd())));
            var parser = new Python3Parser(cts);

            try
            {
                _ant_tree = parser.file_input();
            }
            catch (Exception e)
            {
                // Parsing error.
            }

            StringBuilder sb = new StringBuilder();
            Foobar.ParenthesizedAST(_ant_tree, sb, "", cts);
            string fn = System.IO.Path.GetFileName(ffn);
            fn = "c:\\temp\\" + fn;
            System.IO.File.WriteAllText(fn, sb.ToString());

            return _ant_tree;
        }

        public const string FileExtension = ".py";

        public bool IsFileType(string ffn)
        {
            if (ffn == null) return false;
            var allowable_suffices = FileExtension.Split(';').ToList<string>();
            var suffix = Path.GetExtension(ffn).ToLower();
            foreach (var s in allowable_suffices)
                if (suffix == s)
                    return true;
            return false;
        }

        /* Tagging and classification types. */
        private const string ClassificationNameVariable = "variable";
        private const string ClassificationNameMethod = "method";
        private const string ClassificationNameComment = "comment";
        private const string ClassificationNameKeyword = "keyword";
        private const string ClassificationNameLiteral = "literal";

        public string[] Map { get; } = new string[]
        {
            ClassificationNameVariable,
            ClassificationNameMethod,
            ClassificationNameComment,
            ClassificationNameKeyword,
            ClassificationNameLiteral,
        };

        public Dictionary<string, int> InverseMap { get; } = new Dictionary<string, int>()
        {
            { ClassificationNameVariable, 0 },
            { ClassificationNameMethod, 1 },
            { ClassificationNameComment, 2 },
            { ClassificationNameKeyword, 3 },
            { ClassificationNameLiteral, 4 },
        };

        /* Color scheme for the tagging. */
        public List<System.Windows.Media.Color> MapColor { get; } = new List<System.Windows.Media.Color>()
        {
            Colors.Purple,
            Colors.Orange,
            Colors.Green,
            Colors.Blue,
            Colors.Red,
        };

        public List<System.Windows.Media.Color> MapInvertedColor { get; } = new List<System.Windows.Media.Color>()
        {
            Colors.LightPink,
            Colors.LightYellow,
            Colors.LightGreen,
            Colors.LightBlue,
            Colors.Red,
        };

        public List<bool> CanFindAllRefs { get; } = new List<bool>()
        {
            true, // variable
            true, // method
            false, // comment
            false, // keyword
            true, // literal
        };

        public List<bool> CanRename { get; } = new List<bool>()
        {
            true, // variable
            true, // method
            false, // comment
            false, // keyword
            false, // literal
        };

        public List<bool> CanGotodef { get; } = new List<bool>()
        {
            true, // variable
            true, // method
            false, // comment
            false, // keyword
            false, // literal
        };

        public List<bool> CanGotovisitor { get; } = new List<bool>()
        {
            false, // variable
            false, // method
            false, // comment
            false, // keyword
            false, // literal
        };

        private static List<string> _keywords = new List<string>()
        {
            "def",
            "return",
            "raise",
            "from",
            "import",
            "as",
            "global",
            "nonlocal",
            "assert",
            "if",
            "elif",
            "else",
            "while",
            "for",
            "in",
            "try",
            "finally",
            "with",
            "except",
            "lambda",
            "or",
            "and",
            "not",
            "is",
            "None",
            "True",
            "False",
            "class",
            "yield",
            "del",
            "pass",
            "continue",
            "break",
            "async",
            "await",
        };

        public List<Func<IGrammarDescription, IParseTree, bool>> Identify { get; } = new List<Func<IGrammarDescription, IParseTree, bool>>()
        {
            (IGrammarDescription gd, IParseTree t) => // variable = 0
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    var text = term.GetText();
                    // Make sure it's not a def.
                    var fun = gd.IdentifyDefinition[0];
                    var is_def = fun != null ? fun(gd, term) : false;
                    if (is_def) return false;
                    if (_keywords.Contains(text)) return false;
                    if (term?.Symbol.Type == Python3Parser.NAME) return true;
                    return false;
                },
            (IGrammarDescription gd, IParseTree t) => // method = 1
                {
                    return false;
                },
            null, // comment = 2
            (IGrammarDescription gd, IParseTree t) => // keyword = 3
                {
                    TerminalNodeImpl nonterm = t as TerminalNodeImpl;
                    if (nonterm == null) return false;
                    var text = nonterm.GetText();
                    if (!_keywords.Contains(text)) return false;
                    return true;
                },
            (IGrammarDescription gd, IParseTree t) => // literal = 4
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    // Chicken/egg problem. Assume that literals are marked
                    // with the appropriate token type.
                    if (term.Symbol == null) return false;
                    return false;
                },
        };

        public List<Func<IGrammarDescription, IParseTree, bool>> IdentifyDefinition { get; } = new List<Func<IGrammarDescription, IParseTree, bool>>()
        {
            (IGrammarDescription gd, IParseTree t) => // variable
                {
                    return false;
                },
            (IGrammarDescription gd, IParseTree t) => // method
                {
                    return false;
                },
            null, // comment
            null, // keyword
            null, // literal
        };

        public bool CanNextRule { get { return false; } }

        public bool DoErrorSquiggles { get { return false; } }
    }
}