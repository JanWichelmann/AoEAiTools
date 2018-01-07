using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.LanguageParsing
{
    /// <summary>
    /// Provides functions for parsing (partial) AI documents.
    /// </summary>
    internal class AiParser
    {
        /// <summary>
        /// Creates a new AI parser.
        /// </summary>
        public AiParser() { }

        /// <summary>
        /// Returns the tokens before or after the given position, including all that are in the same line as the starting point.
        /// This method does not do any context checking, it just scans for known keywords.
        /// </summary>
        /// <param name="startingPoint">The position where the parsing shall start.</param>
        /// <param name="backwards">Optional. Determines whether the token search shall be performed backwards beginning with the given starting point.</param>
        /// <returns></returns>
        public IEnumerable<AiToken> GetTokens(SnapshotPoint startingPoint, bool backwards = false)
        {
            // Go backwards through lines and return tokens successively
            ITextSnapshotLine currentLine = startingPoint.GetContainingLine();
            string currentLineString = currentLine.GetText();
            int currentLineIndex = 0;
            List<AiToken> lineTokens = new List<AiToken>();
            int lastLineNumber = (backwards ? 0 : currentLine.Snapshot.LineCount - 1);
            while(true)
            {
                // Read one line at once, independent of parsing direction
                lineTokens.Clear();
                while(currentLineIndex < currentLine.Length)
                {
                    // Handle character types
                    char currentLineChar = currentLineString[currentLineIndex];
                    if(currentLineChar == '(')
                        lineTokens.Add(new AiToken(AiTokenTypes.OpeningBrace, "(", currentLine.Start + currentLineIndex, 1));
                    else if(currentLineChar == ')')
                        lineTokens.Add(new AiToken(AiTokenTypes.ClosingBrace, ")", currentLine.Start + currentLineIndex, 1));
                    else if(currentLineChar == '"')
                    {
                        // Read whole string
                        int nextQuoteIndex = currentLineString.IndexOf('"', currentLineIndex + 1);
                        if(nextQuoteIndex > currentLineIndex)
                        {
                            // Found the closing quotation mark, copy string and update current index
                            int stringLength = nextQuoteIndex - currentLineIndex + 1;
                            lineTokens.Add(new AiToken(AiTokenTypes.String, currentLineString.Substring(currentLineIndex, stringLength), currentLine.Start + currentLineIndex, stringLength));
                            currentLineIndex = nextQuoteIndex;
                        }
                        else
                        {
                            // The line ends with an unclosed string, copy remainder of the line
                            string remainingLineString = currentLineString.Substring(currentLineIndex);
                            lineTokens.Add(new AiToken(AiTokenTypes.String, remainingLineString, currentLine.Start + currentLineIndex, remainingLineString.Length));
                            break;
                        }
                    }
                    else if(currentLineChar == ';')
                    {
                        // Copy remainder of the line
                        string remainingLineString = currentLineString.Substring(currentLineIndex);
                        lineTokens.Add(new AiToken(AiTokenTypes.Comment, remainingLineString, currentLine.Start + currentLineIndex, remainingLineString.Length));
                        break;
                    }
                    else if(currentLineChar == '=')
                    {
                        // This might be the rule arrow, the equality testing operator or a syntax error => inspect next char
                        if(currentLineIndex == currentLine.Length - 1)
                            lineTokens.Add(new AiToken(AiTokenTypes.Word, "=", currentLine.Start + currentLineIndex, 1));
                        else if(currentLineString[currentLineIndex + 1] == '>') 
                            lineTokens.Add(new AiToken(AiTokenTypes.RuleArrow, "=>", currentLine.Start + currentLineIndex++, 2));
                        else if(currentLineString[currentLineIndex + 1] == '=')
                            lineTokens.Add(new AiToken(AiTokenTypes.Word, "==", currentLine.Start + currentLineIndex++, 2));
                        else // Invalid, just define this char as an invalid word and proceed scanning
                            lineTokens.Add(new AiToken(AiTokenTypes.Word, "=", currentLine.Start + currentLineIndex, 1));
                    }
                    else if(currentLineChar == '-' || char.IsDigit(currentLineChar))
                    {
                        // Read number
                        int numberLength = 1;
                        while(currentLineIndex + numberLength < currentLine.Length)
                        {
                            // Test char
                            if(!char.IsDigit(currentLineString[currentLineIndex + numberLength]))
                                break;
                            ++numberLength;
                        }
                        lineTokens.Add(new AiToken(AiTokenTypes.Number, currentLineString.Substring(currentLineIndex, numberLength), currentLine.Start + currentLineIndex, numberLength));
                        currentLineIndex += numberLength - 1;
                    }
                    else if(!char.IsWhiteSpace(currentLineChar))
                    {
                        // Read word
                        int wordLength = 1;
                        while(currentLineIndex + wordLength < currentLine.Length)
                        {
                            // Test char
                            char c = currentLineString[currentLineIndex + wordLength];
                            if(!char.IsLetterOrDigit(c) && c != '-' && c != '=')
                                break;
                            ++wordLength;
                        }
                        string word = currentLineString.Substring(currentLineIndex, wordLength);

                        // Produce token depending on word recognition
                        AiTokenTypes wordType = AiTokenTypes.Word;
                        if(word == "defrule")
                            wordType = AiTokenTypes.Defrule;
                        else if(word == "defconst")
                            wordType = AiTokenTypes.Defconst;
                        else if(word == "load")
                            wordType = AiTokenTypes.Load;
                        else if(word == "load-random")
                            wordType = AiTokenTypes.LoadRandom;
                        else if(Constants.AiRuleBooleanFacts.Contains(word))
                            wordType = AiTokenTypes.BooleanFactName;
                        else if(Constants.AiRuleFacts.ContainsKey(word))
                            wordType = AiTokenTypes.FactName;
                        else if(Constants.AiRuleActions.ContainsKey(word))
                            wordType = AiTokenTypes.ActionName;
                        lineTokens.Add(new AiToken(wordType, word, currentLine.Start + currentLineIndex, wordLength));
                        currentLineIndex += wordLength - 1;
                    }

                    // Next character
                    ++currentLineIndex;
                }

                // Return the line tokens in the order determined by the parsing direction
                if(backwards)
                    for(int i = lineTokens.Count - 1; i >= 0; --i)
                        yield return lineTokens[i];
                else
                    foreach(AiToken lineToken in lineTokens)
                        yield return lineToken;

                // Finished?
                if(currentLine.LineNumber == lastLineNumber)
                    yield break;

                // Next line
                if(backwards)
                    currentLine = currentLine.Snapshot.GetLineFromLineNumber(currentLine.LineNumber - 1);
                else
                    currentLine = currentLine.Snapshot.GetLineFromLineNumber(currentLine.LineNumber + 1);
                currentLineString = currentLine.GetText();
                currentLineIndex = 0;
            }
        }
    }
}
