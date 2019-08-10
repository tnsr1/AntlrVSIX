﻿namespace AntlrVSIX.GoToVisitor
{
    using Antlr4.Runtime;
    using AntlrVSIX.Extensions;
    using AntlrVSIX.Grammar;
    using AntlrVSIX.Keyboard;
    using AntlrVSIX.Package;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Linq;

    public class GoToVisitorCommand
    {
        private readonly Microsoft.VisualStudio.Shell.Package _package;
        private MenuCommand _menu_item1;
        private MenuCommand _menu_item2;
        private MenuCommand _menu_item3;
        private MenuCommand _menu_item4;

        private GoToVisitorCommand(Microsoft.VisualStudio.Shell.Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }
            _package = package;
            OleMenuCommandService commandService = this.ServiceProvider.GetService(
                typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService == null)
            {
                throw new ArgumentNullException("OleMenuCommandService");
            }

            {
                // Set up hook for context menu.
                var menuCommandID = new CommandID(new Guid(AntlrVSIX.Constants.guidVSPackageCommandCodeWindowContextMenuCmdSet), 0x7005);
                _menu_item1 = new MenuCommand(this.MenuItemCallbackListener, menuCommandID);
                _menu_item1.Enabled = false;
                _menu_item1.Visible = true;
                commandService.AddCommand(_menu_item1);
            }
            {
                // Set up hook for context menu.
                var menuCommandID = new CommandID(new Guid(AntlrVSIX.Constants.guidVSPackageCommandCodeWindowContextMenuCmdSet), 0x7006);
                _menu_item2 = new MenuCommand(this.MenuItemCallbackVisitor, menuCommandID);
                _menu_item2.Enabled = false;
                _menu_item2.Visible = true;
                commandService.AddCommand(_menu_item2);
            }
            {
                // Set up hook for context menu.
                var menuCommandID = new CommandID(new Guid(AntlrVSIX.Constants.guidMenuAndCommandsCmdSet), 0x7005);
                _menu_item3 = new MenuCommand(this.MenuItemCallbackListener, menuCommandID);
                _menu_item3.Enabled = false;
                _menu_item3.Visible = true;
                commandService.AddCommand(_menu_item3);
            }
            {
                // Set up hook for context menu.
                var menuCommandID = new CommandID(new Guid(AntlrVSIX.Constants.guidMenuAndCommandsCmdSet), 0x7006);
                _menu_item4 = new MenuCommand(this.MenuItemCallbackVisitor, menuCommandID);
                _menu_item4.Enabled = false;
                _menu_item4.Visible = true;
                commandService.AddCommand(_menu_item4);
            }
        }

        public bool Enabled
        {
            set
            {
                if (_menu_item1 != null) _menu_item1.Enabled = value;
                if (_menu_item2 != null) _menu_item2.Enabled = value;
                if (_menu_item3 != null) _menu_item3.Enabled = value;
                if (_menu_item4 != null) _menu_item4.Enabled = value;
            }
        }

        public bool Visible
        {
            set { }
        }

        public static GoToVisitorCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return this._package; }
        }

        public static void Initialize(Microsoft.VisualStudio.Shell.Package package)
        {
            Instance = new GoToVisitorCommand(package);
        }

        private void MenuItemCallbackVisitor(object sender, EventArgs e)
        {
            MenuItemCallback(sender, e, true);
        }

        private void MenuItemCallbackListener(object sender, EventArgs e)
        {
            MenuItemCallback(sender, e, false);
        }

        private void MenuItemCallback(object sender, EventArgs e, bool visitor)
        {
            // Return if I can't determine what application this is.
            DTE application = DteExtensions.GetApplication();
            if (application == null) return;

            // Get active view and determine if it's a grammar file.
            var grammar_view = AntlrLanguagePackage.Instance.GetActiveView();
            if (grammar_view == null) return;
            ITextCaret car = grammar_view.Caret;
            CaretPosition cp = car.Position;
            SnapshotPoint bp = cp.BufferPosition;
            int pos = bp.Position;
            ITextBuffer buffer = grammar_view.TextBuffer;
            ITextDocument doc = buffer.GetTextDocument();
            string g4_file_path = doc.FilePath;
            if (!g4_file_path.IsAntlrSuffix()) return;

            // Get name of base class for listener and visitor. These are generated by Antlr,
            // constructed from the name of the file.
            var grammar_name = Path.GetFileName(doc.FilePath);
            grammar_name = Path.GetFileNameWithoutExtension(grammar_name);
            var listener_baseclass_name = visitor ? (grammar_name + "BaseVisitor") : (grammar_name + "BaseListener");
            var listener_class_name = visitor ? ("My" + grammar_name + "Visitor") : ("My" + grammar_name + "Listener");

            // In the current view, find the details of the Antlr symbol at the cursor.
            TextExtent extent = AntlrVSIX.Package.AntlrLanguagePackage.Instance.Navigator[grammar_view].GetExtentOfWord(bp);
            SnapshotSpan span = extent.Span;
            AntlrLanguagePackage.Instance.Span = span;

            //  Now, check for valid classification type.
            ClassificationSpan[] c1 = AntlrVSIX.Package.AntlrLanguagePackage.Instance.Aggregator[grammar_view]
                .GetClassificationSpans(span).ToArray();
            foreach (ClassificationSpan classification in c1)
            {
                var cname = classification.ClassificationType.Classification.ToLower();
                if (cname == AntlrVSIX.Constants.ClassificationNameTerminal)
                {
                    AntlrLanguagePackage.Instance.Classification = AntlrVSIX.Constants.ClassificationNameTerminal;
                }
                else if (cname == AntlrVSIX.Constants.ClassificationNameNonterminal)
                {
                    AntlrLanguagePackage.Instance.Classification = AntlrVSIX.Constants.ClassificationNameNonterminal;
                }
                else if (cname == AntlrVSIX.Constants.ClassificationNameLiteral)
                {
                    AntlrLanguagePackage.Instance.Classification = AntlrVSIX.Constants.ClassificationNameLiteral;
                }
            }

            // Determine if the symbol is a rule symbol. The symbol has to be a nonterminal.
            List<IToken> where = new List<IToken>();
            List<ParserDetails> where_details = new List<ParserDetails>();
            IToken token = null;
            foreach (var kvp in ParserDetails._per_file_parser_details)
            {
                string file_name = kvp.Key;
                ParserDetails details = kvp.Value;
                if (AntlrLanguagePackage.Instance.Classification == AntlrVSIX.Constants.ClassificationNameNonterminal)
                {
                    var it = details._ant_nonterminals_defining.Where(
                        (t) => t.Text == span.GetText());
                    where.AddRange(it);
                    foreach (var i in it) where_details.Add(details);
                }
            }

            if (where.Any()) token = where.First();
            else return;

            // Get the symbol name as a string.
            var symbol_name = token.Text;
            var capitalized_symbol_name = Capitalized(symbol_name);

            // Parse all the C# files in the solution.
            Dictionary<string, SyntaxTree> trees = new Dictionary<string, SyntaxTree>();
            foreach (var item in DteExtensions.SolutionFiles(application))
            {
                string file_name = item.Name;
                if (file_name != null)
                {
                    string prefix = file_name.TrimSuffix(".cs");
                    if (prefix == file_name) continue;
                    try
                    {
                        object prop = item.Properties.Item("FullPath").Value;
                        string ffn = (string) prop;
                        StreamReader sr = new StreamReader(ffn);
                        string code = sr.ReadToEnd();
                        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                        trees[ffn] = tree;
                    }
                    catch (Exception eeks)
                    {
                    }
                }
            }

            // Find first occurence of visitor class.
            ClassDeclarationSyntax found_class = null;
            string class_file_path = null;
            try
            {
                foreach (var kvp in trees)
                {
                    var file_name = kvp.Key;
                    var tree = kvp.Value;

                    // Look for IParseTreeListener or IParseTreeVisitor classes.
                    var root = (CompilationUnitSyntax)tree.GetRoot();
                    if (root == null) continue;
                    foreach (var nm in root.Members)
                    {
                        var namespace_member = nm as NamespaceDeclarationSyntax;
                        if (namespace_member == null) continue;
                        foreach (var cm in namespace_member.Members)
                        {
                            var class_member = cm as ClassDeclarationSyntax;
                            if (class_member == null) continue;
                            var bls = class_member.BaseList;
                            if (bls == null) continue;
                            var types = bls.Types;
                            foreach (var type in types)
                            {
                                var s = type.ToString();
                                if (s.ToString() == listener_baseclass_name)
                                {
                                    // Found the right class.
                                    found_class = class_member;
                                    class_file_path = file_name;
                                    throw new Exception();
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            if (found_class == null)
            {
                if (!Options.OptionsCommand.Instance.GenerateVisitorListener)
                    return;

                // Look in grammar directory for any C# files.
                string name_space = null;
                string ffn = Path.GetFullPath(g4_file_path);
                ffn = Path.GetDirectoryName(ffn);
                foreach (var i in DteExtensions.SolutionFiles(application))
                {
                    string file_name = i.Name;
                    if (file_name != null)
                    {
                        string prefix = file_name.TrimSuffix(".cs");
                        if (prefix == file_name) continue;
                        try
                        {
                            object prop = i.Properties.Item("FullPath").Value;
                            string ffncs = (string)prop;
                            // Look for namespace.
                            var t = trees[ffncs];
                            if (t == null) continue;
                            var root = t.GetCompilationUnitRoot();
                            foreach (var nm in root.Members)
                            {
                                var namespace_member = nm as NamespaceDeclarationSyntax;
                                if (namespace_member == null) continue;
                                name_space = namespace_member.Name.ToString();
                                break;
                            }
                        }
                        catch (Exception eeks)
                        {
                        }
                    }
                }
                if (name_space == null) name_space = "Generated";

                // Create class.
                string clazz = visitor ? $@"
using System;
using System.Collections.Generic;
using System.Text;

namespace {name_space}
{{
    class {listener_class_name}<Result> : {listener_baseclass_name}<Result>
    {{
        //public override Result VisitA([NotNull] A3Parser.AContext context)
        //{{
        //  return VisitChildren(context);
        //}}
    }}
}}
"
                : $@"
using System;
using System.Collections.Generic;
using System.Text;

namespace {name_space}
{{
    class {listener_class_name} : {listener_baseclass_name}
    {{
        //public override void EnterA(A3Parser.AContext context)
        //{{
        //    base.EnterA(context);
        //}}
        //public override void ExitA(A3Parser.AContext context)
        //{{
        //    base.ExitA(context);
        //}}
    }}
}}
";

                class_file_path = ffn + Path.DirectorySeparatorChar + listener_class_name + ".cs";
                System.IO.File.WriteAllText(class_file_path, clazz);
                object item = ProjectHelpers.GetSelectedItem();
                string folder = FindFolder(item);
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                    return;
                var file = new FileInfo(class_file_path);
                var selectedItem = item as ProjectItem;
                var selectedProject = item as Project;
                Project project = selectedItem?.ContainingProject ?? selectedProject ?? null;
                var projectItem = project.AddFileToProject(file);
                // Redo parse.
                try
                {
                    StreamReader sr = new StreamReader(class_file_path);
                    string code = sr.ReadToEnd();
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                    trees[class_file_path] = tree;
                }
                catch (Exception eeks)
                {
                }
                // Redo find class.
                try
                {
                    var tree = trees[class_file_path];
                    var save = class_file_path;
                    class_file_path = null;
                    // Look for IParseTreeListener or IParseTreeVisitor classes.
                    var root = (CompilationUnitSyntax)tree.GetRoot();
                    foreach (var nm in root.Members)
                    {
                        var namespace_member = nm as NamespaceDeclarationSyntax;
                        if (namespace_member == null) continue;
                        foreach (var cm in namespace_member.Members)
                        {
                            var class_member = cm as ClassDeclarationSyntax;
                            if (class_member == null) continue;
                            var bls = class_member.BaseList;
                            if (bls == null) continue;
                            var types = bls.Types;
                            foreach (var type in types)
                            {
                                var s = type.ToString();
                                if (s.ToString() == listener_baseclass_name)
                                {
                                    // Found the right class.
                                    found_class = class_member;
                                    class_file_path = save;
                                    throw new Exception();
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            // Look for enter or exit method for symbol.
            MethodDeclarationSyntax found_member = null;
            var ctl = CtrlKeyState.GetStateForView(grammar_view).Enabled;
            var capitalized_member_name = "";
            if (visitor) capitalized_member_name = "Visit" + capitalized_symbol_name;
            else if (ctl) capitalized_member_name = "Exit" + capitalized_symbol_name;
            else capitalized_member_name = "Enter" + capitalized_symbol_name;
            var capitalized_grammar_name = Capitalized(grammar_name);
            try
            {
                foreach (var me in found_class.Members)
                {
                    var method_member = me as MethodDeclarationSyntax;
                    if (method_member == null) continue;
                    if (method_member.Identifier.ValueText.ToLower() == capitalized_member_name.ToLower())
                    {
                        found_member = method_member;
                        throw new Exception();
                    }
                }
            }
            catch
            {
            }
            if (found_member == null)
            {
                if (!Options.OptionsCommand.Instance.GenerateVisitorListener)
                    return;

                // Find point for edit.
                var here = found_class.OpenBraceToken;
                var spn = here.FullSpan;
                var end = spn.End;

                IVsTextView vstv = IVsTextViewExtensions.GetIVsTextView(class_file_path);
                if (vstv == null)
                {
                    IVsTextViewExtensions.ShowFrame(class_file_path);
                    vstv = IVsTextViewExtensions.GetIVsTextView(class_file_path);
                }
                IWpfTextView wpftv = vstv.GetIWpfTextView();
                if (wpftv == null) return;
                ITextSnapshot cc = wpftv.TextBuffer.CurrentSnapshot;

                var res = vstv.GetBuffer(out IVsTextLines ppBuffer);

                var nss = new SnapshotSpan(cc, spn.End + 1, 0);
                var txt_span = nss.Span;

                int line_number;
                int colum_number;
                vstv.GetLineAndColumn(txt_span.Start, out line_number, out colum_number);
                res = ppBuffer.CreateEditPoint(line_number, colum_number, out object ppEditPoint);
                EditPoint editPoint = ppEditPoint as EditPoint;
                // Create class.
                string member = visitor ? $@"
public override Result {capitalized_member_name}([NotNull] {capitalized_grammar_name}Parser.{capitalized_symbol_name}Context context)
{{
    return VisitChildren(context);
}}
"
                    : $@"
public override void {capitalized_member_name}({capitalized_grammar_name}Parser.{capitalized_symbol_name}Context context)
{{
    base.{capitalized_member_name}(context);
}}
";
                editPoint.Insert(member);
                // Redo parse.
                try
                {
                    vstv = IVsTextViewExtensions.GetIVsTextView(class_file_path);
                    if (vstv == null)
                    {
                        IVsTextViewExtensions.ShowFrame(class_file_path);
                        vstv = IVsTextViewExtensions.GetIVsTextView(class_file_path);
                    }
                    var text_buffer = vstv.GetITextBuffer();
                    var code = text_buffer.GetBufferText();
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                    trees[class_file_path] = tree;
                }
                catch (Exception eeks)
                {
                }
                // Redo find class.
                try
                {
                    var tree = trees[class_file_path];
                    var save = class_file_path;
                    class_file_path = null;
                    // Look for IParseTreeListener or IParseTreeVisitor classes.
                    var root = (CompilationUnitSyntax)tree.GetRoot();
                    foreach (var nm in root.Members)
                    {
                        var namespace_member = nm as NamespaceDeclarationSyntax;
                        if (namespace_member == null) continue;
                        foreach (var cm in namespace_member.Members)
                        {
                            var class_member = cm as ClassDeclarationSyntax;
                            if (class_member == null) continue;
                            var bls = class_member.BaseList;
                            if (bls == null) continue;
                            var types = bls.Types;
                            foreach (var type in types)
                            {
                                var s = type.ToString();
                                if (s.ToString() == listener_baseclass_name)
                                {
                                    // Found the right class.
                                    found_class = class_member;
                                    class_file_path = save;
                                    throw new Exception();
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
                try
                {
                    foreach (var me in found_class.Members)
                    {
                        var method_member = me as MethodDeclarationSyntax;
                        if (method_member == null) continue;
                        if (method_member.Identifier.ValueText.ToLower() == capitalized_member_name.ToLower())
                        {
                            found_member = method_member;
                            throw new Exception();
                        }
                    }
                }
                catch
                {
                }
            }

            {
                // Open to this line in editor.
                IVsTextView vstv = IVsTextViewExtensions.GetIVsTextView(class_file_path);
                {
                    IVsTextViewExtensions.ShowFrame(class_file_path);
                    vstv = IVsTextViewExtensions.GetIVsTextView(class_file_path);
                }

                IWpfTextView wpftv = vstv.GetIWpfTextView();
                if (wpftv == null) return;

                int line_number;
                int colum_number;
                var txt_span = found_member.Identifier.Span;
                vstv.GetLineAndColumn(txt_span.Start, out line_number, out colum_number);

                // Create new span in the appropriate view.
                ITextSnapshot cc = wpftv.TextBuffer.CurrentSnapshot;
                SnapshotSpan ss = new SnapshotSpan(cc, txt_span.Start, txt_span.Length);
                SnapshotPoint sp = ss.Start;
                // Put cursor on symbol.
                wpftv.Caret.MoveTo(sp); // This sets cursor, bot does not center.
                // Center on cursor.
                //wpftv.Caret.EnsureVisible(); // This works, sort of. It moves the scroll bar, but it does not CENTER! Does not really work!
                if (line_number > 0)
                    vstv.CenterLines(line_number - 1, 2);
                else
                    vstv.CenterLines(line_number, 1);
                return;
            }
        }

        private static string FindFolder(object item)
        {
            if (item == null)
                return null;
            DTE application = DteExtensions.GetApplication();
            if (application == null) return "";
            if (application.ActiveWindow is Window2 window && window.Type == vsWindowType.vsWindowTypeDocument)
            {
                // if a document is active, use the document's containing directory
                Document doc = application.ActiveDocument;
                if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                {
                    ProjectItem docItem = application.Solution.FindProjectItem(doc.FullName);

                    if (docItem != null && docItem.Properties != null)
                    {
                        string fileName = docItem.Properties.Item("FullPath").Value.ToString();
                        if (System.IO.File.Exists(fileName))
                            return Path.GetDirectoryName(fileName);
                    }
                }
            }

            string folder = null;

            var projectItem = item as ProjectItem;
            if (projectItem != null && "{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}" == projectItem.Kind) //Constants.vsProjectItemKindVirtualFolder
            {
                ProjectItems items = projectItem.ProjectItems;
                foreach (ProjectItem it in items)
                {
                    if (System.IO.File.Exists(it.FileNames[1]))
                    {
                        folder = Path.GetDirectoryName(it.FileNames[1]);
                        break;
                    }
                }
            }
            else
            {
                var project = item as Project;
                if (projectItem != null)
                {
                    string fileName = projectItem.FileNames[1];

                    if (System.IO.File.Exists(fileName))
                    {
                        folder = Path.GetDirectoryName(fileName);
                    }
                    else
                    {
                        folder = fileName;
                    }
                }
                else if (project != null)
                {
                    folder = project.GetRootFolder();
                }
            }
            return folder;
        }

        string Capitalized(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
