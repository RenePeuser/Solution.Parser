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
                    case "required":
                        yield return Modifier.Required;
                        break;
                }
            }
        }

        internal static Record ToRecord(this RecordDeclarationSyntax recordDeclarationSyntax, string filePath)
        {
            var modifiers = recordDeclarationSyntax.ToModifiers().ToImmutableList();
            var constructors = recordDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors(filePath);
            var properties = recordDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties(filePath);
            var methods = recordDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods(filePath);
            var nameSpace = recordDeclarationSyntax.SyntaxTree.GetNamespace();
            var attributesOfClass = recordDeclarationSyntax.AttributeLists.ToAttributes(filePath);
            var fields = recordDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields(filePath);
            var baseTypes = recordDeclarationSyntax.BaseList?.ToBaseTypes() ?? ImmutableList<BaseType>.Empty;
            var interfaces = recordDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces(filePath);
            var events = recordDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents(filePath);
            var eventFields = recordDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields(filePath);
            var nestedClasses = recordDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses(filePath);
            var nestedStructs = recordDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs(filePath);
            var nestedEnums = recordDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums(filePath);
            var nestedInterfaces = recordDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces(filePath);
            var name = recordDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = recordDeclarationSyntax.ToString();

            var fullQualifiedName = BuildFullQualifiedName(recordDeclarationSyntax);
            var parameters = recordDeclarationSyntax.ParameterList?.ToParameters(filePath) ?? ImmutableList<Parameter>.Empty;

            if (name.Contains("UpdateCapabilityRequest"))
            {

            }

            var result = new Record(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass, fields,
                interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, parameters, syntaxTree, fullQualifiedName, filePath);

            return result;
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


        internal static ImmutableList<Record> ToRecords(this ImmutableList<RecordDeclarationSyntax> recordDeclarationSyntaxes, string filePath)
        {
            return recordDeclarationSyntaxes.Select(item => item.ToRecord(filePath)).ToImmutableList();
        }
    }
}
