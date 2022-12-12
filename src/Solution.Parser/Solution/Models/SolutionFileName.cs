using System;
using System.Diagnostics;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Solution
{
    [DebuggerDisplay("{Value}")]
    public class SolutionFileName : ImmutableSemanticType<string>
    {
        private const string SOLUTION_FILE_EXTENSION = ".sln";

        public SolutionFileName(string value)
            : base(value)
        {
            Throw.IfNullOrWhiteSpace(value);

            if (!value.EndWith(SOLUTION_FILE_EXTENSION))
            {
                throw new ArgumentException("The solution file has to has solution file extension '.sln'");
            }
        }
    }
}
