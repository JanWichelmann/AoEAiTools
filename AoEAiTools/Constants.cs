using AoEAiTools.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools
{
	/// <summary>
	/// Contains constants used in various places.
	/// </summary>
	internal static class Constants
	{
		/// <summary>
		/// The allowed keywords.
		/// </summary>
		internal static string[] AiKeywords = new string[] { "defrule", "defconst", "load", "load-random", "or", "and", "xor", "nor", "nand", "xnor", "not" };

		/// <summary>
		/// The allowed delimiters.
		/// </summary>
		internal static char[] AiDelimiters = new char[] { '(', ')' };

        /// <summary>
        /// The allowed operators.
        /// </summary>
        internal static string[] AiOperators = new string[] { "less-than", "less-or-equal", "greater-than", "greater-or-equal", "equal", "not-equal", "<", "<=", ">", ">=", "==", "!=" };

        /// <summary>
        /// The allowed commands.
        /// </summary>
        internal static string[] AiCommands = Resources.Commands.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// The allowed identifiers.
        /// </summary>
        internal static string[] AiIdentifiers = Resources.Identifiers.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
}
