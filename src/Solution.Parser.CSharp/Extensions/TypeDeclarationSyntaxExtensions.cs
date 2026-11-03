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

                // An extension block is a TypeDeclarationSyntax, yet no type: see ToExtensionBlocks.
                ExtensionBlockDeclarationSyntax => null,
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
                BaseTypes = syntax.BaseList?.ToBaseTypes(filePath) ?? ImmutableList<BaseType>.Empty,
                Constructors = constructors,
                Properties = syntax.ToProperties(filePath),
                Methods = syntax.DirectMembers<MethodDeclarationSyntax>().ToMethods(filePath),
                Fields = syntax.DirectMembers<FieldDeclarationSyntax>().ToFields(filePath),
                Events = syntax.DirectMembers<EventDeclarationSyntax>().ToEvents(filePath),
                EventFields = syntax.DirectMembers<EventFieldDeclarationSyntax>().ToEventFields(filePath),
                Indexers = syntax.DirectMembers<IndexerDeclarationSyntax>().ToIndexers(filePath),
                Operators = syntax.ToOperators(filePath),
                Delegates = syntax.DirectMembers<DelegateDeclarationSyntax>().ToDelegates(filePath),
                Finalizers = syntax.DirectMembers<DestructorDeclarationSyntax>().ToFinalizers(filePath),
                NestedTypes = syntax.ToNestedTypes(filePath),
                ExtensionBlocks = syntax.ToExtensionBlocks(filePath),
                Parameters = parameters
            };
        }

        /// <summary>
        /// The declared properties, preceded by those the compiler writes for the parameters of a
        /// positional record, unless the record declares a property of that name itself.
        /// </summary>
        private static ImmutableList<Property> ToProperties(this TypeDeclarationSyntax syntax, string filePath)
        {
            var declared = syntax.DirectMembers<PropertyDeclarationSyntax>().ToProperties(filePath);

            if (syntax is not RecordDeclarationSyntax { ParameterList.Parameters.Count: > 0 } record)
            {
                return declared;
            }

            var declaredNames = declared.Select(p => p.Name).ToHashSet();
            var isMutable = record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) && !record.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);

            var positional = record.ParameterList.Parameters
                                   .Where(p => !declaredNames.Contains(p.Identifier.ValueText))
                                   .Select(p => p.ToPositionalProperty(isMutable, filePath));

            return positional.Concat(declared).ToImmutableList();
        }

        private static Property ToPositionalProperty(this ParameterSyntax parameter, bool isMutable, string filePath)
        {
            var name = parameter.Identifier.ValueText;
            var fullyQualifiedName = parameter.BuildFullyQualifiedName(name);
            var getter = parameter.ToSynthesizedAccessor(AccessorKind.Get, fullyQualifiedName, filePath);
            var setter = parameter.ToSynthesizedAccessor(isMutable ? AccessorKind.Set : AccessorKind.Init, fullyQualifiedName, filePath);

            return new Property
            {
                Name = name,
                FullyQualifiedName = fullyQualifiedName,
                Type = parameter.Type?.ToSourceCode() ?? string.Empty,
                TypeReference = parameter.Type.ToTypeReference(filePath),
                Accessibility = Accessibility.Public,

                // [property: JsonPropertyName("id")] goes to the property; an attribute without target stays on the parameter.
                Attributes = parameter.AttributeLists.ToAttributes(filePath).Where(a => a.Target == "property").ToImmutableList(),
                Accessors = isMutable ? new PropertyAccessors(getter, setter, null) : new PropertyAccessors(getter, null, setter),
                IsAutoProperty = true,
                IsPositional = true,
                IsNullable = parameter.Type is NullableTypeSyntax,
                SourceNode = parameter,
                FilePath = filePath,
                Location = parameter.ToCodeLocation(filePath)
            };
        }

        private static ImmutableList<ExtensionBlock> ToExtensionBlocks(this TypeDeclarationSyntax syntax, string filePath)
        {
            return syntax.DirectMembers<ExtensionBlockDeclarationSyntax>()
                         .Select(block =>
                         {
                             var receiver = block.ParameterList?.Parameters.FirstOrDefault()?.ToParameter(0, filePath) ?? new Parameter
                             {
                                 Name = string.Empty,
                                 FullyQualifiedName = string.Empty,
                                 Type = string.Empty,
                                 FilePath = filePath
                             };

                             return new ExtensionBlock
                             {
                                 Receiver = receiver,
                                 TypeParameters = block.TypeParameterList.ToTypeParameters(block.ConstraintClauses, filePath),
                                 Methods = block.DirectMembers<MethodDeclarationSyntax>()
                                                .ToMethods(filePath)
                                                .Select(m => m with { ExtensionReceiver = receiver, IsInExtensionBlock = true })
                                                .ToImmutableList(),
                                 Properties = block.DirectMembers<PropertyDeclarationSyntax>()
                                                   .ToProperties(filePath)
                                                   .Select(p => p with { ExtensionReceiver = receiver, IsInExtensionBlock = true })
                                                   .ToImmutableList(),
                                 Operators = block.ToOperators(filePath),
                                 SourceNode = block,
                                 FilePath = filePath,
                                 Location = block.ToCodeLocation(filePath)
                             };
                         })
                         .ToImmutableList();
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
            var fullQualifiedName = syntax.BuildFullyQualifiedName(name);
            var nameSpace = syntax.NamespaceOf();
            var syntaxTree = syntax.ToSourceCode();
            var location = syntax.ToCodeLocation(filePath);

            return syntax switch
            {
                RecordDeclarationSyntax record => new Record
                                                  {
                                                      Name = name,
                                                      FullyQualifiedName = fullQualifiedName,
                                                      Namespace = nameSpace,
                                                      IsRecordStruct = record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
                                                      SourceCode = syntaxTree,
                                                      FilePath = filePath,
                                                      Location = location
                                                  },
                StructDeclarationSyntax => new Struct
                                           {
                                               Name = name,
                                               FullyQualifiedName = fullQualifiedName,
                                               Namespace = nameSpace,
                                               SourceCode = syntaxTree,
                                               FilePath = filePath,
                                               Location = location
                                           },
                InterfaceDeclarationSyntax => new Interface
                                              {
                                                  Name = name,
                                                  FullyQualifiedName = fullQualifiedName,
                                                  Namespace = nameSpace,
                                                  SourceCode = syntaxTree,
                                                  FilePath = filePath,
                                                  Location = location
                                              },
                _ => new Class
                     {
                         Name = name,
                         FullyQualifiedName = fullQualifiedName,
                         Namespace = nameSpace,
                         SourceCode = syntaxTree,
                         FilePath = filePath,
                         Location = location
                     }
            };
        }
    }
}
