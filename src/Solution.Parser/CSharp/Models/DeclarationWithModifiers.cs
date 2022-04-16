using System.Collections.Generic;
using System.Diagnostics;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class DeclarationWithModifiers : DeclarationBase
    {
        internal DeclarationWithModifiers(string name, IEnumerable<Modifier> modifiers) : base(name)
        {
            Modifiers = modifiers;
            Throw.IfNull(() => modifiers);
        }

        public IEnumerable<Modifier> Modifiers { get; }
    }
}
