using System;
using System.Collections.Generic;
using System.Linq;
using Argument.Check;

namespace Solution.Parser.Core
{
    /// <summary>
    /// The end of a code rule: the findings must be empty. Works with any test framework, since a failing
    /// rule throws and every runner reports a thrown exception as a failed test.
    /// </summary>
    /// <example>
    /// <code>
    /// var findings = from type in solution.ProductionTrees.AllTypes()
    ///                from property in type.Properties
    ///                where !property.IsReadOnly
    ///                select $"{property.Location}: {type.FullyQualifiedName}.{property.Name} is mutable";
    ///
    /// findings.ShouldBeEmpty("Properties must be immutable");
    /// </code>
    /// </example>
    public static class Findings
    {
        /// <summary>
        /// Throws a <see cref="CodeRuleViolationException"/> listing every finding on its own line, when
        /// there are any. A finding that starts with its <c>Location</c> renders as <c>path(line,column)</c>,
        /// which test runners and IDEs turn into a link.
        /// </summary>
        /// <param name="findings">What the rule found, each rendered with <c>ToString()</c>.</param>
        /// <param name="rule">What the rule demands, the headline of the failure.</param>
        public static void ShouldBeEmpty<T>(this IEnumerable<T> findings, string rule)
        {
            Throw.IfNull(findings);
            Throw.IfNullOrWhiteSpace(rule);

            var lines = findings.Select(f => f?.ToString() ?? string.Empty).ToList();
            if (lines.Count == 0)
            {
                return;
            }

            var message = $"{rule}. {lines.Count} finding{(lines.Count == 1 ? string.Empty : "s")}:{Environment.NewLine}"
                          + string.Join(Environment.NewLine, lines.Select(l => $"  {l}"));

            throw new CodeRuleViolationException(rule, lines, message);
        }
    }

    /// <summary>A code rule found something; <see cref="Findings"/> holds each finding as rendered.</summary>
    public sealed class CodeRuleViolationException : Exception
    {
        public CodeRuleViolationException()
        {
            Rule = string.Empty;
            Findings = [];
        }

        public CodeRuleViolationException(string message)
            : base(message)
        {
            Rule = string.Empty;
            Findings = [];
        }

        public CodeRuleViolationException(string message, Exception innerException)
            : base(message, innerException)
        {
            Rule = string.Empty;
            Findings = [];
        }

        internal CodeRuleViolationException(string rule, IReadOnlyList<string> findings, string message)
            : base(message)
        {
            Rule = rule;
            Findings = findings;
        }

        public string Rule { get; }

        public IReadOnlyList<string> Findings { get; }
    }
}
