using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Method(string Name,
                         IImmutableList<Parameter> Parameters,
                         string ReturnParameter,
                         string MethodValue,
                         string MethodBody,
                         IImmutableList<string> Statements,
                         IImmutableList<Attribute> Attributes,
                         IImmutableList<Modifier> Modifiers,
                         IImmutableList<string> LineStatements) : DeclarationWithModifiers(Name, Modifiers);
}
