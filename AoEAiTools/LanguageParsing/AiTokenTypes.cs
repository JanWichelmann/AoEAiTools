using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.LanguageParsing
{
    /// <summary>
    /// The different possible types of tokens.
    /// </summary>
    internal enum AiTokenTypes
    {
        /// <summary>
        /// Represents a comment that starts with ";" and goes till the line end.
        /// </summary>
        Comment,

        /// <summary>
        /// Represents the rule start keyword "defrule".
        /// </summary>
        Defrule,

        /// <summary>
        /// Represents the rule arrow keyword "=>".
        /// </summary>
        RuleArrow,

        /// <summary>
        /// Represents constant keyword "defconst".
        /// </summary>
        Defconst,

        /// <summary>
        /// Represents the file load keyword "load".
        /// </summary>
        Load,

        /// <summary>
        /// Represents the randomized file load keyword "load-random".
        /// </summary>
        LoadRandom,

        /// <summary>
        /// Represents an opening brace "(".
        /// </summary>
        OpeningBrace,

        /// <summary>
        /// Represents a closing brace ")".
        /// </summary>
        ClosingBrace,

        /// <summary>
        /// Represents a string that starts with a quotation mark (") and goes till a closing quotation mark.
        /// </summary>
        String,

        /// <summary>
        /// Represents a number consisting of an optional sign "-" 
        /// </summary>
        Number,

        /// <summary>
        /// Represents the name of a boolean fact (and/or/...).
        /// </summary>
        BooleanFactName,

        /// <summary>
        /// Represents the name of a fact.
        /// </summary>
        FactName,

        /// <summary>
        /// Represents the name of an action.
        /// </summary>
        ActionName,

        /// <summary>
        /// Represents any other word.
        /// </summary>
        Word
    }
}
