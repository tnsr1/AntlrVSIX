﻿namespace AntlrVSIX
{
    using AntlrVSIX.Extensions;
    using AntlrVSIX.Grammar;
    using AntlrVSIX.GrammarDescription;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Utilities;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(AntlrVSIX.Constants.ContentType)]
    [Name("AntlrCompletion")]
    class AntlrCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new AntlrCompletionSource(textBuffer);
        }
    }

    class AntlrCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        
        public AntlrCompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            ITextDocument doc = _buffer.GetTextDocument();
            if (doc == null) return;
            var ffn = doc.FilePath;
            var gd = AntlrVSIX.GrammarDescription.GrammarDescriptionFactory.Create(ffn);
            if (gd == null) return;
            List<Completion> completions = new List<Completion>();
            var item = AntlrVSIX.GrammarDescription.Workspace.Instance.FindDocumentFullName(doc.FilePath);
            var pd = ParserDetailsFactory.Create(item);
            foreach (var s in pd.Refs
                .Where(t => t.Value == 0)
                .Select(t => t.Key)
                .OrderBy(p => p.Symbol.Text)
                .Select(p => p.Symbol.Text)
                .Distinct())
            {
                completions.Add(new Completion(s));
            }
            foreach (var s in pd.Refs
                .Where(t => t.Value == 1)
                .Select(t => t.Key)
                .OrderBy(p => p.Symbol.Text)
                .Select(p => p.Symbol.Text)
                .Distinct())
            {
                completions.Add(new Completion(s));
            }

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            SnapshotPoint snapshot_point = (SnapshotPoint)session.GetTriggerPoint(snapshot);

            if (snapshot_point == null)
                return;

            var line = snapshot_point.GetContainingLine();
            SnapshotPoint start = snapshot_point;

            while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
            {
                start -= 1;
            }

            ITrackingSpan tracking_span = snapshot.CreateTrackingSpan(new SnapshotSpan(start, snapshot_point), SpanTrackingMode.EdgeInclusive);

            completionSets.Add(new CompletionSet("All", "All", tracking_span, completions, Enumerable.Empty<Completion>()));
        }

        public void Dispose()
        {
        }
    }
}

