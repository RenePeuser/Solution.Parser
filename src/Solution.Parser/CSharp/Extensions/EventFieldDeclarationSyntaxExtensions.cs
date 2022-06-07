using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EventFieldDeclarationSyntaxExtensions
    {
        internal static IImmutableList<EventField> ToEventFields(
            this IImmutableList<EventFieldDeclarationSyntax> eventDeclarationSyntaxes)
        {
            return eventDeclarationSyntaxes.Select(ToEventField).ToImmutableList();
        }

        internal static EventField ToEventField(this EventFieldDeclarationSyntax eventDeclarationSyntax)
        {
            Throw.IfNull(eventDeclarationSyntax);

            return new EventField(eventDeclarationSyntax.Declaration.Type.ToString(),
                eventDeclarationSyntax.Declaration.Variables.First().Identifier.ValueText);
        }
    }
}
