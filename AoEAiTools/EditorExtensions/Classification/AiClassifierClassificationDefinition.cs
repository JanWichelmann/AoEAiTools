using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace AoEAiTools.EditorExtensions.Classification
{
	/// <summary>
	/// Classification type definition export for AiClassifier
	/// </summary>
	internal static class AiClassifierClassificationDefinition
	{
		// This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(AiClassifierTypes.Comment)]
		private static ClassificationTypeDefinition AiCommentClassificationType;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(AiClassifierTypes.Default)]
		private static ClassificationTypeDefinition AiDefaultClassificationType;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(AiClassifierTypes.Keyword)]
		private static ClassificationTypeDefinition AiKeywordClassificationType;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(AiClassifierTypes.String)]
		private static ClassificationTypeDefinition AiStringClassificationType;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(AiClassifierTypes.Number)]
		private static ClassificationTypeDefinition AiNumberClassificationType;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(AiClassifierTypes.Operator)]
		private static ClassificationTypeDefinition AiOperatorClassificationType;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(AiClassifierTypes.Delimiter)]
		private static ClassificationTypeDefinition AiDelimiterClassificationType;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(AiClassifierTypes.Identifier)]
		private static ClassificationTypeDefinition AiIdentifierClassificationType;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(AiClassifierTypes.Command)]
        private static ClassificationTypeDefinition AiCommandClassificationType;

#pragma warning restore 169
    }
}
