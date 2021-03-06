﻿namespace LanguageServer
{
    using Antlr4.Runtime.Tree;
    using Graphs;
    using Symtab;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Workspaces;


    public class Module
    {

        public static int GetIndex(int line, int column, Document doc)
        {
            int index = 0;
            string buffer = doc.Code;
            if (buffer == null)
            {
                return 0;
            }

            int cur_line = 0;
            int cur_col = 0;
            for (; ; )
            {
                if (cur_line > line)
                {
                    break;
                }

                if (cur_line >= line && cur_col >= column)
                {
                    break;
                }

                char ch = buffer[index];
                if (ch == '\r')
                {
                    if (index + 1 >= buffer.Length)
                    {
                        break;
                    }
                    else if (buffer[index + 1] == '\n')
                    {
                        cur_line++;
                        cur_col = 0;
                        index += 2;
                    }
                    else
                    {
                        // Error in code.
                        cur_line++;
                        cur_col = 0;
                        index += 1;
                    }
                }
                else if (ch == '\n')
                {
                    cur_line++;
                    cur_col = 0;
                    index += 1;
                }
                else
                {
                    cur_col += 1;
                    index += 1;
                }
                if (index >= buffer.Length)
                {
                    break;
                }
            }
            return index;
        }

        public static (int, int) GetLineColumn(int index, Document doc)
        {
            int cur_index = 0;
            string buffer = doc.Code;
            if (buffer == null)
            {
                return (0, 0);
            }

            int cur_line = 0; // zero based LSP.
            int cur_col = 0; // zero based LSP.
            for (; ; )
            {
                if (cur_index >= buffer.Length)
                {
                    break;
                }

                if (cur_index >= index)
                {
                    break;
                }

                char ch = buffer[cur_index];
                if (ch == '\r')
                {
                    if (cur_index + 1 >= buffer.Length)
                    {
                        break;
                    }
                    else if (buffer[cur_index + 1] == '\n')
                    {
                        cur_line++;
                        cur_col = 0;
                        cur_index += 2;
                    }
                    else
                    {
                        // Error in code.
                        cur_line++;
                        cur_col = 0;
                        cur_index += 1;
                    }
                }
                else if (ch == '\n')
                {
                    cur_line++;
                    cur_col = 0;
                    cur_index += 1;
                }
                else
                {
                    cur_col += 1;
                    cur_index += 1;
                }
                if (cur_index >= buffer.Length)
                {
                    break;
                }
            }
            return (cur_line, cur_col);
        }

        public static QuickInfo GetQuickInfo(int index, Document doc)
        {
            ParserDetails pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            Antlr4.Runtime.Tree.IParseTree pt = LanguageServer.Util.Find(index, doc);
            IGrammarDescription gd = GrammarDescriptionFactory.Create(doc.FullPath);
            if (pt == null)
            {
                return null;
            }

            Antlr4.Runtime.Tree.IParseTree p = pt;
            pd.Attributes.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
            if (list_value == null)
            {
                return null;
            }

            TerminalNodeImpl q = p as Antlr4.Runtime.Tree.TerminalNodeImpl;
            Range range = new Workspaces.Range(new Workspaces.Index(q.Symbol.StartIndex), new Workspaces.Index(q.Symbol.StopIndex + 1));
            bool found = pd.Tags.TryGetValue(q, out int tag_type);
            if (!found)
            {
                return null;
            }

            if (list_value == null || list_value.Count == 0)
            {
                return new QuickInfo() { Display = gd.Map[tag_type], Range = range };
            }
            if (list_value.Count == 1)
            {
                CombinedScopeSymbol value = list_value.First();
                ISymbol name = value as Symtab.ISymbol;
                string show = name?.Name;
                if (value is Symtab.Literal)
                {
                    show = ((Symtab.Literal)value).Cleaned;
                }
                if (gd.PopUpDefinition[tag_type] != null)
                {
                    Func<ParserDetails, IParseTree, string> fun = gd.PopUpDefinition[tag_type];
                    string mess = fun(pd, p);
                    if (mess != null)
                    {
                        return new QuickInfo() { Display = mess, Range = range };
                    }
                }
                string display = gd.Map[tag_type]
                    + "\n"
                    + show;
                return new QuickInfo() { Display = display, Range = range };
            }
            {
                string display = "Ambiguous -- ";
                foreach (CombinedScopeSymbol value in list_value)
                {
                    ISymbol name = value as Symtab.ISymbol;
                    string show = name?.Name;
                    if (value is Symtab.Literal)
                    {
                        show = ((Symtab.Literal)value).Cleaned;
                    }
                    if (gd.PopUpDefinition[tag_type] != null)
                    {
                        Func<ParserDetails, IParseTree, string> fun = gd.PopUpDefinition[tag_type];
                        string mess = fun(pd, p);
                        if (mess != null)
                        {
                            display = display + mess;
                        }
                    }
                    else
                    {
                        display = display + gd.Map[tag_type]
                            + "\n"
                            + show;
                    }
                }
                return new QuickInfo() { Display = display, Range = range };
            }
        }

        public static int GetTag(int index, Document doc)
        {
            ParserDetails pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            Antlr4.Runtime.Tree.IParseTree pt = LanguageServer.Util.Find(index, doc);
            IGrammarDescription gd = GrammarDescriptionFactory.Create(doc.FullPath);
            if (pt == null)
            {
                return -1;
            }

            Antlr4.Runtime.Tree.IParseTree p = pt;
            TerminalNodeImpl q = p as Antlr4.Runtime.Tree.TerminalNodeImpl;
            bool found = pd.Tags.TryGetValue(q, out int tag_type);
            if (found)
            {
                return tag_type;
            }

            if (q.Symbol == null)
            {
                return -1;
            }

            bool found2 = pd.Comments.TryGetValue(q.Symbol, out int tag2);
            if (found2)
            {
                return tag2;
            }

            return -1;
        }

        public static DocumentSymbol GetDocumentSymbol(int index, Document doc)
        {
            ParserDetails pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            Antlr4.Runtime.Tree.IParseTree pt = LanguageServer.Util.Find(index, doc);
            IGrammarDescription gd = GrammarDescriptionFactory.Create(doc.FullPath);
            if (pt == null)
            {
                return default(DocumentSymbol);
            }

            Antlr4.Runtime.Tree.IParseTree p = pt;
            TerminalNodeImpl q = p as Antlr4.Runtime.Tree.TerminalNodeImpl;
            bool found = pd.Tags.TryGetValue(q, out int tag_type);
            if (!found)
            {
                return null;
            }

            if (q.Symbol == null)
            {
                return null;
            }

            return new DocumentSymbol()
            {
                name = q.Symbol.Text,
                range = new Workspaces.Range(q.Symbol.StartIndex, q.Symbol.StopIndex),
                kind = tag_type
            };
        }

        public static IEnumerable<DocumentSymbol> Get(Document doc)
        {
            ParserDetails pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            List<DocumentSymbol> combined = new List<DocumentSymbol>();
            foreach (KeyValuePair<TerminalNodeImpl, int> p in pd.Tags)
            {
                if (p.Key.Symbol == null)
                {
                    continue;
                }

                combined.Add(
                    new DocumentSymbol()
                    {
                        name = p.Key.Symbol.Text,
                        range = new Workspaces.Range(p.Key.Symbol.StartIndex, p.Key.Symbol.StopIndex),
                        kind = p.Value
                    });
            }
            foreach (KeyValuePair<Antlr4.Runtime.IToken, int> p in pd.Comments)
            {
                combined.Add(
                    new DocumentSymbol()
                    {
                        name = p.Key.Text,
                        range = new Workspaces.Range(p.Key.StartIndex, p.Key.StopIndex),
                        kind = p.Value
                    });
            }

            // Sort the list.
            IOrderedEnumerable<DocumentSymbol> sorted_combined_tokens = combined.OrderBy(t => t.range.Start.Value).ThenBy(t => t.range.End.Value);
            return sorted_combined_tokens;
        }

        public static IEnumerable<DocumentSymbol> Get(Workspaces.Range range, Document doc)
        {
            ParserDetails pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            List<DocumentSymbol> combined = new System.Collections.Generic.List<DocumentSymbol>();
            foreach (KeyValuePair<TerminalNodeImpl, int> p in pd.Tags)
            {
                if (p.Key.Symbol == null)
                {
                    continue;
                }

                int start_token_start = p.Key.Symbol.StartIndex;
                int end_token_end = p.Key.Symbol.StopIndex + 1;
                if (start_token_start > range.End.Value)
                {
                    continue;
                }

                if (end_token_end < range.Start.Value)
                {
                    continue;
                }

                combined.Add(
                    new DocumentSymbol()
                    {
                        name = p.Key.Symbol.Text,
                        range = new Workspaces.Range(p.Key.Symbol.StartIndex, p.Key.Symbol.StopIndex),
                        kind = p.Value
                    });
            }
            foreach (KeyValuePair<Antlr4.Runtime.IToken, int> p in pd.Comments)
            {
                int start_token_start = p.Key.StartIndex;
                int end_token_end = p.Key.StopIndex + 1;
                if (start_token_start > range.End.Value)
                {
                    continue;
                }

                if (end_token_end < range.Start.Value)
                {
                    continue;
                }

                combined.Add(
                    new DocumentSymbol()
                    {
                        name = p.Key.Text,
                        range = new Workspaces.Range(p.Key.StartIndex, p.Key.StopIndex),
                        kind = p.Value
                    });
            }

            // Sort the list.
            IEnumerable<DocumentSymbol> result;
            IOrderedEnumerable<DocumentSymbol> sorted_combined_tokens = combined.OrderBy(t => t.range.Start.Value).ThenBy(t => t.range.End.Value);
            result = sorted_combined_tokens;
            return result;
        }

        public static IEnumerable<Workspaces.Range> GetErrors(Workspaces.Range range, Document doc)
        {
            ParserDetails pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            List<Range> result = new List<Workspaces.Range>();
            foreach (IParseTree p in pd.Errors)
            {
                ErrorNodeImpl q = p as Antlr4.Runtime.Tree.ErrorNodeImpl;
                if (q == null)
                {
                    continue;
                }

                if (q.Payload == null)
                {
                    continue;
                }

                int y = q.Payload.StartIndex;
                int z = q.Payload.StopIndex;
                if (y < 0)
                {
                    y = 0;
                }

                if (z < 0)
                {
                    z = 0;
                }

                int a = y;
                int b = z + 1;
                int start_token_start = a;
                int end_token_end = b;
                if (start_token_start > range.End.Value)
                {
                    continue;
                }

                if (end_token_end < range.Start.Value)
                {
                    continue;
                }

                Range r = new Workspaces.Range(new Workspaces.Index(a), new Workspaces.Index(b));
                result.Add(r);
            }
            return result;
        }

        public static IList<Location> FindDef(int index, Document doc)
        {
            List<Location> result = new List<Location>();
            if (doc == null)
            {
                return result;
            }

            IParseTree ref_pt = Util.Find(index, doc);
            if (ref_pt == null)
            {
                return result;
            }

            ParserDetails ref_pd = ParserDetailsFactory.Create(doc);
            if (ref_pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            ref_pd.Attributes.TryGetValue(ref_pt, out IList<Symtab.CombinedScopeSymbol> list_values);
            if (list_values == null)
            {
                return result;
            }

            foreach (CombinedScopeSymbol value in list_values)
            {
                if (value == null)
                {
                    continue;
                }

                ISymbol @ref = value as Symtab.ISymbol;
                if (@ref == null)
                {
                    continue;
                }

                ISymbol def = @ref.resolve();
                if (def == null)
                {
                    continue;
                }

                string def_file = def.file;
                if (def_file == null)
                {
                    continue;
                }

                Document def_item = Workspaces.Workspace.Instance.FindDocument(def_file);
                if (def_item == null)
                {
                    continue;
                }

                Location new_loc = new Location()
                {
                    Range = new Workspaces.Range(def.Token.StartIndex, def.Token.StopIndex),
                    Uri = def_item
                };
                result.Add(new_loc);
            }
            return result;
        }

        public static IEnumerable<Location> FindRefsAndDefs(int index, Document doc)
        {
            List<Location> result = new List<Location>();
            IParseTree ref_pt = Util.Find(index, doc);
            if (ref_pt == null)
            {
                return result;
            }

            ParserDetails ref_pd = ParserDetailsFactory.Create(doc);
            if (ref_pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            ref_pd.Attributes.TryGetValue(ref_pt, out IList<Symtab.CombinedScopeSymbol> list_value);
            if (list_value == null)
            {
                return result;
            }

            ISymbol found_def = null;
            ISymbol found_ref = null;
            foreach (CombinedScopeSymbol value in list_value)
            {
                if (value == null)
                {
                    continue;
                }

                ISymbol @ref = value as Symtab.ISymbol;
                if (@ref == null)
                {
                    continue;
                }

                if (@ref.Token == null)
                {
                    continue;
                }

                found_ref = @ref;
                ISymbol def = @ref.resolve();
                if (def == null)
                {
                    continue;
                }

                if (def.Token == null)
                {
                    continue;
                }

                found_def = def;
                break;
            }
            List<Antlr4.Runtime.Tree.TerminalNodeImpl> where = new List<Antlr4.Runtime.Tree.TerminalNodeImpl>();
            // Go through all files and look for refs.
            foreach (KeyValuePair<string, List<string>> d in AntlrGrammarDetails._dependent_grammars)
            {
                Document d_doc = Workspaces.Workspace.Instance.FindDocument(d.Key);
                ParserDetails d_pd = ParserDetailsFactory.Create(d_doc);
                if (d_pd.ParseTree == null)
                {
                    continue;
                }

                IEnumerable<TerminalNodeImpl> refs = d_pd.Refs.Where(
                    (t) =>
                    {
                        Antlr4.Runtime.Tree.TerminalNodeImpl x = t.Key;
                        if (x.Symbol == found_ref.Token)
                        {
                            return true;
                        }

                        d_pd.Attributes.TryGetValue(x, out IList<Symtab.CombinedScopeSymbol> list_v);
                        if (list_v == null)
                        {
                            return false;
                        }

                        foreach (CombinedScopeSymbol v in list_v)
                        {
                            ISymbol vv = v as Symtab.ISymbol;
                            if (vv == null)
                            {
                                return false;
                            }

                            if (vv.resolve() == found_def)
                            {
                                return true;
                            }

                            return false;
                        }
                        return false;
                    }).Select(t => t.Key);
                foreach (TerminalNodeImpl r in refs)
                {
                    result.Add(
                            new Location()
                            {
                                Range = new Workspaces.Range(r.Symbol.StartIndex, r.Symbol.StopIndex),
                                Uri = Workspaces.Workspace.Instance.FindDocument(r.Symbol.InputStream.SourceName)
                            });
                }
            }
            if (found_def != null)
            {
                result.Add(
                    new Location()
                    {
                        Range = new Workspaces.Range(found_def.Token.StartIndex, found_def.Token.StopIndex),
                        Uri = Workspaces.Workspace.Instance.FindDocument(found_def.file)
                    });
            }
            return result;
        }

        public static IEnumerable<Location> GetDefs(Document doc)
        {
            List<Location> result = new List<Location>();
            ParserDetails ref_pd = ParserDetailsFactory.Create(doc);
            if (ref_pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            foreach (KeyValuePair<TerminalNodeImpl, int> value in ref_pd.Defs)
            {
                TerminalNodeImpl key = value.Key;
                Antlr4.Runtime.IToken sym = key.Payload;
                result.Add(
                    new Location()
                    {
                        Range = new Workspaces.Range(sym.StartIndex, sym.StopIndex),
                        Uri = Workspaces.Workspace.Instance.FindDocument(sym.InputStream.SourceName)
                    });
            }
            return result;
        }

        private static Digraph<ParserDetails> ConstructGraph(IEnumerable<ParserDetails> to_do)
        {
            Digraph<ParserDetails> g = new Digraph<ParserDetails>();
            HashSet<ParserDetails> done = new HashSet<ParserDetails>();
            Stack<ParserDetails> stack = new Stack<ParserDetails>();
            foreach (ParserDetails f in to_do)
            {
                stack.Push(f);
            }

            while (stack.Count > 0)
            {
                ParserDetails f = stack.Pop();
                g.AddVertex(f);
                done.Add(f);
                foreach (string d in f.PropagateChangesTo)
                {
                    Document d_doc = Workspace.Instance.FindDocument(d);
                    ParserDetails d_pd = ParserDetailsFactory.Create(d_doc);
                    if (done.Contains(d_pd))
                    {
                        continue;
                    }

                    stack.Push(d_pd);
                }
            }

            foreach (ParserDetails v in g.Vertices)
            {
                HashSet<string> deps = v.PropagateChangesTo;
                Document doc = Workspace.Instance.FindDocument(v.FullFileName);
                ParserDetails pd = ParserDetailsFactory.Create(doc);
                foreach (string d in deps)
                {
                    Document d_doc = Workspace.Instance.FindDocument(d);
                    ParserDetails d_pd = ParserDetailsFactory.Create(d_doc);
                    g.AddEdge(new DirectedEdge<ParserDetails>(pd, d_pd));
                }
            }

            return g;
        }

        public static List<ParserDetails> Compile()
        {
            try
            {
                Workspace ws = Workspaces.Workspace.Instance;

                // Get all changed files.
                HashSet<ParserDetails> to_do = new HashSet<ParserDetails>();

            DoAgain:

                // Get current directory, and add all grammar files.
                foreach (Document document in Workspaces.DFSContainer.DFS(ws))
                {
                    string file_name = document.FullPath;
                    if (file_name == null)
                    {
                        continue;
                    }

                    Container parent = document.Parent;
                    IGrammarDescription gd = LanguageServer.GrammarDescriptionFactory.Create(file_name);
                    if (gd == null)
                    {
                        continue;
                    }

                    // Get suffix of file_name.
                    string extension = System.IO.Path.GetExtension(file_name);
                    string directory = System.IO.Path.GetDirectoryName(file_name);

                    foreach (string file in System.IO.Directory.GetFiles(directory))
                    {
                        if (System.IO.Path.GetExtension(file) != extension)
                        {
                            continue;
                        }

                        IGrammarDescription g2 = LanguageServer.GrammarDescriptionFactory.Create(file);
                        if (g2 == null)
                        {
                            continue;
                        }

                        Document x = Workspaces.Workspace.Instance.FindDocument(file);
                        if (x == null)
                        {
                            // Add document.
                            Container proj = parent;
                            Document new_doc = new Workspaces.Document(file);
                            proj.AddChild(new_doc);
                        }
                        ParserDetails p2 = ParserDetailsFactory.Create(document);
                        if (!p2.Changed)
                        {
                            continue;
                        }

                        to_do.Add(p2);
                    }
                }

                foreach (Document document in Workspaces.DFSContainer.DFS(ws))
                {
                    string file_name = document.FullPath;
                    if (file_name == null)
                    {
                        continue;
                    }

                    IGrammarDescription gd = LanguageServer.GrammarDescriptionFactory.Create(file_name);
                    if (gd == null)
                    {
                        continue;
                    }
                    // file_name can be a URI, so this doesn't make sense.
                    //if (!System.IO.File.Exists(file_name)) continue;
                    ParserDetails pd = ParserDetailsFactory.Create(document);
                    if (!pd.Changed)
                    {
                        continue;
                    }

                    to_do.Add(pd);
                }
                Digraph<ParserDetails> g = ConstructGraph(to_do);
                foreach (ParserDetails v in g.Vertices)
                {
                    v.Item.Changed = true; // Force.
                    v.Parse();
                }
                bool changed = true;
                for (int pass = 0; changed; pass++)
                {
                    changed = false;
                    foreach (ParserDetails v in g.Vertices)
                    {
                        int number_of_passes = v.Passes.Count;
                        if (pass < number_of_passes)
                        {
                            bool reset = v.Pass(pass);
                            if (reset)
                            {
                                goto DoAgain;
                            }
                            changed = true;
                        }
                    }
                }
                foreach (ParserDetails v in g.Vertices)
                {
                    v.GatherDefs();
                }
                foreach (ParserDetails v in g.Vertices)
                {
                    v.GatherRefs();
                }
                foreach (ParserDetails v in g.Vertices)
                {
                    v.GatherErrors();
                }
                return g.Vertices.ToList();
            }
            catch (Exception e)
            {
                Logger.Log.Notify(e.ToString());
            }
            return new List<ParserDetails>();
        }

        public static TextEdit[] Reformat(Document doc)
        {
            ParserDetails ref_pd = ParserDetailsFactory.Create(doc);
            string code = doc.Code;
            string corpus_location = Options.Option.GetString("CorpusLocation");
            if (corpus_location == null)
            {
                TextEdit[] result = new TextEdit[] { };
                return result;
            }

            string ffn = doc.FullPath;
            if (ffn == null)
            {
                TextEdit[] result = new TextEdit[] { };
                return result;
            }
            IGrammarDescription grammar_description = LanguageServer.GrammarDescriptionFactory.Create(ffn);
            if (grammar_description == null)
            {
                TextEdit[] result = new TextEdit[] { };
                return result;
            }
            org.antlr.codebuff.Tool.unformatted_input = code;
            try
            {
                string result = org.antlr.codebuff.Tool.Main(
                    new object[]
                    {
                    "-g", grammar_description.Name,
                    "-lexer", grammar_description.Lexer,
                    "-parser", grammar_description.Parser,
                    "-rule", grammar_description.StartRule,
                    "-files", grammar_description.FileExtension,
                    "-corpus", corpus_location,
                    "-inoutstring",
                    ""
                    });
                List<TextEdit> edits = new List<TextEdit>();
                diff_match_patch diff = new diff_match_patch();
                List<Diff> diffs = diff.diff_main(code, result);
                List<Patch> patch = diff.patch_make(diffs);
                //patch.Reverse();

                // Start edit session.
                int times = 0;
                int delta = 0;
                foreach (Patch p in patch)
                {
                    times++;
                    int start = p.start1 - delta;

                    int offset = 0;
                    foreach (Diff ed in p.diffs)
                    {
                        if (ed.operation == Operation.EQUAL)
                        {
                            //// Let's verify that.
                            int len = ed.text.Length;
                            //var tokenSpan = new SnapshotSpan(buffer.CurrentSnapshot,
                            //  new Span(start + offset, len));
                            //var tt = tokenSpan.GetText();
                            //if (ed.text != tt)
                            //{ }
                            offset = offset + len;
                        }
                        else if (ed.operation == Operation.DELETE)
                        {
                            int len = ed.text.Length;
                            //var tokenSpan = new SnapshotSpan(buffer.CurrentSnapshot,
                            //  new Span(start + offset, len));
                            //var tt = tokenSpan.GetText();
                            //if (ed.text != tt)
                            //{ }
                            TextEdit edit = new TextEdit()
                            {
                                range = new Workspaces.Range(
                                    new Workspaces.Index(start + offset),
                                    new Workspaces.Index(start + offset + len)),
                                NewText = ""
                            };
                            offset = offset + len;
                            edits.Add(edit);
                        }
                        else if (ed.operation == Operation.INSERT)
                        {
                            int len = ed.text.Length;
                            TextEdit edit = new TextEdit()
                            {
                                range = new Workspaces.Range(
                                    new Workspaces.Index(start + offset),
                                    new Workspaces.Index(start + offset)),
                                NewText = ed.text
                            };
                            edits.Add(edit);
                        }
                    }
                    delta = delta + (p.length2 - p.length1);
                }
                return edits.ToArray();
            }
            catch (Exception)
            {
                TextEdit[] result = new TextEdit[] { };
                return result;
            }
        }

        public static Dictionary<string, TextEdit[]> Rename(int index, string new_text, Document doc)
        {
            IEnumerable<Location> locations = LanguageServer.Module.FindRefsAndDefs(index, doc);
            Dictionary<string, TextEdit[]> result = new Dictionary<string, TextEdit[]>();
            IEnumerable<Document> documents = locations.Select(r => r.Uri).OrderBy(q => q).Distinct();
            foreach (Document f in documents)
            {
                string fn = f.FullPath;
                IOrderedEnumerable<Location> per_file_changes = locations.Where(z => z.Uri == f).OrderBy(q => q.Range.Start.Value);
                StringBuilder sb = new StringBuilder();
                int previous = 0;
                string code = f.Code;
                foreach (Location l in per_file_changes)
                {
                    Document d = l.Uri;
                    string xx = d.FullPath;
                    Range r = l.Range;
                    string pre = code.Substring(previous, r.Start.Value - previous);
                    sb.Append(pre);
                    sb.Append(new_text);
                    previous = r.End.Value + 1;
                }
                string rest = code.Substring(previous);
                sb.Append(rest);
                string new_code = sb.ToString();
                List<TextEdit> edits = new List<TextEdit>();
                diff_match_patch diff = new diff_match_patch();
                List<Diff> diffs = diff.diff_main(code, new_code);
                List<Patch> patch = diff.patch_make(diffs);
                int times = 0;
                int delta = 0;
                foreach (Patch p in patch)
                {
                    times++;
                    int start = p.start1 - delta;
                    int offset = 0;
                    foreach (Diff ed in p.diffs)
                    {
                        if (ed.operation == Operation.EQUAL)
                        {
                            //// Let's verify that.
                            int len = ed.text.Length;
                            //var tokenSpan = new SnapshotSpan(buffer.CurrentSnapshot,
                            //  new Span(start + offset, len));
                            //var tt = tokenSpan.GetText();
                            //if (ed.text != tt)
                            //{ }
                            offset = offset + len;
                        }
                        else if (ed.operation == Operation.DELETE)
                        {
                            int len = ed.text.Length;
                            //var tokenSpan = new SnapshotSpan(buffer.CurrentSnapshot,
                            //  new Span(start + offset, len));
                            //var tt = tokenSpan.GetText();
                            //if (ed.text != tt)
                            //{ }
                            TextEdit edit = new TextEdit()
                            {
                                range = new Workspaces.Range(
                                    new Workspaces.Index(start + offset),
                                    new Workspaces.Index(start + offset + len)),
                                NewText = ""
                            };
                            offset = offset + len;
                            edits.Add(edit);
                        }
                        else if (ed.operation == Operation.INSERT)
                        {
                            int len = ed.text.Length;
                            TextEdit edit = new TextEdit()
                            {
                                range = new Workspaces.Range(
                                    new Workspaces.Index(start + offset),
                                    new Workspaces.Index(start + offset)),
                                NewText = ed.text
                            };
                            edits.Add(edit);
                        }
                    }
                    delta = delta + (p.length2 - p.length1);
                }
                TextEdit[] e = edits.ToArray();
                result.Add(fn, e);
            }
            return result;
        }

        public static List<string> Completion(int char_index, Document document)
        {
            ParserDetails ref_pd = ParserDetailsFactory.Create(document);
            if (ref_pd.ParseTree == null)
            {
                LanguageServer.Module.Compile();
            }

            List<string> result = ref_pd.Candidates(char_index);
            return result;
        }
    }
}
