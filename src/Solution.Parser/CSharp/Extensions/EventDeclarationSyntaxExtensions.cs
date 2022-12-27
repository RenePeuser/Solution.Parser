using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EventDeclarationSyntaxExtensions
    {
        internal static IImmutableList<Event> ToEvents(this IImmutableList<EventDeclarationSyntax> eventDeclarationSyntaxes)
        {
            return eventDeclarationSyntaxes.Select(ToEvent).ToImmutableList();
        }

        internal static Event ToEvent(this EventDeclarationSyntax eventDeclarationSyntax)
        {
            Throw.IfNull(eventDeclarationSyntax);

            return new Event(eventDeclarationSyntax.Type.ToString(),
                             eventDeclarationSyntax.Identifier.ValueText,
                             eventDeclarationSyntax.SyntaxTree.ToString());
        }
    }
}
