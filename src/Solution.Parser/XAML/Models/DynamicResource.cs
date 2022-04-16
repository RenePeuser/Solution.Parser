using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(ResourceKey) + "}")]
    public class DynamicResource : StaticResource
    {
        internal DynamicResource(string value) : base(value)
        {
        }
    }
}
