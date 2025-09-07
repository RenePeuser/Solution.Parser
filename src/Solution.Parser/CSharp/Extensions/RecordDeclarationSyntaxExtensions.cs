using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Extensions.Pack;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class RecordDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifiers(this RecordDeclarationSyntax recordDeclarationSyntax)
        {
            foreach (var syntaxToken in recordDeclarationSyntax.Modifiers)
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

        internal static Record ToRecord(this RecordDeclarationSyntax recordDeclarationSyntax, string filePath)
        {
            var modifiers = recordDeclarationSyntax.ToModifiers().ToImmutableList();
            var constructors = recordDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors(filePath).ToImmutableList();
            var properties = recordDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties(filePath).ToImmutableList();
            var methods = recordDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods(filePath).ToImmutableList();
            var nameSpace = recordDeclarationSyntax.SyntaxTree.GetNamespace();
            var attributesOfClass = recordDeclarationSyntax.AttributeLists.ToAttributes(filePath).ToImmutableList();
            var fields = recordDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields(filePath).ToImmutableList();
            var baseTypes = recordDeclarationSyntax.BaseList?.ToBaseTypes().ToImmutableList() ?? ImmutableList<BaseType>.Empty;
            var interfaces = recordDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces(filePath).ToImmutableList();
            var events = recordDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents(filePath).ToImmutableList();
            var eventFields = recordDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields(filePath).ToImmutableList();
            var nestedClasses = recordDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses(filePath).ToImmutableList();
            var nestedStructs = recordDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs(filePath).ToImmutableList();
            var nestedEnums = recordDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums(filePath).ToImmutableList();
            var nestedInterfaces = recordDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces(filePath).ToImmutableList();
            var name = recordDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = recordDeclarationSyntax.ToString();

            var fullQualifiedName = BuildFullQualifiedName(recordDeclarationSyntax);
            var parameters = recordDeclarationSyntax.ParameterList?.ToParameters(filePath) ?? ImmutableList<Parameter>.Empty;

            return new Record(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass, fields,
                interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, parameters, syntaxTree, fullQualifiedName, filePath);
        }

        internal static string BuildFullQualifiedName(RecordDeclarationSyntax recordDeclarationSyntax)
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


        internal static IImmutableList<Record> ToRecords(this IImmutableList<RecordDeclarationSyntax> recordDeclarationSyntaxes, string filePath)
        {
            return recordDeclarationSyntaxes.Select(item => item.ToRecord(filePath)).ToImmutableList();
        }
    }
}
