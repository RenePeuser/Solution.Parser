using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Extensions.Pack;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class InterfaceDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifiers(this InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            foreach (var syntaxToken in interfaceDeclarationSyntax.Modifiers)
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
                }
            }
        }

        internal static IImmutableList<Interface> ToInterfaces(this IImmutableList<InterfaceDeclarationSyntax> interfaceDeclarationSyntaxes,
                                                               string filePath)
        {
            return interfaceDeclarationSyntaxes.Select(item => item.ToInterface(filePath)).ToImmutableList();
        }

        internal static Interface ToInterface(this InterfaceDeclarationSyntax interfaceDeclarationSyntax, string filePath)
        {
            Throw.IfNull(interfaceDeclarationSyntax);

            var name = interfaceDeclarationSyntax.Identifier.ValueText;
            var modifiers = interfaceDeclarationSyntax.ToModifiers().ToImmutableList();
            var properties = interfaceDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties(filePath).ToImmutableList();
            var methods = interfaceDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods(filePath).ToImmutableList();
            var nameSpace = interfaceDeclarationSyntax.SyntaxTree.GetNamespace();
            var attributesOfClass = interfaceDeclarationSyntax.AttributeLists.ToAttributes(filePath).ToImmutableList();
            var baseTypes = interfaceDeclarationSyntax.BaseList != null
                ? interfaceDeclarationSyntax.BaseList.ToBaseTypes().ToImmutableList()
                : ImmutableList<BaseType>.Empty;
            var events = interfaceDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents(filePath).ToImmutableList();
            var eventFields = interfaceDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields(filePath)
                .ToImmutableList();
            var nestedClasses = interfaceDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses(filePath).ToImmutableList();
            var nestedStructs = interfaceDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs(filePath).ToImmutableList();
            var nestedEnums = interfaceDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums(filePath).ToImmutableList();
            var nestedInterfaces = interfaceDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces(filePath)
                .ToImmutableList();
            var fullQualifiedName = BuildFullQualifiedName(interfaceDeclarationSyntax);
            var syntaxTree = interfaceDeclarationSyntax.ToString();

            return new Interface(nameSpace,
                                 name,
                                 modifiers,
                                 properties,
                                 methods,
                                 attributesOfClass,
                                 baseTypes,
                                 events,
                                 eventFields,
                                 nestedClasses,
                                 nestedStructs,
                                 nestedEnums,
                                 nestedInterfaces,
                                 fullQualifiedName,
                                 syntaxTree,
                                 filePath);
        }

        internal static string BuildFullQualifiedName(InterfaceDeclarationSyntax recordDeclarationSyntax)
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
    }
}
