using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Builds every declared type from one shared walk over the members declared directly in it. A
    /// nested type is converted by recursing into it, so each member is reported by exactly one type.
    /// </summary>
    internal static class TypeDeclarationSyntaxExtensions
    {
        /// <summary>
        /// Converts a class, struct, record, record struct, interface or enum. Returns null for any
        /// other member, so callers can filter a mixed member list.
        /// </summary>
        internal static TypeDeclaration? ToTypeDeclarationOrDefault(this MemberDeclarationSyntax member, string filePath)
        {
            return member switch
            {
                EnumDeclarationSyntax enumDeclaration => enumDeclaration.ToEnum(filePath),
                TypeDeclarationSyntax typeDeclaration => typeDeclaration.ToTypeDeclaration(filePath),
                _ => null
            };
        }

        internal static TypeDeclaration ToTypeDeclaration(this TypeDeclarationSyntax syntax, string filePath)
        {
            Throw.IfNull(syntax);

            var name = syntax.Identifier.ValueText;
            var seed = syntax.ToSeed(name, filePath);

            var declaredConstructors = syntax.DirectMembers<ConstructorDeclarationSyntax>().ToConstructors(filePath);
            var primaryConstructorParameters = syntax.ParameterList.ToParameters(filePath);

            var constructors = syntax.ParameterList is null
                                   ? declaredConstructors
                                   : declaredConstructors.Insert(0, syntax.ToPrimaryConstructor(primaryConstructorParameters, filePath));

            // A primary constructor wins; otherwise the constructor taking the most arguments stands in.
            var parameters = syntax.ParameterList is not null
                                 ? primaryConstructorParameters
                                 : declaredConstructors.MaxBy(c => c.Parameters.Count)?.Parameters ?? ImmutableList<Parameter>.Empty;

            return seed with
            {
                Modifiers = syntax.Modifiers.ToModifiers(),
                Accessibility = syntax.Modifiers.ToAccessibility(syntax),
                Attributes = syntax.AttributeLists.ToAttributes(filePath),
                Documentation = syntax.ToDocumentation(),
                TypeParameters = syntax.TypeParameterList.ToTypeParameters(syntax.ConstraintClauses, filePath),
                BaseTypes = syntax.BaseList?.ToBaseTypes() ?? ImmutableList<BaseType>.Empty,
                Constructors = constructors,
                Properties = syntax.DirectMembers<PropertyDeclarationSyntax>().ToProperties(filePath),
                Methods = syntax.DirectMembers<MethodDeclarationSyntax>().ToMethods(filePath),
                Fields = syntax.DirectMembers<FieldDeclarationSyntax>().ToFields(filePath),
                Events = syntax.DirectMembers<EventDeclarationSyntax>().ToEvents(filePath),
                EventFields = syntax.DirectMembers<EventFieldDeclarationSyntax>().ToEventFields(filePath),
                Indexers = syntax.DirectMembers<IndexerDeclarationSyntax>().ToIndexers(filePath),
                Operators = syntax.ToOperators(filePath),
                Delegates = syntax.DirectMembers<DelegateDeclarationSyntax>().ToDelegates(filePath),
                Finalizers = syntax.DirectMembers<DestructorDeclarationSyntax>().ToFinalizers(filePath),
                NestedTypes = syntax.ToNestedTypes(filePath),
                Parameters = parameters
            };
        }

        /// <summary>The types declared directly in this type, each with its own members.</summary>
        private static ImmutableList<TypeDeclaration> ToNestedTypes(this TypeDeclarationSyntax syntax, string filePath)
        {
            return syntax.Members
                         .Select(member => member.ToTypeDeclarationOrDefault(filePath))
                         .Where(type => type is not null)
                         .Select(type => type!)
                         .ToImmutableList();
        }

        /// <summary>
        /// The concrete record for the declaration kind, carrying only what every type needs. The
        /// members are layered on with a <c>with</c> expression, which keeps the runtime type.
        /// </summary>
        private static TypeDeclaration ToSeed(this TypeDeclarationSyntax syntax, string name, string filePath)
        {
            var fullQualifiedName = syntax.BuildFullQualifiedName(name);
            var nameSpace = syntax.NamespaceOf();
            var syntaxTree = syntax.ToString();
            var location = syntax.ToCodeLocation(filePath);

            return syntax switch
            {
                RecordDeclarationSyntax record => new Record
                                                  {
                                                      Name = name,
                                                      FullQualifiedName = fullQualifiedName,
                                                      NameSpace = nameSpace,
                                                      IsRecordStruct = record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
                                                      SyntaxTree = syntaxTree,
                                                      FilePath = filePath,
                                                      Location = location
                                                  },
                StructDeclarationSyntax => new Struct
                                           {
                                               Name = name,
                                               FullQualifiedName = fullQualifiedName,
                                               NameSpace = nameSpace,
                                               SyntaxTree = syntaxTree,
                                               FilePath = filePath,
                                               Location = location
                                           },
                InterfaceDeclarationSyntax => new Interface
                                              {
                                                  Name = name,
                                                  FullQualifiedName = fullQualifiedName,
                                                  NameSpace = nameSpace,
                                                  SyntaxTree = syntaxTree,
                                                  FilePath = filePath,
                                                  Location = location
                                              },
                _ => new Class
                     {
                         Name = name,
                         FullQualifiedName = fullQualifiedName,
                         NameSpace = nameSpace,
                         SyntaxTree = syntaxTree,
                         FilePath = filePath,
                         Location = location
                     }
            };
        }
    }
}
