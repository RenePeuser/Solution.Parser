using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SolutionParser.CSharp
{
    internal static class EventFieldDeclarationSyntaxExtensions
    {
        internal static IEnumerable<EventField> ToEventFields(
            this IEnumerable<EventFieldDeclarationSyntax> eventDeclarationSyntaxes)
        {
            return eventDeclarationSyntaxes.Select(ToEventField);
        }

        internal static EventField ToEventField(this EventFieldDeclarationSyntax eventDeclarationSyntax)
        {
            Throw.IfNull(() => eventDeclarationSyntax);

            return new EventField(eventDeclarationSyntax.Declaration.Type.ToString(),
                eventDeclarationSyntax.Declaration.Variables.First().Identifier.ValueText);
        }
    }
}
