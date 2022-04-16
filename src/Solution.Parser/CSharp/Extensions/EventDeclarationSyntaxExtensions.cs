using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EventDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Event> ToEvents(this IEnumerable<EventDeclarationSyntax> eventDeclarationSyntaxes)
        {
            return eventDeclarationSyntaxes.Select(ToEvent);
        }

        internal static Event ToEvent(this EventDeclarationSyntax eventDeclarationSyntax)
        {
            Throw.IfNull(() => eventDeclarationSyntax);

            return new Event(eventDeclarationSyntax.Type.ToString(), eventDeclarationSyntax.Identifier.ValueText);
        }
    }
}
