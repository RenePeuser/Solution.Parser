using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
                }
            }
        }

        internal static Record ToRecord(this RecordDeclarationSyntax recordDeclarationSyntax)
        {
            var modifiers = recordDeclarationSyntax.ToModifiers().ToImmutableList();
            var constructors = recordDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors().ToImmutableList();
            var properties = recordDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties().ToImmutableList();
            var methods = recordDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods().ToImmutableList();
            var nameSpace = recordDeclarationSyntax.SyntaxTree.AllOfType<NamespaceDeclarationSyntax>()[0].ToNamespace();
            var attributesOfClass = recordDeclarationSyntax.AttributeLists.ToAttributes().ToImmutableList();
            var fields = recordDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields().ToImmutableList();
            var baseTypes = recordDeclarationSyntax.BaseList != null ? recordDeclarationSyntax.BaseList.ToBaseTypes().ToImmutableList() : ImmutableList<BaseType>.Empty;
            var interfaces = recordDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToImmutableList();
            var events = recordDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents().ToImmutableList();
            var eventFields = recordDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields().ToImmutableList();
            var nestedClasses = recordDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses().ToImmutableList();
            var nestedStructs = recordDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs().ToImmutableList();
            var nestedEnums = recordDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums().ToImmutableList();
            var nestedInterfaces = recordDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToImmutableList();
            var name = recordDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = recordDeclarationSyntax.ToString();
            var fullQualifiedName = $"{nameSpace.Name}.{name}";
            var parameters = recordDeclarationSyntax.ParameterList?.ToParameters() ?? ImmutableList<Parameter>.Empty;

            return new Record(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass, fields,
                interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, parameters, syntaxTree, fullQualifiedName);
        }

        internal static IImmutableList<Record> ToRecords(this IImmutableList<RecordDeclarationSyntax> recordDeclarationSyntaxes)
        {
            return recordDeclarationSyntaxes.Select(ToRecord).ToImmutableList();
        }
    }
}
