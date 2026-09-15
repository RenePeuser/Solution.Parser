using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class FieldDeclarationSyntaxExtensions
    {
        /// <summary>
        /// One <see cref="Field"/> per declared variable, so <c>private int _a, _b;</c> reports both.
        /// </summary>
        internal static ImmutableList<Field> ToFields(this FieldDeclarationSyntax fieldDeclarationSyntax, string filePath)
        {
            Throw.IfNull(fieldDeclarationSyntax);

            var declaration = fieldDeclarationSyntax.Declaration;
            var type = declaration.Type.ToString();
            var modifiers = fieldDeclarationSyntax.Modifiers.ToModifiers();
            var accessibility = fieldDeclarationSyntax.Modifiers.ToAccessibility(fieldDeclarationSyntax);
            var attributes = fieldDeclarationSyntax.AttributeLists.ToAttributes(filePath);
            var documentation = fieldDeclarationSyntax.ToDocumentation();
            var isMultiVariable = declaration.Variables.Count > 1;

            return declaration.Variables.Select(variable => new Field
                              {
                                  Name = variable.Identifier.Text,
                                  FullQualifiedName = fieldDeclarationSyntax.BuildFullQualifiedName(variable.Identifier.Text),
                                  Type = type,
                                  Modifiers = modifiers,
                                  Accessibility = accessibility,
                                  Attributes = attributes,
                                  Documentation = documentation,
                                  Initializer = variable.Initializer?.ToInitializer(),
                                  IsNullable = declaration.Type is NullableTypeSyntax,
                                  IsPartOfMultiVariableDeclaration = isMultiVariable,
                                  SyntaxTree = fieldDeclarationSyntax.ToString(),
                                  FilePath = filePath,
                                  Location = variable.ToCodeLocation(filePath)
                              })
                              .ToImmutableList();
        }

        internal static ImmutableList<Field> ToFields(this ImmutableList<FieldDeclarationSyntax> fieldDeclarationSyntaxes, string filePath)
        {
            return fieldDeclarationSyntaxes.SelectMany(f => f.ToFields(filePath)).ToImmutableList();
        }
    }
}
