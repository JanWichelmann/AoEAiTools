using AoEAiTools.LanguageParsing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.EditorExtensions.Formatting
{
    /// <summary>
    /// Triggers code formatting.
    /// </summary>
    internal class AiFormatCommandHandler : IOleCommandTarget
    {
        private IOleCommandTarget _nextCommandHandler;
        private ITextView _textView;
        private AiFormatHandlerProvider _provider;

        /// <summary>
        /// An AI parser object for tokenizing text.
        /// </summary>
        private AiParser _aiParser;

        internal AiFormatCommandHandler(IVsTextView textViewAdapter, ITextView textView, AiFormatHandlerProvider provider)
        {
            // Save arguments
            _textView = textView;
            _provider = provider;

            // Create parser object
            _aiParser = new AiParser();

            // Add the command to the command chain
            textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
        }

        /// <summary>
        /// Retrieves the commands this class supports.
        /// </summary>
        /// <param name="commandGroupGuid"></param>
        /// <param name="commandCount"></param>
        /// <param name="programmCommands"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public int QueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] programmCommands, IntPtr commandText)
        {
            // Set supported commands
            if(commandGroupGuid == typeof(VSConstants.VSStd2KCmdID).GUID)
                for(int i = 0; i < commandCount; ++i)
                    switch((VSConstants.VSStd2KCmdID)programmCommands[i].cmdID)
                    {
                        case VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
                            //case VSConstants.VSStd2KCmdID.FORMATSELECTION:
                            programmCommands[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            break;
                    }

            // Pass to next command handler, if necessary
            if(programmCommands.All(pc => pc.cmdf != 0))
                return VSConstants.S_OK;
            return _nextCommandHandler.QueryStatus(ref commandGroupGuid, commandCount, programmCommands, commandText);
        }

        public int Exec(ref Guid commandGroupGuid, uint commandId, uint commandExecuteOptionsCount, IntPtr variantIn, IntPtr variantOut)
        {
            // Handle depending on command
            if(commandGroupGuid == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                switch((VSConstants.VSStd2KCmdID)commandId)
                {
                    case VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
                    {
                        // Run formatter
                        FormatSpan(new SnapshotSpan(_textView.TextSnapshot, 0, _textView.TextSnapshot.Length));
                        return VSConstants.S_OK;
                    }
                }
            }

            // Command not handled, call the next handler
            return _nextCommandHandler.Exec(ref commandGroupGuid, commandId, commandExecuteOptionsCount, variantIn, variantOut);
        }

        /// <summary>
        /// Formats the given span.
        /// </summary>
        /// <param name="span">The span to be formatted.</param>
        private void FormatSpan(SnapshotSpan span)
        {
            // TODO this is hardcoded for spaces (not tabs)

            // Always format whole lines
            ITextSnapshot snapshot = span.Snapshot;
            SnapshotPoint currentPoint = span.Start.GetContainingLine().Start;
            SnapshotPoint endPoint = span.End.GetContainingLine().End;

            // Get indentation of previous line, if there is one
            int currentIndentLevel = 0;
            if(currentPoint.GetContainingLine().LineNumber > 0)
            {
                // Count whitespace at line start
                string previousLineText = snapshot.GetLineFromLineNumber(currentPoint.GetContainingLine().LineNumber - 1).GetText();
                while(currentIndentLevel < previousLineText.Length && char.IsWhiteSpace(previousLineText[currentIndentLevel]))
                    ++currentIndentLevel;
            }

            // Get tokens
            var tokens = _aiParser.GetTokens(currentPoint);
            if(!tokens.Any())
                return;
            tokens.SkipWhile(t => t.Type == AiTokenTypes.Comment);

            // Keep track of all modifications
            List<SnapshotModification> modifications = new List<SnapshotModification>();

            // Set indentation of first token
            AiToken lastNonCommentToken = tokens.First();
            AiToken lastToken = tokens.First();
            foreach(AiToken currentToken in tokens.Skip(1))
            {
                // Compute decision for the current token
                FormatDecision decision = FormatDecision.Keep;
                int indentOffset = 0; // The number of levels the indentation shall be changed
                switch(currentToken.Type)
                {
                    case AiTokenTypes.OpeningBrace:
                    {
                        // Always put into a new line
                        decision = FormatDecision.NewLineIndent;
                        if(lastNonCommentToken.Type == AiTokenTypes.OpeningBrace
                            || lastNonCommentToken.Type == AiTokenTypes.Defrule
                            || lastNonCommentToken.Type == AiTokenTypes.BooleanFactName
                            || lastNonCommentToken.Type == AiTokenTypes.RuleArrow)
                            indentOffset = 4;
                        break;
                    }

                    case AiTokenTypes.ClosingBrace:
                    {
                        // If the last token was a closing brace, then break and indent one level less than the last one, else remove any space
                        if(lastNonCommentToken.Type == AiTokenTypes.ClosingBrace)
                        {
                            decision = FormatDecision.NewLineIndent;
                            indentOffset = -4;
                        }
                        else if(lastToken.Type != AiTokenTypes.Comment)
                            decision = FormatDecision.NoSpace;
                        else
                            decision = FormatDecision.NewLineIndent;
                        break;
                    }

                    case AiTokenTypes.Defrule:
                    case AiTokenTypes.Defconst:
                    case AiTokenTypes.Load:
                    case AiTokenTypes.LoadRandom:
                    {
                        // These tokens are always at line begin, so manipulate current indentation level variable accordingly
                        // This works around errors like forgotten closing braces
                        indentOffset = -currentIndentLevel;

                        // If there is no brace before the current token (skipping comments), an error must have occured, or the selected area starts with comments
                        if(lastNonCommentToken.Type != AiTokenTypes.OpeningBrace)
                            decision = FormatDecision.NewLineIndent;
                        else
                            decision = FormatDecision.NoSpace;
                        break;
                    }

                    case AiTokenTypes.BooleanFactName:
                    case AiTokenTypes.FactName:
                    case AiTokenTypes.ActionName:
                    {
                        // Attach the fact name to the leading brace, or add a space after a preceding UserPatch fact name
                        if(lastNonCommentToken.Type == AiTokenTypes.OpeningBrace)
                            decision = FormatDecision.NoSpace;
                        else if(lastNonCommentToken.Type == AiTokenTypes.FactName)
                            decision = FormatDecision.Space;
                        else
                            decision = FormatDecision.NewLineIndent;
                        break;
                    }

                    case AiTokenTypes.RuleArrow:
                    {
                        // Remove any indentation
                        indentOffset = -currentIndentLevel;
                        decision = FormatDecision.NewLineIndent;
                        break;
                    }

                    case AiTokenTypes.Number:
                    case AiTokenTypes.String:
                    case AiTokenTypes.Word:
                    {
                        // Use new line if there is a comment before, else insert just a space
                        // If we have some unrecognized fact or action with an opening brace in the front, then do not insert any space
                        if(lastToken.Type == AiTokenTypes.Comment)
                            decision = FormatDecision.NewLineIndent;
                        else if(lastToken.Type == AiTokenTypes.OpeningBrace)
                            decision = FormatDecision.NoSpace;
                        else
                            decision = FormatDecision.Space;
                        break;
                    }

                    case AiTokenTypes.Comment:
                    {
                        // Do not touch comments
                        decision = FormatDecision.Keep;
                        break;
                    }
                }

                // Use formatting decision to create a modification
                int lastTokenEndPosition = lastToken.StartPoint.Position + lastToken.Length;
                Span spanBetweenLastAndCurrentToken = new Span(lastTokenEndPosition, currentToken.StartPoint.Position - lastTokenEndPosition);
                switch(decision)
                {
                    case FormatDecision.Keep:
                    {
                        // Do not do anything
                        break;
                    }

                    case FormatDecision.NewLineIndent:
                    {
                        // Create new line and indentation between the last and the current token
                        currentIndentLevel += indentOffset;
                        if(currentIndentLevel < 0)
                            currentIndentLevel = 0;
                        modifications.Add(new SnapshotModification(spanBetweenLastAndCurrentToken, "\r\n" + new string(' ', currentIndentLevel)));
                        break;
                    }

                    case FormatDecision.NoSpace:
                    {
                        // Set no space
                        modifications.Add(new SnapshotModification(spanBetweenLastAndCurrentToken, ""));
                        break;
                    }

                    case FormatDecision.Space:
                    {
                        // Set exactly one space
                        modifications.Add(new SnapshotModification(spanBetweenLastAndCurrentToken, " "));
                        break;
                    }
                }

                // Remember current token
                if(currentToken.Type != AiTokenTypes.Comment)
                    lastNonCommentToken = currentToken;
                lastToken = currentToken;
            }

            // Apply changes
            using(var edit = _textView.TextBuffer.CreateEdit())
            {
                // Go through modifications and apply them
                foreach(SnapshotModification modification in modifications)
                    edit.Replace(modification.ReplacedSpan, modification.Replacement);

                // Execute edit
                edit.Apply();
            }
        }

        /// <summary>
        /// Helper class to store replacement information.
        /// </summary>
        private class SnapshotModification
        {
            public Span ReplacedSpan { get; }
            public string Replacement { get; }

            public SnapshotModification(Span replacedSpan, string replacement)
            {
                // Save arguments
                ReplacedSpan = replacedSpan;
                Replacement = replacement;
            }
        }

        /// <summary>
        /// The format decision for the space between two tokens.
        /// </summary>
        private enum FormatDecision
        {
            /// <summary>
            /// Leave no space in between.
            /// </summary>
            NoSpace,

            /// <summary>
            /// Insert one space.
            /// </summary>
            Space,

            /// <summary>
            /// Insert a new line and indent the next token.
            /// </summary>
            NewLineIndent,

            /// <summary>
            /// Keep the format (used for comments).
            /// </summary>
            Keep
        }
    }
}
