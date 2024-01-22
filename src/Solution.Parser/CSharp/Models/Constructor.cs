using System.Collections.Immutable;

namespace Solution.Parser.CSharp
{
    public record Constructor(IImmutableList<Parameter> Parameters, IImmutableList<string> Arguments, IImmutableList<Modifier> Modifiers);
    
    public record PrimaryConstructor(IImmutableList<Parameter> Parameters, IImmutableList<string> Arguments, IImmutableList<Modifier> Modifiers);
}
