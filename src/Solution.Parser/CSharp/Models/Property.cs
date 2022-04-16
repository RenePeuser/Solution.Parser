using System.Collections.Generic;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Property : DeclarationWithModifiers
    {
        internal Property(string type, string name, bool isReadOnly, IEnumerable<Modifier> modifiers) : base(name,
            modifiers)
        {
            Type = type;
            IsReadOnly = isReadOnly;
        }

        public string Type { get; }

        public bool IsReadOnly { get; }
    }
}
