using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{FullQualifiedName}")]
    public class DataContext
    {
        internal DataContext(string fullQualifiedName)
        {
            FullQualifiedName = fullQualifiedName;
        }

        public string FullQualifiedName { get; }
    }
}
