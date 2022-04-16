using System.Collections.Generic;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public class Enum : DeclarationWithModifiers
    {
        internal Enum(NameSpace nameSpace, string name, IEnumerable<Modifier> modifiers,
            IEnumerable<EnumField> enumFields) : base(name, modifiers)
        {
            NameSpace = nameSpace;
            EnumFields = enumFields;
            FullQualifiedName = nameSpace.Name + "." + name;
        }

        public string FullQualifiedName { get; }

        public NameSpace NameSpace { get; }

        public IEnumerable<EnumField> EnumFields { get; }
    }
}
