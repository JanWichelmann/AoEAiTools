using AoEAiTools.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AoEAiTools.LanguageParsing
{
    /// <summary>
    /// Contains constants used in various places.
    /// </summary>
    internal static class Constants
    {
        // TODO remove unused ones

        /// <summary>
        /// The allowed keywords.
        /// </summary>
        internal static readonly string[] AiKeywords = new string[] { "defrule", "defconst", "load", "load-random", "or", "and", "xor", "nor", "nand", "xnor", "not" };

        /// <summary>
        /// The allowed delimiters.
        /// </summary>
        internal static readonly char[] AiDelimiters = new char[] { '(', ')' };

        /// <summary>
        /// The allowed operators.
        /// </summary>
        internal static readonly string[] AiOperators = new string[] { "less-than", "less-or-equal", "greater-than", "greater-or-equal", "equal", "not-equal", "<", "<=", ">", ">=", "==", "!=" };

        /// <summary>
        /// The allowed commands.
        /// </summary>
        internal static readonly IEnumerable<string> AiCommands;

        /// <summary>
        /// The allowed identifiers.
        /// </summary>
        internal static readonly IEnumerable<string> AiIdentifiers;

        /// <summary>
        /// The special boolean facts (and/or/...).
        /// </summary>
        internal static readonly HashSet<string> AiRuleBooleanFacts;

        /// <summary>
        /// The rule facts and their metadata (does not include boolean meta-facts like and/or/...).
        /// </summary>
        internal static readonly Dictionary<string, RuleFactDefinition> AiRuleFacts;

        /// <summary>
        /// The rule actions and their metadata.
        /// </summary>
        internal static readonly Dictionary<string, RuleActionDefinition> AiRuleActions;

        /// <summary>
        /// The command parameters and their metadata.
        /// </summary>
        internal static readonly Dictionary<string, CommandParameterDefinition> AiCommandParameters;

        /// <summary>
        /// Initializes some fields.
        /// </summary>
        static Constants()
        {
            // Populate boolean fact set
            AiRuleBooleanFacts = new HashSet<string>(new[] { "or", "and", "xor", "nor", "nand", "xnor", "not" });

            // Read facts, actions and parameters
            XDocument ruleDefinitionsXml = XDocument.Parse(Resources.RuleDefinitions);
            AiRuleFacts = ruleDefinitionsXml.Root
                .Element("facts")
                    .Elements().ToDictionary
                    (
                        factTag => factTag.Attribute("name").Value,
                        factTag => new RuleFactDefinition(factTag.Elements().Select(paramTag => paramTag.Attribute("type").Value))
                    );
            AiRuleActions = ruleDefinitionsXml.Root
                .Element("actions")
                    .Elements().ToDictionary
                    (
                        actionTag => actionTag.Attribute("name").Value,
                        actionTag => new RuleActionDefinition(actionTag.Elements().Select(paramTag => paramTag.Attribute("type").Value))
                    );
            AiCommandParameters = ruleDefinitionsXml.Root
                .Element("parameters")
                    .Elements().ToDictionary
                    (
                        paramTag => paramTag.Attribute("type").Value,
                        paramTag => new CommandParameterDefinition(paramTag.Value.Trim().Split(','))
                    );

            // Create lookup arrays for the classifier
            AiCommands = AiRuleFacts.Select(rf => rf.Key).Union(AiRuleActions.Select(ra => ra.Key));
            AiIdentifiers = AiCommandParameters.SelectMany(cp => cp.Value.PossibleValues);
        }

        /// <summary>
        /// Base class for a fact/action definition object with a parameter list.
        /// </summary>
        internal abstract class CommandDefinition
        {
            /// <summary>
            /// The parameters (in correct order).
            /// </summary>
            public IEnumerable<string> Parameters { get; }

            /// <summary>
            /// Creates a new parameter list object.
            /// </summary>
            /// <param name="parameters">The parameters (in correct order).</param>
            protected CommandDefinition(IEnumerable<string> parameters)
            {
                // Save arguments
                Parameters = parameters;
            }
        }

        /// <summary>
        /// Contains information about a fact.
        /// </summary>
        internal class RuleFactDefinition : CommandDefinition
        {
            /// <summary>
            /// The description of this fact, as shown in the info tooltip.
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// Creates a new fact information object.
            /// </summary>
            /// <param name="parameters">The fact's parameters (in correct order).</param>
            public RuleFactDefinition(IEnumerable<string> parameters)
                : base(parameters)
            {
                // Auto-generate description
                Description = "Parameters: " + string.Join("  ", parameters.Select(p => "<" + p + ">"));
            }
        }

        /// <summary>
        /// Contains information about an action.
        /// </summary>
        internal class RuleActionDefinition : CommandDefinition
        {
            /// <summary>
            /// The description of this action, as shown in the info tooltip.
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// Creates a new action information object.
            /// </summary>
            /// <param name="parameters">The action's parameters (in correct order).</param>
            public RuleActionDefinition(IEnumerable<string> parameters)
                : base(parameters)
            {
                // Auto-generate description
                Description = "Parameters: " + string.Join("  ", parameters.Select(p => "<" + p + ">"));
            }
        }

        /// <summary>
        /// Contains information about a parameter.
        /// </summary>
        internal class CommandParameterDefinition
        {
            /// <summary>
            /// The parameter's allowed values.
            /// </summary>
            public IEnumerable<string> PossibleValues { get; }

            /// <summary>
            /// Creates a new parameter information object.
            /// </summary>
            /// <param name="possibleValues">The parameter's allowed values.</param>
            public CommandParameterDefinition(IEnumerable<string> possibleValues)
            {
                // Save arguments
                PossibleValues = possibleValues;
            }
        }
    }
}
