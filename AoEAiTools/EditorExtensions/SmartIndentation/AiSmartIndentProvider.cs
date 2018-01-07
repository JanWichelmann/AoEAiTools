using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.EditorExtensions.SmartIndentation
{
    /// <summary>
    /// Exposes a handler for smart identation.
    /// </summary>
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(AiContentTypeDefinition.ContentType)]
    internal class AiSmartIndentProvider : ISmartIndentProvider
    {
        /// <summary>
        /// Generates an indent handler for the given text view.
        /// </summary>
        /// <param name="textView">The current text view.</param>
        /// <returns></returns>
        public ISmartIndent CreateSmartIndent(ITextView textView)
        {
            // Create handler
            return new AiSmartIndent(textView);
        }
    }
}
