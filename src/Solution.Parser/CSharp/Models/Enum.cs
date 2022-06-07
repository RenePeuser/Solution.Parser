using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Enum : DeclarationWithModifiers
    {
        internal Enum(NameSpace nameSpace, string name, IImmutableList<Modifier> modifiers,
            IImmutableList<EnumField> enumFields) : base(name, modifiers)
        {
            NameSpace = nameSpace;
            EnumFields = enumFields;
            FullQualifiedName = nameSpace.Name + "." + name;
        }

        public string FullQualifiedName { get; }

        public NameSpace NameSpace { get; }

        public IImmutableList<EnumField> EnumFields { get; }
    }
}
