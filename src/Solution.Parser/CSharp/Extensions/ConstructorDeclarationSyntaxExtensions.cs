using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ConstructorDeclarationSyntaxExtensions
    {
        internal static Constructor ToConstructor(this ConstructorDeclarationSyntax constructorDeclarationSyntax, string filePath)
        {
            Throw.IfNull(constructorDeclarationSyntax);

            var name = constructorDeclarationSyntax.Identifier.ValueText;
            var initializer = constructorDeclarationSyntax.Initializer;
            var body = constructorDeclarationSyntax.Body.ToMemberBody(constructorDeclarationSyntax.ExpressionBody, filePath);

            return new Constructor
            {
                Name = name,
                FullQualifiedName = constructorDeclarationSyntax.BuildFullQualifiedName(name),
                Parameters = constructorDeclarationSyntax.ParameterList.ToParameters(filePath),
                Arguments = initializer?.ArgumentList.Arguments.Select(a => a.ToString()).ToImmutableList() ?? ImmutableList<string>.Empty,
                InitializerKind = initializer.ToInitializerKind(),
                Modifiers = constructorDeclarationSyntax.Modifiers.ToModifiers(),
                Accessibility = constructorDeclarationSyntax.Modifiers.ToAccessibility(constructorDeclarationSyntax),
                Attributes = constructorDeclarationSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = constructorDeclarationSyntax.ToDocumentation(),
                Body = body.Text,
                Statements = body.Statements,
                LineStatements = body.LineStatements,
                LocalFunctions = body.LocalFunctions,
                IsExpressionBodied = body.IsExpressionBodied,
                IsIterator = body.IsIterator,
                IsPrimary = false,
                SyntaxTree = constructorDeclarationSyntax.ToString(),
                FilePath = filePath,
                Location = constructorDeclarationSyntax.ToCodeLocation(filePath)
            };
        }

        /// <summary>
        /// The primary constructor of a record, class or struct, reported next to the declared ones so
        /// that a rule over constructors does not miss it.
        /// </summary>
        internal static Constructor ToPrimaryConstructor(this TypeDeclarationSyntax typeDeclarationSyntax,
                                                         ImmutableList<Parameter> parameters,
                                                         string filePath)
        {
            var name = typeDeclarationSyntax.Identifier.ValueText;
            var parameterList = typeDeclarationSyntax.ParameterList;
            var locationNode = parameterList ?? (SyntaxNode)typeDeclarationSyntax;

            // The arguments a primary constructor passes on sit on the base list, as in ": Base(a, b)".
            var arguments = typeDeclarationSyntax.BaseList?.Types
                                                 .OfType<PrimaryConstructorBaseTypeSyntax>()
                                                 .SelectMany(b => b.ArgumentList.Arguments.Select(a => a.ToString()))
                                                 .ToImmutableList() ?? ImmutableList<string>.Empty;

            return new Constructor
            {
                Name = name,
                FullQualifiedName = typeDeclarationSyntax.BuildFullQualifiedName(name),
                Parameters = parameters,
                Arguments = arguments,
                InitializerKind = arguments.IsEmpty ? ConstructorInitializerKind.None : ConstructorInitializerKind.Base,
                Accessibility = typeDeclarationSyntax.Modifiers.ToAccessibility(typeDeclarationSyntax),
                IsPrimary = true,
                SyntaxTree = parameterList?.ToString() ?? string.Empty,
                FilePath = filePath,
                Location = locationNode.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Constructor> ToConstructors(this ImmutableList<ConstructorDeclarationSyntax> constructorDeclarationSyntaxes,
                                                                  string filePath)
        {
            Throw.IfNull(constructorDeclarationSyntaxes);

            return constructorDeclarationSyntaxes.Select(c => c.ToConstructor(filePath)).ToImmutableList();
        }

        private static ConstructorInitializerKind ToInitializerKind(this ConstructorInitializerSyntax? initializer)
        {
            if (initializer is null)
            {
                return ConstructorInitializerKind.None;
            }

            return initializer.IsKind(SyntaxKind.BaseConstructorInitializer)
                       ? ConstructorInitializerKind.Base
                       : ConstructorInitializerKind.This;
        }
    }
}
