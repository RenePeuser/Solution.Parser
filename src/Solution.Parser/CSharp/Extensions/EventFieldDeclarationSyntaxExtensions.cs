using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EventFieldDeclarationSyntaxExtensions
    {
        internal static IImmutableList<EventField> ToEventFields(this IImmutableList<EventFieldDeclarationSyntax> eventDeclarationSyntaxes,
                                                                 string filePath)
        {
            return eventDeclarationSyntaxes.Select(item => item.ToEventField(filePath)).ToImmutableList();
        }

        internal static EventField ToEventField(this EventFieldDeclarationSyntax eventDeclarationSyntax, string filePath)
        {
            Throw.IfNull(eventDeclarationSyntax);

            return new EventField(eventDeclarationSyntax.Declaration.Type.ToString(),
                                  eventDeclarationSyntax.Declaration.Variables.First().Identifier.ValueText,
                                  eventDeclarationSyntax.SyntaxTree.ToString(),
                                  filePath);
        }
    }
}
