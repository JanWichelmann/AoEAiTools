using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.LanguageParsing
{
    /// <summary>
    /// Represents one AI token.
    /// </summary>
    internal class AiToken
    {
        /// <summary>
        /// The type of this token.
        /// </summary>
        public AiTokenTypes Type { get; }

        /// <summary>
        /// The string value of this token.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// The starting point of this token.
        /// </summary>
        public SnapshotPoint StartPoint { get; }

        /// <summary>
        /// The length of this token.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Creates a new AI token.
        /// </summary>
        /// <param name="type">The type of this token.</param>
        /// <param name="content">The string value of this token.</param>
        /// <param name="startPoint">The starting point of this token.</param>
        /// <param name="length">The length of this token.</param>
        public AiToken(AiTokenTypes type, string content, SnapshotPoint startPoint, int length)
        {
            // Store parameters
            Type = type;
            Content = content;
            StartPoint = startPoint;
            Length = length;
        }
    }
}
