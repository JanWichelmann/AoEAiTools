using Microsoft.VisualStudio.Language.StandardClassification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.EditorExtensions.Classification
{
	/// <summary>
	/// Defines the classification type names.
	/// </summary>
	class AiClassifierTypes
	{
        internal const string Comment /**/= PredefinedClassificationTypeNames.Comment; /*/ = "AiComment";/**/
		internal const string Default /**/= PredefinedClassificationTypeNames.Other; /*/ = "AiDefault";/**/
        internal const string Keyword /**/= PredefinedClassificationTypeNames.Keyword; /*/ = "AiKeyword";/**/
        internal const string String /**/= PredefinedClassificationTypeNames.String; /*/ = "AiString";/**/
        internal const string Number /**/= PredefinedClassificationTypeNames.Number; /*/ = "AiNumber";/**/
        internal const string Operator /**/= PredefinedClassificationTypeNames.Operator; /*/ = "AiOperator";/**/
        internal const string Delimiter /**/= PredefinedClassificationTypeNames.Character; /*/ = "AiDelimiter";/**/
        internal const string Identifier /**/= PredefinedClassificationTypeNames.Identifier; /*/ = "AiIdentifier";/**/
        internal const string RuleBooleanFactName /**/= PredefinedClassificationTypeNames.Keyword; /*/ = "AiKeyword";/**/
        internal const string RuleFactName /**/= PredefinedClassificationTypeNames.Type; /*/ = "AiCommand";/**/
        internal const string RuleActionName /**/= PredefinedClassificationTypeNames.Type; /*/ = "AiCommand";/**/
    }
}
