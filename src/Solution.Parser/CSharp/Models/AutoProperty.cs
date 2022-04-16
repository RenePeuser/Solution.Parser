using System.Collections.Generic;

namespace Solution.Parser.CSharp
{
    public class AutoProperty : Property
    {
        internal AutoProperty(string type, string name, bool isReadOnly, IEnumerable<Modifier> modifiers) : base(
            type, name, isReadOnly, modifiers)
        {
        }
    }
}
