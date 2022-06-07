using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{ResourceKey}")]
    public class StaticResource : PropertyValue
    {
        internal StaticResource(string value) : base(value)
        {
            ResourceKey = Value.ToString();
        }

        public string ResourceKey { get; }
    }
}
