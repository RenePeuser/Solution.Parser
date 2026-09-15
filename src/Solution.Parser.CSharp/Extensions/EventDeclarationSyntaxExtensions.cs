using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EventDeclarationSyntaxExtensions
    {
        internal static Event ToEvent(this EventDeclarationSyntax eventDeclarationSyntax, string filePath)
        {
            Throw.IfNull(eventDeclarationSyntax);

            var name = eventDeclarationSyntax.Identifier.ValueText;
            var accessors = eventDeclarationSyntax.AccessorList?.Accessors ?? default;

            return new Event
            {
                Name = name,
                FullQualifiedName = eventDeclarationSyntax.BuildFullQualifiedName(name),
                Type = eventDeclarationSyntax.Type.ToString(),
                Modifiers = eventDeclarationSyntax.Modifiers.ToModifiers(),
                Accessibility = eventDeclarationSyntax.Modifiers.ToAccessibility(eventDeclarationSyntax),
                Attributes = eventDeclarationSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = eventDeclarationSyntax.ToDocumentation(),
                ExplicitInterfaceSpecifier = eventDeclarationSyntax.ExplicitInterfaceSpecifier?.Name.ToString(),
                HasAdd = accessors.Any(a => a.IsKind(SyntaxKind.AddAccessorDeclaration)),
                HasRemove = accessors.Any(a => a.IsKind(SyntaxKind.RemoveAccessorDeclaration)),
                SyntaxTree = eventDeclarationSyntax.ToString(),
                FilePath = filePath,
                Location = eventDeclarationSyntax.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Event> ToEvents(this ImmutableList<EventDeclarationSyntax> eventDeclarationSyntaxes, string filePath)
        {
            return eventDeclarationSyntaxes.Select(item => item.ToEvent(filePath)).ToImmutableList();
        }
    }
}
