using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.EditorExtensions.BraceMatching
{
    /// <summary>
    /// Responsible for highlighting matching braces.
    /// </summary>
    internal class AiBraceMatchingTagger : ITagger<TextMarkerTag>
    {
        /// <summary>
        /// The current text view.
        /// </summary>
        private readonly ITextView _textView;

        /// <summary>
        /// The current text view contents.
        /// </summary>
        private readonly ITextBuffer _sourceBuffer;

        /// <summary>
        /// The current cursor position.
        /// </summary>
        private SnapshotPoint? _currentCursorPoint;

        /// <summary>
        /// The character at the current cursor position.
        /// </summary>
        private char _currentCursorPointChar = ' ';

        /// <summary>
        /// The character before the current cursor position.
        /// </summary>
        private char _currentCursorPointPreviousChar = ' ';

        /// <summary>
        /// Occurs when the currently selected char changes.
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// Creates a new brace matching tagger.
        /// </summary>
        /// <param name="textView">The current text view.</param>
        /// <param name="sourceBuffer">The current text view contents.</param>
        internal AiBraceMatchingTagger(ITextView textView, ITextBuffer sourceBuffer)
        {
            // Save arguments
            _textView = textView;
            _sourceBuffer = sourceBuffer;

            // Assign event handlers to automatically highlight matching braces
            _textView.LayoutChanged += _textView_LayoutChanged;
            _textView.Caret.PositionChanged += Caret_PositionChanged;
        }

        /// <summary>
        /// Raises the TagsChanged event if there is a character at the current cursor position.
        /// </summary>
        /// <param name="cursorPosition">The current cursor position.</param>
        private void RaiseTagsChanged(CaretPosition cursorPosition)
        {
            // Check if there is currently a character at the cursor position
            SnapshotPoint? newCursorPoint = cursorPosition.Point.GetPoint(_sourceBuffer, cursorPosition.Affinity);
            if(newCursorPoint.HasValue)
            {
                // Check whether the adjacent character type changed in a relevant way, to avoid lots of unneccessary calls
                char currentChar = (newCursorPoint.Value.Position < newCursorPoint.Value.Snapshot.Length ? newCursorPoint.Value.GetChar() : ' ');
                char previousChar = (newCursorPoint.Value > 0 ? (newCursorPoint.Value - 1).GetChar() : ' ');
                if(_currentCursorPointChar == '(' || _currentCursorPointPreviousChar == ')' || currentChar == '(' || previousChar == ')')
                {
                    // Raise event
                    _currentCursorPointChar = currentChar;
                    _currentCursorPointPreviousChar = previousChar;
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0, _sourceBuffer.CurrentSnapshot.Length)));
                }
            }

            // Remember new cursor point
            _currentCursorPoint = newCursorPoint;
        }

        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            // Check for possibly changed tags
            RaiseTagsChanged(e.NewPosition);
        }

        private void _textView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // Check for possibly changed tags
            if(e.OldSnapshot != e.NewSnapshot)
                RaiseTagsChanged(_textView.Caret.Position);
        }

        /// <summary>
        /// Returns the tags that intersect the given spans.
        /// </summary>
        /// <param name="spans">The spans for which the tags shall be determined.</param>
        /// <returns></returns>
        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            // Check if there are any spans
            if(spans.Count == 0)
                yield break;

            // The current cursor position must be initialized
            if(!_currentCursorPoint.HasValue)
                yield break;

            // Copy the variable for modification
            SnapshotPoint currentCursorPoint = _currentCursorPoint.Value;

            // Make sure that the given spans correspond to the same text snapshot as the cursor point
            if(spans[0].Snapshot != currentCursorPoint.Snapshot)
                currentCursorPoint = currentCursorPoint.TranslateTo(spans[0].Snapshot, PointTrackingMode.Positive);

            // Get current and previous character (to be sure, do not use the cached ones)
            char currentChar = (currentCursorPoint.Position < currentCursorPoint.Snapshot.Length ? currentCursorPoint.GetChar() : ' ');
            char previousChar = (currentCursorPoint > 0 ? (currentCursorPoint - 1).GetChar() : ' ');

            // Act depending on opening or closing brace
            if(currentChar == '(')
            {
                // Find matching closing brace
                ITextSnapshotLine currentLine = currentCursorPoint.GetContainingLine();
                string currentLineString = currentLine.GetText();
                int maxLineNumber = Math.Min(currentCursorPoint.Snapshot.LineCount - 1, currentLine.LineNumber + _textView.TextViewLines.Count); // Stay within the visible view
                int currentLineIndex = currentCursorPoint - currentLine.Start + 1; // Ignore the current brace
                int currentBraceLevel = 0;
                bool finished = false;
                while(currentLine.LineNumber <= maxLineNumber)
                {
                    // Check line
                    while(currentLineIndex < currentLine.Length)
                    {
                        // Handle character types
                        char currentLineChar = currentLineString[currentLineIndex];
                        if(currentLineChar == '(')
                            ++currentBraceLevel;
                        else if(currentLineChar == ')')
                        {
                            // Found the matching closing brace?
                            if(currentBraceLevel > 0)
                                --currentBraceLevel;
                            else
                            {
                                // Found, highlight current and connected brace
                                yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentCursorPoint, 1), new TextMarkerTag("brace matching"));
                                yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentCursorPoint.Snapshot, currentLine.Start + currentLineIndex, 1), new TextMarkerTag("brace matching"));

                                // Exit loop
                                finished = true;
                                break;
                            }
                        }
                        else if(currentLineChar == '"')
                        {
                            // Skip whole string
                            while(currentLineIndex < currentLine.Length - 1 && currentLineString[currentLineIndex + 1] != '"')
                                ++currentLineIndex;
                            ++currentLineIndex;
                        }
                        else if(currentLineChar == ';')
                        {
                            // Skip the rest of this line
                            break;
                        }

                        // Next character
                        ++currentLineIndex;
                    }

                    // Finished?
                    if(finished || currentLine.LineNumber == maxLineNumber)
                        break;

                    // Next line
                    currentLine = currentLine.Snapshot.GetLineFromLineNumber(currentLine.LineNumber + 1);
                    currentLineString = currentLine.GetText();
                    currentLineIndex = 0;
                }
            }
            else if(previousChar == ')')
            {
                // Find matching opening brace
                ITextSnapshotLine currentLine = currentCursorPoint.GetContainingLine();
                string currentLineString = currentLine.GetText();
                int minLineNumber = Math.Max(0, currentLine.LineNumber - _textView.TextViewLines.Count); // Stay within the visible view
                int currentLineIndex = currentCursorPoint - currentLine.Start - 2; // Ignore the current brace
                int currentBraceLevel = 0;
                bool finished = false;
                while(currentLine.LineNumber >= minLineNumber)
                {
                    // First skip a possible comment at line end
                    bool inString = false;
                    for(int currentLineIndex2 = 0; currentLineIndex2 < currentLineString.Length; ++currentLineIndex2)
                        if(currentLineString[currentLineIndex2] == '"')
                            inString = !inString;
                        else if(currentLineString[currentLineIndex2] == ';' && !inString)
                        {
                            // Found the comment, start parsing before
                            currentLineIndex = Math.Min(currentLineIndex, currentLineIndex2 - 1);
                            break;
                        }

                    // Check line
                    while(currentLineIndex >= 0)
                    {
                        // Handle character types
                        char currentLineChar = currentLineString[currentLineIndex];
                        if(currentLineChar == '(')
                        {
                            // Found the matching opening brace?
                            if(currentBraceLevel > 0)
                                --currentBraceLevel;
                            else
                            {
                                // Found, highlight current and connected brace
                                // Calculating currentCursorPoint - 1 is safe, since we wouldn't have found a match if currentCursorPoint == 0
                                yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentCursorPoint - 1, 1), new TextMarkerTag("brace matching"));
                                yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentCursorPoint.Snapshot, currentLine.Start + currentLineIndex, 1), new TextMarkerTag("brace matching"));

                                // Exit loop
                                finished = true;
                                break;
                            }
                        }
                        else if(currentLineChar == ')')
                            ++currentBraceLevel;
                        else if(currentLineChar == '"')
                        {
                            // Skip whole string
                            while(currentLineIndex > 0 && currentLineString[currentLineIndex - 1] != '"')
                                --currentLineIndex;
                            --currentLineIndex;
                        }

                        // Next character
                        --currentLineIndex;
                    }

                    // Finished?
                    if(finished)
                        break;

                    // Next line
                    if(currentLine.LineNumber == minLineNumber)
                        break;
                    currentLine = currentLine.Snapshot.GetLineFromLineNumber(currentLine.LineNumber - 1);
                    currentLineString = currentLine.GetText();
                    currentLineIndex = currentLineString.Length - 1;
                }
            }
        }
    }
}
