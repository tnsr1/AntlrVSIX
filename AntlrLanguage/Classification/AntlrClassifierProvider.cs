﻿using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using AntlrLanguage.Tag;

namespace AntlrLanguage
{
    // Please refer to Language Service and Editor Extension Points,
    // https://msdn.microsoft.com/en-us/library/dd885244.aspx,
    // for information on how this Managed Extensiblility Framework (MEF)
    // extension hooks into Visual Studio 2015.

    [Export(typeof(ITaggerProvider))]
    [ContentType(Constants.ContentType)]
    [TagType(typeof(ClassificationTag))]
    internal sealed class AntlrClassifierProvider : ITaggerProvider
    {
        [Export]
        [Name("Antlr")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition AntlrContentType = null;

        [Export]
        [FileExtension(Constants.FileExtension)]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition AntlrFileType = null;

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IBufferTagAggregatorFactoryService aggregatorFactory = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagAggregator<AntlrTokenTag> antlrTagAggregator =
                                            aggregatorFactory.CreateTagAggregator<AntlrTokenTag>(buffer);

            return new AntlrClassifier(null, buffer, antlrTagAggregator, ClassificationTypeRegistry) as ITagger<T>;
        }
    }
}
