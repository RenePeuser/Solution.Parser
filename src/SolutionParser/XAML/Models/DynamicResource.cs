using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(ResourceKey) + "}")]
    public class DynamicResource : StaticResource
    {
        internal DynamicResource(string value) : base(value)
        {
        }
    }
}
