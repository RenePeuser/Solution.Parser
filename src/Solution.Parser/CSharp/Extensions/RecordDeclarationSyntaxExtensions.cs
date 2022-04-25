using System.Collections.Generic;
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
            var modifiers = recordDeclarationSyntax.ToModifiers().ToList();
            var constructors = recordDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors().ToList();
            var properties = recordDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties().ToList();
            var methods = recordDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods().ToList();
            var nameSpace = recordDeclarationSyntax.SyntaxTree.AllOfType<NamespaceDeclarationSyntax>().First().ToNamespace();
            var attributesOfClass = recordDeclarationSyntax.AttributeLists.ToAttributes().ToList();
            var fields = recordDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields().ToList();
            var baseTypes = recordDeclarationSyntax.BaseList != null ? recordDeclarationSyntax.BaseList.ToBaseTypes().ToList() : Enumerable.Empty<BaseType>();
            var interfaces = recordDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToList();
            var events = recordDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents().ToList();
            var eventFields = recordDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields().ToList();
            var nestedClasses = recordDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses().ToList();
            var nestedStructs = recordDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs().ToList();
            var nestedEnums = recordDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums().ToList();
            var nestedInterfaces = recordDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToList();
            var name = recordDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = recordDeclarationSyntax.ToString();

            return new Record(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass, fields,
                interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, syntaxTree);
        }

        internal static IEnumerable<Record> ToRecords(this IEnumerable<RecordDeclarationSyntax> recordDeclarationSyntaxes)
        {
            return recordDeclarationSyntaxes.Select(ToRecord);
        }
    }
}
