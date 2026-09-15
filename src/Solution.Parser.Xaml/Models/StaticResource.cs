using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{ResourceKey}")]
    public record StaticResource : ResourceReference
    {
        internal StaticResource(string rawValue) : base(rawValue)
        {
        }
    }
}
