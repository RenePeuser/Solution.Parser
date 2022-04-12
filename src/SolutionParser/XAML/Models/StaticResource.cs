using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(ResourceKey) + "}")]
    public class StaticResource : PropertyValue
    {
        internal StaticResource(string value) : base(value)
        {
            ResourceKey = Value.ToString();
        }

        public string ResourceKey { get; }
    }
}
