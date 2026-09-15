using System.Collections.Immutable;

namespace Solution.Parser.Sln
{
    public static class SolutionFileFormat
    {
        public const string SLN = ".sln";

        public const string SLNX = ".slnx";

        // Ordered by preference: the newer xml based format wins when both files exist side by side.
        public static ImmutableList<string> All { get; } = [SLNX, SLN];
    }
}
