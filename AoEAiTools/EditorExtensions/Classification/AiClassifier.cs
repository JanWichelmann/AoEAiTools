using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace AoEAiTools.EditorExtensions.Classification
{
	/// <summary>
	/// The classifier.
	/// </summary>
	internal class AiClassifier : IClassifier
	{
		/// <summary>
		/// An event that occurs when the classification of a span of text has changed.
		/// </summary>
		/// <remarks>
		/// This event gets raised if a non-text change would affect the classification in some way,
		/// for example typing /* would cause the classification to change in C# without directly
		/// affecting the span.
		/// </remarks>
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		private IClassificationTypeRegistryService _classificationTypeRegistry;
		private ITextBuffer _textBuffer;

		/// <summary>
		/// Initializes a new instance of the <see cref="AiClassifier"/> class.
		/// </summary>
		/// <param name="registry">Classification registry.</param>
		internal AiClassifier(ITextBuffer textBuffer, IClassificationTypeRegistryService classificationTypeRegistry)
		{
			_textBuffer = textBuffer;
			_classificationTypeRegistry = classificationTypeRegistry;
		}

		/// <summary>
		/// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
		/// </summary>
		/// <remarks>
		/// This method scans the given SnapshotSpan for potential matches for this classification.
		/// </remarks>
		/// <param name="span">The span currently being classified.</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			// Initialize result list
			List<ClassificationSpan> spans = new List<ClassificationSpan>();

			// Get first and last line, use these to get the whole text in between (including line breaks)
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
				spans.Add(CreateClassificationSpan(span, lastClassificationEndIndex, remainingCharCountTillEnd, AiClassifierTypes.Default));

            // Return classication list
			return spans;
		}

		private ClassificationSpan CreateClassificationSpan(SnapshotSpan span, int position, int length, string classifierType)
			=> new ClassificationSpan(new SnapshotSpan(span.Snapshot, span.Start.GetContainingLine().Start + position, length), _classificationTypeRegistry.GetClassificationType(classifierType));
	}
}
