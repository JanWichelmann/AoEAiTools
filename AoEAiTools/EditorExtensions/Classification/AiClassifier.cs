using System;
using System.Collections.Generic;
using System.Linq;
using AoEAiTools.LanguageParsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace AoEAiTools.EditorExtensions.Classification
{
	/// <summary>
	/// The classifier.
	/// </summary>
	internal class AiClassifier : IClassifier
	{
#pragma warning disable 67
        /// <summary>
        /// Unused here, since the AI language does not support multi-line comments.
        /// </summary>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67

        /// <summary>
        /// The registry of classification types, containing the types and their respective formatting settings.
        /// </summary>
        private IClassificationTypeRegistryService _classificationTypeRegistry;

        /// <summary>
        /// An AI parser object for tokenizing text.
        /// </summary>
        private AiParser _aiParser;

		/// <summary>
		/// Initializes a new instance of the <see cref="AiClassifier"/> class.
		/// </summary>
		/// <param name="registry">Classification registry.</param>
		internal AiClassifier(ITextBuffer textBuffer, IClassificationTypeRegistryService classificationTypeRegistry)
		{
            // Save needed parameters
			_classificationTypeRegistry = classificationTypeRegistry;

            // Create parser object
            _aiParser = new AiParser();
		}

		/// <summary>
		/// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
		/// </summary>
		/// <param name="span">The span currently being classified.</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
            // Initialize result list
			List<ClassificationSpan> spans = new List<ClassificationSpan>();

            // Get tokens beginning with the start of the first line of the given span (to avoid starting within a word)
            foreach(AiToken token in _aiParser.GetTokens(span.Start.GetContainingLine().Start))
            {
                // Finished?
                if(token.StartPoint >= span.End)
                    break;

                // Classify depending on token type
                string classifierType = AiClassifierTypes.Default;
                switch(token.Type)
                {
                    case AiTokenTypes.Comment:
                        classifierType = AiClassifierTypes.Comment;
                        break;
                    case AiTokenTypes.Defrule:
                    case AiTokenTypes.BooleanFactName:
                    case AiTokenTypes.RuleArrow:
                    case AiTokenTypes.Defconst:
                    case AiTokenTypes.Load:
                    case AiTokenTypes.LoadRandom:
                        classifierType = AiClassifierTypes.Keyword;
                        break;
                    case AiTokenTypes.OpeningBrace:
                    case AiTokenTypes.ClosingBrace:
                        classifierType = AiClassifierTypes.Delimiter;
                        break;
                    case AiTokenTypes.String:
                        classifierType = AiClassifierTypes.String;
                        break;
                    case AiTokenTypes.Number:
                        classifierType = AiClassifierTypes.Number;
                        break;
                    case AiTokenTypes.FactName:
                        classifierType = AiClassifierTypes.RuleFactName;
                        break;
                    case AiTokenTypes.ActionName:
                        classifierType = AiClassifierTypes.RuleActionName;
                        break;
                    case AiTokenTypes.Word:
                        if(Constants.AiOperators.Contains(token.Content))
                            classifierType = AiClassifierTypes.Operator;
                        else if(Constants.AiIdentifiers.Contains(token.Content))
                            classifierType = AiClassifierTypes.Identifier;
                        else
                            classifierType = AiClassifierTypes.Default;
                        break;
                }
                spans.Add(new ClassificationSpan(new SnapshotSpan(token.StartPoint, token.Length), _classificationTypeRegistry.GetClassificationType(classifierType)));
            }

			/*// Get first and last line, use these to get the whole text in between (including line breaks)
			var startLine = span.Start.GetContainingLine();
			var endLine = (span.End - 1).GetContainingLine();
			var text = span.Snapshot.GetText(new SnapshotSpan(startLine.Start, endLine.End));

			// Step through text char by char
			int lastClassificationEndIndex = 0;
			for(int charIndex = 0; charIndex < text.Length; ++charIndex)
			{
				// Check current character
				if(text[charIndex] == ';')
				{
					// Move unrecognized content or whitespace before this position into default class
					if(charIndex > lastClassificationEndIndex)
						spans.Add(CreateClassificationSpan(span, lastClassificationEndIndex, charIndex - lastClassificationEndIndex, AiClassifierTypes.Default));

					// The remainder of the line is a comment
					var currentLine = span.Snapshot.GetLineFromPosition(startLine.Start.Position + charIndex);
					int remainingCharCount = currentLine.EndIncludingLineBreak.Position - (startLine.Start.Position + charIndex);
					spans.Add(CreateClassificationSpan(span, charIndex, remainingCharCount, AiClassifierTypes.Comment));
					charIndex += remainingCharCount;
					lastClassificationEndIndex = charIndex + 1;
				}
				else if(text[charIndex] == '\r' || text[charIndex] == '\n')
				{
					// Update indices to next line (maybe with another intermediate line break character)
					lastClassificationEndIndex = charIndex + 1;
				}
				else if(Constants.AiDelimiters.Any(c => c == text[charIndex]))
				{
					// Move unrecognized content or whitespace before this position into default class
					if(charIndex > lastClassificationEndIndex)
						spans.Add(CreateClassificationSpan(span, lastClassificationEndIndex, charIndex - lastClassificationEndIndex, AiClassifierTypes.Default));

					// The current char is a delimiter
					spans.Add(CreateClassificationSpan(span, charIndex, 1, AiClassifierTypes.Delimiter));
					lastClassificationEndIndex = charIndex + 1;
				}
				else if(text[charIndex] == '"')
				{
					// Move unrecognized content or whitespace before this position into default class
					if(charIndex > lastClassificationEndIndex)
						spans.Add(CreateClassificationSpan(span, lastClassificationEndIndex, charIndex - lastClassificationEndIndex, AiClassifierTypes.Default));

					// Search for string termination character
					int stringBeginIndex = charIndex;
					var currentLine = span.Snapshot.GetLineFromPosition(startLine.Start.Position + charIndex);
					while(startLine.Start.Position + charIndex < currentLine.End.Position - 1)
						if(text[++charIndex] == '"')
							break;

					// Set string class
					spans.Add(CreateClassificationSpan(span, stringBeginIndex, charIndex - stringBeginIndex + 1, AiClassifierTypes.String));
					lastClassificationEndIndex = charIndex + 1;
				}
				else if(char.IsDigit(text[charIndex]))
				{
					// Move unrecognized content or whitespace before this position into default class
					if(charIndex > lastClassificationEndIndex)
						spans.Add(CreateClassificationSpan(span, lastClassificationEndIndex, charIndex - lastClassificationEndIndex, AiClassifierTypes.Default));

					// Search for last number character
					int numberBeginIndex = charIndex;
					var currentLine = span.Snapshot.GetLineFromPosition(startLine.Start.Position + charIndex);
					while(startLine.Start.Position + charIndex < currentLine.End.Position - 1)
						if(!char.IsDigit(text[++charIndex]))
							break;

					// Ignore delimiters
					if(Constants.AiDelimiters.Any(c => c == text[charIndex]))
						--charIndex;

					// The last character must be a digit (occurs if a delimiter follows), whitespace or span end
					if(char.IsDigit(text[charIndex]) || char.IsWhiteSpace(text[charIndex]) || startLine.Start.Position + charIndex == currentLine.End.Position - 1)
					{
						// Set number class
						spans.Add(CreateClassificationSpan(span, numberBeginIndex, charIndex - numberBeginIndex + 1, AiClassifierTypes.Number));
					}
					else
					{
						// Use default class
						spans.Add(CreateClassificationSpan(span, numberBeginIndex, charIndex - numberBeginIndex + 1, AiClassifierTypes.Default));
					}
					lastClassificationEndIndex = charIndex + 1;
				}
                else if(text[charIndex] == '=' && charIndex < text.Length-1 && text[charIndex + 1] == '>')
                {
                    // Move unrecognized content or whitespace before this position into default class
                    if(charIndex > lastClassificationEndIndex)
                        spans.Add(CreateClassificationSpan(span, lastClassificationEndIndex, charIndex - lastClassificationEndIndex, AiClassifierTypes.Default));

                    // Set as keyword (must be handled separately from the other keywords, since this one might conflict with the operators)
                    spans.Add(CreateClassificationSpan(span, charIndex, 2, AiClassifierTypes.Keyword));
                    ++charIndex;
                    lastClassificationEndIndex = charIndex + 1;
                }
                else if(!char.IsWhiteSpace(text[charIndex]))
                {
                    // Move unrecognized content or whitespace before this position into default class
                    if(charIndex > lastClassificationEndIndex)
                        spans.Add(CreateClassificationSpan(span, lastClassificationEndIndex, charIndex - lastClassificationEndIndex, AiClassifierTypes.Default));

                    // Search for whitespace or delimiter
                    int wordBeginIndex = charIndex;
                    var currentLine = span.Snapshot.GetLineFromPosition(startLine.Start.Position + charIndex);
                    while(startLine.Start.Position + charIndex < currentLine.End.Position - 1)
                        if(char.IsWhiteSpace(text[++charIndex]) || Constants.AiDelimiters.Any(c => c == text[charIndex]))
                        {
                            // Ignore this char
                            --charIndex;
                            break;
                        }

                    // Classify this word
                    string word = text.Substring(wordBeginIndex, charIndex - wordBeginIndex + 1);
                    if(Constants.AiKeywords.Contains(word))
                        spans.Add(CreateClassificationSpan(span, wordBeginIndex, charIndex - wordBeginIndex + 1, AiClassifierTypes.Keyword));
                    else if(Constants.AiOperators.Contains(word))
                        spans.Add(CreateClassificationSpan(span, wordBeginIndex, charIndex - wordBeginIndex + 1, AiClassifierTypes.Operator));
                    else if(Constants.AiCommands.Contains(word))
                        spans.Add(CreateClassificationSpan(span, wordBeginIndex, charIndex - wordBeginIndex + 1, AiClassifierTypes.Command));
                    else if(Constants.AiIdentifiers.Contains(word))
                        spans.Add(CreateClassificationSpan(span, wordBeginIndex, charIndex - wordBeginIndex + 1, AiClassifierTypes.Identifier));
                    else
                    {
                        // Use default class
                        spans.Add(CreateClassificationSpan(span, wordBeginIndex, charIndex - wordBeginIndex + 1, AiClassifierTypes.Default));
                    }
                    lastClassificationEndIndex = charIndex + 1;
                }
			}

            // Classify remaining whitespace
			int remainingCharCountTillEnd = endLine.End.Position - (startLine.Start.Position + lastClassificationEndIndex);
			if(remainingCharCountTillEnd > 0)
				spans.Add(CreateClassificationSpan(span, lastClassificationEndIndex, remainingCharCountTillEnd, AiClassifierTypes.Default));*/

            // Return classication list
			return spans;
		}

		private ClassificationSpan CreateClassificationSpan(SnapshotSpan span, int position, int length, string classifierType)
			=> new ClassificationSpan(new SnapshotSpan(span.Snapshot, span.Start.GetContainingLine().Start + position, length), _classificationTypeRegistry.GetClassificationType(classifierType));
	}
}
