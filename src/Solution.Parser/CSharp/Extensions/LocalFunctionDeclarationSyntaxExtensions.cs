using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Argument.Check;
using Extensions.Pack;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class LocalFunctionDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifier(this LocalFunctionStatementSyntax methodDeclarationSyntax)
        {
            Throw.IfNull(methodDeclarationSyntax);

            foreach (var syntaxToken in methodDeclarationSyntax.Modifiers)
            {
                switch (syntaxToken.Text)
                {
                    case "public":
                        yield return Modifier.Public;
                        break;
                    case "internal":
                        yield return Modifier.Internal;
                        break;
                    case "protected":
                        yield return Modifier.Protected;
                        break;
                    case "private":
                        yield return Modifier.Private;
                        break;
                    case "static":
                        yield return Modifier.Static;
                        break;
                    case "const":
                        yield return Modifier.Const;
                        break;
                    case "abstract":
                        yield return Modifier.Abstract;
                        break;
                    case "partial":
                        yield return Modifier.Partial;
                        break;
                    case "required":
                        yield return Modifier.Required;
                        break;
                }
            }
        }

        internal static LocalFunction ToLocalFunction(this LocalFunctionStatementSyntax methodDeclarationSyntax, string filePath)
        {
            Throw.IfNull(methodDeclarationSyntax);

            var parameters = methodDeclarationSyntax.ParameterList.ToParameters(filePath);
            var returnType = methodDeclarationSyntax.ReturnType.ToString();
            var methodName = methodDeclarationSyntax.Identifier.ValueText;

            var methodValue = methodDeclarationSyntax.ToString();
            var methodBody = (methodDeclarationSyntax.Body?.ToString()).IsNull() ? string.Empty : methodDeclarationSyntax.Body.ToString();
            var statements = methodBody.IsEmpty() ? ImmutableList<string>.Empty : methodDeclarationSyntax.Body?.Statements.Select(s => s.ToString()).ToImmutableList() ?? ImmutableList<string>.Empty;
            var attributes = GetAttributes(methodDeclarationSyntax, filePath);
            var modifiers = methodDeclarationSyntax.ToModifier().ToImmutableList();
            var lineStatementsRaw = methodBody.Split(Environment.NewLine).ToImmutableList();
            var lineStatements = lineStatementsRaw.Take(new Range(1, lineStatementsRaw.Count - 1)).FilterNullOrWhitespace().ToImmutableList();
            var fullqualifiedName = BuildFullQualifiedName(methodDeclarationSyntax);

            return new LocalFunction(methodName,
                              parameters,
                              returnType,
                              methodValue,
                              methodBody,
                              statements,
                              attributes,
                              modifiers,
                              lineStatements,
                              methodDeclarationSyntax.SyntaxTree.ToString(),
                              fullqualifiedName,
                              filePath);
        }

        internal static string BuildFullQualifiedName(LocalFunctionStatementSyntax recordDeclarationSyntax)
        {
            var getFullQualifiedName = GetFullQualifiedName().Reverse();

            var flattenParentNameSpaceQualifiers = getFullQualifiedName.Flatten(".");
            var fullQualifiedName = $"{flattenParentNameSpaceQualifiers}.{recordDeclarationSyntax.Identifier.ValueText}";
            return fullQualifiedName;

            IEnumerable<string> GetFullQualifiedName()
            {
                var parent = recordDeclarationSyntax.Parent;
                while (parent.IsNotNull())
                {
                    switch (parent)
                    {
                        case null:
                            break;
                        case ClassDeclarationSyntax classDeclarationSyntax:
                            parent = parent.Parent;
                            yield return classDeclarationSyntax.Identifier.ValueText;
                            break;
                        case InterfaceDeclarationSyntax interfaceDeclarationSyntax:
                            parent = parent.Parent;
                            yield return interfaceDeclarationSyntax.Identifier.ValueText;
                            break;
                        case RecordDeclarationSyntax recordDeclarationSyntax:
                            parent = parent.Parent;
                            yield return recordDeclarationSyntax.Identifier.ValueText;
                            break;
                        case NamespaceDeclarationSyntax namespaceDeclarationSyntax:
                            parent = null;
                            yield return namespaceDeclarationSyntax.ToNamespace().Name;
                            break;
                        default:
                            parent = null;
                            yield return string.Empty;
                            break;
                    }
                }
            }
        }

        internal static ImmutableList<LocalFunction> ToLocalFunctions(this ImmutableList<LocalFunctionStatementSyntax> methodDeclarationSyntaxes,
                                                         string filePath)
        {
            Throw.IfNull(methodDeclarationSyntaxes);

            return methodDeclarationSyntaxes.Select(m => m.ToLocalFunction(filePath)).ToImmutableList();
        }

        private static ImmutableList<Attribute> GetAttributes(LocalFunctionStatementSyntax methodDeclarationSyntax,
                                                              string filePath)
        {
            return (from attrList in methodDeclarationSyntax.AttributeLists
                    from attr in attrList.Attributes
                    select new Attribute(attr.Name.ToString(), attr.ArgumentList?.Arguments.Select(arg => arg.ToString()).ToImmutableList() ?? ImmutableList<string>.Empty, attr.Parent?.ToString() ?? string.Empty, filePath)).ToImmutableList();
        }
    }
}
