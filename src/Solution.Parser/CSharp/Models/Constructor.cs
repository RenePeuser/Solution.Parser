using System.Collections.Immutable;

namespace Solution.Parser.CSharp
{
    public record Constructor(ImmutableList<Parameter> Parameters, ImmutableList<string> Arguments, ImmutableList<Modifier> Modifiers);
    
    public record PrimaryConstructor(ImmutableList<Parameter> Parameters, ImmutableList<string> Arguments, ImmutableList<Modifier> Modifiers);
}
