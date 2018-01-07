using AoEAiTools.LanguageParsing;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AoEAiTools.EditorExtensions.Completion
{
    /// <summary>
    /// Contains the main completion logic.
    /// </summary>
    internal class AiCompletionSource : ICompletionSource
    {
        /// <summary>
        /// The overall completion source provider (used to obtain other references).
        /// </summary>
        private readonly AiCompletionSourceProvider _sourceProvider;

        /// <summary>
        /// The current text buffer.
        /// </summary>
        private readonly ITextBuffer _textBuffer;

        /// <summary>
        /// Determines whether this object has been disposed.
        /// </summary>
        private bool _isDisposed = false;

        /// <summary>
        /// An AI parser object for tokenizing text.
        /// </summary>
        private AiParser _aiParser;

        /// <summary>
        /// Completion objects for the top level keywords (i.e. not within a rule).
        /// </summary>
        private readonly IEnumerable<Microsoft.VisualStudio.Language.Intellisense.Completion> _aiTopLevelKeywordCompletions;

        /// <summary>
        /// Completion objects for facts.
        /// </summary>
        private readonly IEnumerable<Microsoft.VisualStudio.Language.Intellisense.Completion> _aiFactCompletions;

        /// <summary>
        /// Completion objects for actions.
        /// </summary>
        private readonly IEnumerable<Microsoft.VisualStudio.Language.Intellisense.Completion> _aiActionCompletions;

        /// <summary>
        /// Completion objects for all fact/action parameter types.
        /// </summary>
        private readonly Dictionary<string, IEnumerable<Microsoft.VisualStudio.Language.Intellisense.Completion>> _aiCommandParameterCompletions;

        /// <summary>
        /// Creates a new completion source.
        /// </summary>
        /// <param name="sourceProvider">The overall completion source provider (used to obtain other references).</param>
        /// <param name="textBuffer">The current text buffer.</param>
        public AiCompletionSource(AiCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            // Store parameters
            _sourceProvider = sourceProvider;
            _textBuffer = textBuffer;

            // Create parser object
            _aiParser = new AiParser();

            // Initialize top level keyword completions
            ImageSource keywordCompletionIcon = _sourceProvider.GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic);
            _aiTopLevelKeywordCompletions = new[]
            {
                new Microsoft.VisualStudio.Language.Intellisense.Completion("defconst", "defconst", "Constant", keywordCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("defrule", "defrule", "Rule", keywordCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("load", "load", "Load file", keywordCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("load-random", "load-random", "Load file with chance", keywordCompletionIcon, null),
            }.OrderBy(c => c.DisplayText);

            // Initialize fact completions
            ImageSource keywordFactCompletionIcon = _sourceProvider.GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPublic);
            ImageSource factCompletionIcon = _sourceProvider.GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
            var keywordFactCompletions = new[]
            {
                new Microsoft.VisualStudio.Language.Intellisense.Completion("or", "or", "Boolean OR operation", keywordFactCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("and", "and", "Boolean AND operation", keywordFactCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("xor", "xor", "Boolean XOR operation", keywordFactCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("nor", "nor", "Boolean NOR operation", keywordFactCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("nand", "nand", "Boolean NAND operation", keywordFactCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("xnor", "xnor", "Boolean XNOR operation", keywordFactCompletionIcon, null),
                new Microsoft.VisualStudio.Language.Intellisense.Completion("not", "not", "Boolean NOT operation", keywordFactCompletionIcon, null),
            };
            _aiFactCompletions = Constants.AiRuleFacts.Select
            (
                rf => new Microsoft.VisualStudio.Language.Intellisense.Completion(rf.Key, rf.Key, rf.Value.Description, factCompletionIcon, null)
            ).Union(keywordFactCompletions).OrderBy(c => c.DisplayText);

            // Initialize action completions
            ImageSource actionCompletionIcon = _sourceProvider.GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
            _aiActionCompletions = Constants.AiRuleActions.Select
            (
                ra => new Microsoft.VisualStudio.Language.Intellisense.Completion(ra.Key, ra.Key, ra.Value.Description, actionCompletionIcon, null)
            ).OrderBy(c => c.DisplayText);

            // Initialize parameter completions
            ImageSource commandParameterCompletionIcon = _sourceProvider.GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPublic);
            _aiCommandParameterCompletions = Constants.AiCommandParameters.ToDictionary
            (
                cp => cp.Key,
                cp => cp.Value.PossibleValues.Select(v => new Microsoft.VisualStudio.Language.Intellisense.Completion(v, v, v, commandParameterCompletionIcon, null))
            );
        }

        /// <summary>
        /// Does a loose scan of the surrounding code to determine the completion context, and returns a list of meaningful completions.
        /// </summary>
        /// <param name="completionSession">The current completion session.</param>
        /// <param name="completionSets">The target list to store the generated completion sets into.</param>
        public void AugmentCompletionSession(ICompletionSession completionSession, IList<CompletionSet> completionSets)
        {
            // Run text parser, skip all tokens behind the current word
            SnapshotPoint cursorPoint = (completionSession.TextView.Caret.Position.BufferPosition) - 1;
            var tokens = _aiParser.GetTokens(cursorPoint, true).SkipWhile(t => t.StartPoint > cursorPoint);

            // Check whether we are in a comment or in a string; skip the current word
            AiToken firstToken = tokens.First();
            if(firstToken.Type == AiTokenTypes.Comment || firstToken.Type == AiTokenTypes.String)
                return;
            if(firstToken.Type != AiTokenTypes.OpeningBrace)
                tokens = tokens.Skip(1);

            // Go backwards and find "(" with known keyword or "=>"
            CurrentCompletionContext completionContext = CurrentCompletionContext.Unknown;
            int closingBraceCount = 0;
            List<AiToken> tokensTillFirstOpeningBrace = new List<AiToken>();
            Constants.CommandDefinition commandDefinition = null; // Used for storing the parameter type list of a fact/action.
            bool encounteredOpeningBrace = false;
            foreach(AiToken token in tokens)
            {
                // Check token type
                switch(token.Type)
                {
                    case AiTokenTypes.OpeningBrace:
                    {
                        // Found an opening brace, set flag to stop copying tokens
                        if(!encounteredOpeningBrace)
                            encounteredOpeningBrace = true;
                        else
                            --closingBraceCount;
                        break;
                    }

                    case AiTokenTypes.ClosingBrace:
                    {
                        // If a closing brace is encountered before an opening one, an error must have occured
                        if(encounteredOpeningBrace)
                            ++closingBraceCount;
                        else
                            completionContext = CurrentCompletionContext.Error;
                        break;
                    }

                    case AiTokenTypes.Defconst:
                    case AiTokenTypes.Load:
                    case AiTokenTypes.LoadRandom:
                    {
                        // These commands hint a top level context, but completion within one of them is not supported
                        if(encounteredOpeningBrace)
                            completionContext = CurrentCompletionContext.TopLevel;
                        else
                            completionContext = CurrentCompletionContext.NotSupported;
                        break;
                    }

                    case AiTokenTypes.Defrule:
                    case AiTokenTypes.BooleanFactName:
                    {
                        // If an opening brace was found, we have a fact
                        if(encounteredOpeningBrace)
                            completionContext = CurrentCompletionContext.RuleFacts;
                        else
                            completionContext = CurrentCompletionContext.Error; // "defrule" has no parameters
                        break;
                    }

                    case AiTokenTypes.RuleArrow:
                    {
                        // If an opening brace was found, we have an action
                        if(encounteredOpeningBrace && closingBraceCount == 0)
                            completionContext = CurrentCompletionContext.RuleActions;
                        else if(encounteredOpeningBrace && closingBraceCount == 1)
                            completionContext = CurrentCompletionContext.TopLevel;
                        else
                            completionContext = CurrentCompletionContext.Error; // "=>" has no parameters
                        break;
                    }

                    case AiTokenTypes.FactName:
                    {
                        // We probably have a fact/action with parameter list
                        if(!encounteredOpeningBrace)
                        {
                            // Get parameter list
                            commandDefinition = Constants.AiRuleFacts[token.Content];
                            completionContext = CurrentCompletionContext.Parameters;
                        }
                        // TODO error state if directly in front of a brace
                        break;
                    }

                    case AiTokenTypes.ActionName:
                    {
                        // We probably have a fact/action with parameter list
                        if(!encounteredOpeningBrace)
                        {
                            // Get parameter list
                            commandDefinition = Constants.AiRuleActions[token.Content];
                            completionContext = CurrentCompletionContext.Parameters;
                        }
                        // TODO error state if directly in front of a brace
                        break;
                    }

                    case AiTokenTypes.Word:
                    case AiTokenTypes.Number:
                    case AiTokenTypes.String:
                    {
                        // Put this token into the list to determine the current parameter position of a given fact or action
                        if(!encounteredOpeningBrace)
                            tokensTillFirstOpeningBrace.Add(token);
                        // TODO error state if directly in front of a brace
                        break;
                    }
                }

                // Stop if a context was deduced or an error occured
                if(completionContext != CurrentCompletionContext.Unknown)
                    break;
            }

            // Handle input at document start
            if(completionContext == CurrentCompletionContext.Unknown && encounteredOpeningBrace)
                completionContext = CurrentCompletionContext.TopLevel;

            // Show completion set depending on current context
            switch(completionContext)
            {
                case CurrentCompletionContext.TopLevel:
                    completionSets.Add(new CompletionSet("Commands", "Commands", GetTrackingSpan(firstToken.StartPoint, cursorPoint), _aiTopLevelKeywordCompletions, null));
                    break;

                case CurrentCompletionContext.RuleFacts:
                    completionSets.Add(new CompletionSet("Facts", "Facts", GetTrackingSpan(firstToken.StartPoint, cursorPoint), _aiFactCompletions, null));
                    break;

                case CurrentCompletionContext.RuleActions:
                    completionSets.Add(new CompletionSet("Actions", "Actions", GetTrackingSpan(firstToken.StartPoint, cursorPoint), _aiActionCompletions, null));
                    break;

                case CurrentCompletionContext.Parameters:
                {
                    // Compare scanned parameters with parameter list
                    tokensTillFirstOpeningBrace.Reverse();
                    if(tokensTillFirstOpeningBrace.Count > commandDefinition.Parameters.Count())
                        break;
                    int i = 0;
                    foreach(string parameterType in commandDefinition.Parameters)
                    {
                        // Reached current parameter?
                        if(i == tokensTillFirstOpeningBrace.Count)
                        {
                            // Show completion list, if there is one
                            if(Constants.AiCommandParameters.ContainsKey(parameterType))
                                completionSets.Add(new CompletionSet("Parameter", "Parameter values", GetTrackingSpan(firstToken.StartPoint, cursorPoint), _aiCommandParameterCompletions[parameterType], null));
                            break;
                        }
                        else
                        {
                            // Check type
                            AiToken token = tokensTillFirstOpeningBrace[i];
                            if(parameterType == "string" && token.Type != AiTokenTypes.String)
                                goto case CurrentCompletionContext.Error;
                            else if(parameterType == "value" && token.Type != AiTokenTypes.Number)
                                goto case CurrentCompletionContext.Error;
                            else if(Constants.AiCommandParameters.ContainsKey(parameterType) && !Constants.AiCommandParameters[parameterType].PossibleValues.Contains(token.Content))
                                goto case CurrentCompletionContext.Error;
                        }
                        ++i;
                    }
                    break;
                }

                case CurrentCompletionContext.Error:
                    // Do nothing
                    break;
            }
        }

        /// <summary>
        /// Returns the span of the word at the current cursor position.
        /// </summary>
        /// <param name="currentWordStart">The starting point of the current word.</param>
        /// <param name="currentCursorPosition">The current cursor position.</param>
        /// <returns></returns>
        ITrackingSpan GetTrackingSpan(SnapshotPoint currentWordStart, SnapshotPoint cursorPosition)
        {
            // Calculate tracking span for current word
            var span = new SnapshotSpan(currentWordStart, cursorPosition + 1);
            return cursorPosition.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
        }

        /// <summary>
        /// Frees any used resources.
        /// </summary>
        public void Dispose()
        {
            // TODO Why?
            if(!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }

        /// <summary>
        /// The possible contexts of a completion session.
        /// </summary>
        private enum CurrentCompletionContext
        {
            /// <summary>
            /// The context couldn't be deduced yet.
            /// </summary>
            Unknown,

            /// <summary>
            /// An error has occured during parsing.
            /// </summary>
            Error,

            /// <summary>
            /// The context was successfully detected, but completion in this context is currently not supported.
            /// </summary>
            NotSupported,

            /// <summary>
            /// Top level: Not within rules, constants or load commands.
            /// </summary>
            TopLevel,

            /// <summary>
            /// The fact block of a rule.
            /// </summary>
            RuleFacts,

            /// <summary>
            /// The action block of a rule.
            /// </summary>
            RuleActions,

            /// <summary>
            /// A fact/action parameter list.
            /// </summary>
            Parameters
        }
    }
}
