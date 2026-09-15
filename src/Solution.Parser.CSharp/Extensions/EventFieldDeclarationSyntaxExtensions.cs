using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EventFieldDeclarationSyntaxExtensions
    {
        /// <summary>
        /// One <see cref="EventField"/> per declared variable, mirroring how fields are reported.
        /// </summary>
        internal static ImmutableList<EventField> ToEventFields(this EventFieldDeclarationSyntax eventFieldSyntax, string filePath)
        {
            Throw.IfNull(eventFieldSyntax);

            var declaration = eventFieldSyntax.Declaration;
            var type = declaration.Type.ToString();
            var modifiers = eventFieldSyntax.Modifiers.ToModifiers();
            var accessibility = eventFieldSyntax.Modifiers.ToAccessibility(eventFieldSyntax);
            var attributes = eventFieldSyntax.AttributeLists.ToAttributes(filePath);
            var documentation = eventFieldSyntax.ToDocumentation();
            var isMultiVariable = declaration.Variables.Count > 1;

            return declaration.Variables.Select(variable => new EventField
                              {
                                  Name = variable.Identifier.ValueText,
                                  FullQualifiedName = eventFieldSyntax.BuildFullQualifiedName(variable.Identifier.ValueText),
                                  Type = type,
                                  Modifiers = modifiers,
                                  Accessibility = accessibility,
                                  Attributes = attributes,
                                  Documentation = documentation,
                                  Initializer = variable.Initializer?.ToInitializer(),
                                  IsPartOfMultiVariableDeclaration = isMultiVariable,
                                  SyntaxTree = eventFieldSyntax.ToString(),
                                  FilePath = filePath,
                                  Location = variable.ToCodeLocation(filePath)
                              })
                              .ToImmutableList();
        }

        internal static ImmutableList<EventField> ToEventFields(this ImmutableList<EventFieldDeclarationSyntax> eventFieldSyntaxes,
                                                                string filePath)
        {
            return eventFieldSyntaxes.SelectMany(item => item.ToEventFields(filePath)).ToImmutableList();
        }
    }
}
