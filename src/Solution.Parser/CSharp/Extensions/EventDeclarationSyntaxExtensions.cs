using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EventDeclarationSyntaxExtensions
    {
        internal static IImmutableList<Event> ToEvents(this IImmutableList<EventDeclarationSyntax> eventDeclarationSyntaxes, string filePath)
        {
            return eventDeclarationSyntaxes.Select(item => item.ToEvent(filePath)).ToImmutableList();
        }

        internal static Event ToEvent(this EventDeclarationSyntax eventDeclarationSyntax, string filePath)
        {
            Throw.IfNull(eventDeclarationSyntax);

            return new Event(eventDeclarationSyntax.Type.ToString(),
                             eventDeclarationSyntax.Identifier.ValueText,
                             eventDeclarationSyntax.SyntaxTree.ToString(),
                             filePath);
        }
    }
}
