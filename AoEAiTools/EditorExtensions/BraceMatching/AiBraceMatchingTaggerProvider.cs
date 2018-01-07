using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.EditorExtensions.BraceMatching
{
    /// <summary>
    /// Exports the AI brace matching tagger.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(AiContentTypeDefinition.ContentType)]
    [TagType(typeof(TextMarkerTag))]
    internal class AiBraceMatchingTaggerProvider : IViewTaggerProvider
    {
        /// <summary>
        /// Creates a AI brace matching tagger.
        /// </summary>
        /// <typeparam name="T">The desired tagger type (should be TextMarkerTag).</typeparam>
        /// <param name="textView">The current text view.</param>
        /// <param name="sourceBuffer">The current text view contents.</param>
        /// <returns></returns>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer sourceBuffer) where T : ITag
        {
            // Do null checks, provide highlighting only on the top-level buffer
            if(textView == null)
                return null;
            if(textView.TextBuffer != sourceBuffer)
                return null;

            // Create tagger
            return new AiBraceMatchingTagger(textView, sourceBuffer) as ITagger<T>;
        }
    }
}
