using System;
using System.Diagnostics;
using System.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Sln
{
    [DebuggerDisplay("{Name}")]
    internal sealed class SolutionFileName
    {
        public SolutionFileName(string value)
            : this(value, true)
        {
        }

        private SolutionFileName(string value, bool formatIsMandatory)
        {
            Throw.IfNullOrWhiteSpace(value);

            Name = value;

            HasExplicitFormat = SolutionFileFormat.All.Any(value.EndWith);

            if (!HasExplicitFormat && formatIsMandatory)
            {
                throw new ArgumentException($"The solution file '{value}' has to have a solution file extension '{SolutionFileFormat.SLN}' or '{SolutionFileFormat.SLNX}'. Use '{nameof(SolutionFileName)}.{nameof(WithAnySolutionFormat)}' to accept both formats.");
            }
        }

        /// <summary>The name as given, <c>MySolution.sln</c> or, for any format, <c>MySolution</c>.</summary>
        public string Name { get; }

        // False when the name was given without extension, which means every known solution format is accepted.
        public bool HasExplicitFormat { get; }

        // Solution names contain dots themselves ('Solution.Parser'), so a missing extension can not be detected reliably.
        // Therefore accepting both solution formats has to be requested explicitly.
        public static SolutionFileName WithAnySolutionFormat(string solutionName)
        {
            Throw.IfNullOrWhiteSpace(solutionName);

            return new SolutionFileName(solutionName, false);
        }
    }
}
