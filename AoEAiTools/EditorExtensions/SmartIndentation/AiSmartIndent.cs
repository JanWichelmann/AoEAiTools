using AoEAiTools.LanguageParsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.EditorExtensions.SmartIndentation
{
    /// <summary>
    /// Provides smart identations.
    /// </summary>
    class AiSmartIndent : ISmartIndent
    {
        /// <summary>
        /// The current text view.
        /// </summary>
        private readonly ITextView _textView;

        /// <summary>
        /// Determines whether tabs shall be used instead of spaces.
        /// </summary>
        private bool _useTabs;

        /// <summary>
        /// The amount of spaces per indentation.
        /// </summary>
        private int _indentSize;

        /// <summary>
        /// An AI parser object for tokenizing text.
        /// </summary>
        private AiParser _aiParser;

        /// <summary>
        /// Creates a new indentation object.
        /// </summary>
        /// <param name="textView">The current text view.</param>
        public AiSmartIndent(ITextView textView)
        {
            // Save parameters
            _textView = textView;

            // Get indentation options
            _useTabs = _textView.Options.GetOptionValue(new EditorOptionKey<bool>("Tabs/ConvertTabsToSpaces"));
            _indentSize = _textView.Options.GetOptionValue(new EditorOptionKey<int>("Tabs/IndentSize"));

            // Create parser object
            _aiParser = new AiParser();
        }

        /// <summary>
        /// Returns the indentation for the given line.
        /// </summary>
        /// <param name="indentLine">The line to be indented.</param>
        /// <returns></returns>
        public int? GetDesiredIndentation(ITextSnapshotLine indentLine)
        {
            // Make sure there is a line before the requested line
            if(indentLine.LineNumber == 0 || indentLine.Start.Position < 1)
                return 0;

            // Get indentation of previous line
            string previousLineText = indentLine.Snapshot.GetLineFromLineNumber(indentLine.LineNumber - 1).GetText();
            int previousLineIndentation = 0;
            while(previousLineIndentation < previousLineText.Length && char.IsWhiteSpace(previousLineText[previousLineIndentation]))
                ++previousLineIndentation;

            // Find first token before this line
            var tokens = _aiParser.GetTokens(indentLine.Start - 1, true);
            if(!tokens.Any())
                return 0;
            AiToken firstToken = tokens.First();
            switch(firstToken.Type)
            {
                case AiTokenTypes.ClosingBrace:
                    return previousLineIndentation;
                default:
                    return previousLineIndentation + (_useTabs ? 4 : _indentSize); // TODO _useTabs is always true, 4 is hardcoded as long as this is not fixed
            }
        }

        public void Dispose()
        {
            // Nothing to do
        }
    }
}
