using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.EditorExtensions
{
    /// <summary>
    /// Defines and exports the content type for *.per AI files.
    /// </summary>
    internal class AiContentTypeDefinition
    {
        /// <summary>
        /// The content type name.
        /// </summary>
        public const string ContentType = "Ai";

        /// <summary>
        /// Exports the AI content type.
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(ContentType)]
        [BaseDefinition("code")]
        public ContentTypeDefinition AiContentType { get; set; }

        /// <summary>
        /// Exports the AI file extension.
        /// </summary>
        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(ContentType)]
        [FileExtension(".per")]
        public FileExtensionToContentTypeDefinition AiFileExtension { get; set; }
    }
}
