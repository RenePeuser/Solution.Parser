using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(FullQualifiedName) + "}")]
    public class DataContext
    {
        internal DataContext(string fullQualifiedName)
        {
            FullQualifiedName = fullQualifiedName;
        }

        public string FullQualifiedName { get; }
    }
}
