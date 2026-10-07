using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynAccessibility = Microsoft.CodeAnalysis.Accessibility;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Turns a Roslyn symbol into the records a rule already knows, so a resolved method reads like any
    /// other <see cref="Method"/> whether it is declared in the code base or in a package.
    /// </summary>
    internal static class SymbolToModel
    {
        private static readonly ConditionalWeakTable<Method, SymbolOrigin> Origins = new();

        // Types as people write them, "List<Person>" rather than "System.Collections.Generic.List<My.Person>", like the source records.
        private static readonly SymbolDisplayFormat TypeFormat =
            new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                                      | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                                      | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        private static readonly SymbolDisplayFormat QualifiedFormat =
            SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
                               .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// The origin recorded when the method was resolved. A method that never went through a
        /// resolution but points at source is source; anything else is unknown.
        /// </summary>
        internal static SymbolOrigin OriginOf(Method method)
        {
            if (Origins.TryGetValue(method, out var origin))
            {
                return origin;
            }

            return method.Location == CodeLocation.None ? SymbolOrigin.Unknown : SymbolOrigin.Source;
        }

        internal static Method ToMethod(IMethodSymbol symbol, Workspace workspace, CSharpCompilation compilation)
        {
            // The declaration, not the use: an extension method with its this parameter, a generic method
            // with its type parameters, the implementing part of a partial method.
            var definition = (symbol.ReducedFrom ?? symbol).OriginalDefinition;
            definition = definition.PartialImplementationPart ?? definition;

            var origin = OriginOf(definition, workspace, compilation);
            var method = (origin.IsSource ? FindDeclared(definition, workspace) : null) ?? FromSymbol(definition, origin);

            Origins.AddOrUpdate(method, origin);

            return method;
        }

        internal static Parameter ToParameter(IParameterSymbol parameter, string filePath)
        {
            var modifiers = ImmutableList.CreateBuilder<Modifier>();

            switch (parameter.RefKind)
            {
                case RefKind.Ref:
                    modifiers.Add(Modifier.Ref);
                    break;
                case RefKind.Out:
                    modifiers.Add(Modifier.Out);
                    break;
                case RefKind.In or RefKind.RefReadOnlyParameter:
                    modifiers.Add(Modifier.In);
                    break;
            }

            if (parameter.ScopedKind != ScopedKind.None)
            {
                modifiers.Add(Modifier.Scoped);
            }

            var defaultValue = parameter.HasExplicitDefaultValue ? FormatConstant(parameter.ExplicitDefaultValue) : null;

            return new Parameter
            {
                Name = parameter.Name,
                FullQualifiedName = parameter.Name,
                Type = parameter.Type.ToDisplayString(TypeFormat),
                Modifiers = modifiers.ToImmutable(),
                IsOptional = parameter.IsOptional,
                DefaultValue = defaultValue,
                IsParams = parameter.IsParams,
                IsNullable = parameter.NullableAnnotation == NullableAnnotation.Annotated,
                Ordinal = parameter.Ordinal,
                SyntaxTree = parameter.ToDisplayString(),
                FilePath = filePath
            };
        }

        internal static string FormatConstant(object? value)
        {
            return value is null ? "null" : SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false) ?? value.ToString() ?? "null";
        }

        /// <summary>The very record the fast mode built for this declaration, found by its span in the cached tree.</summary>
        private static Method? FindDeclared(IMethodSymbol definition, Workspace workspace)
        {
            var declaration = definition.DeclaringSyntaxReferences.FirstOrDefault();

            if (declaration is null || !workspace.IsSourceFile(declaration.SyntaxTree.FilePath))
            {
                return null;
            }

            var tree = workspace.Model(declaration.SyntaxTree.FilePath);

            return tree.AllTypes()
                       .SelectMany(t => t.Methods)
                       .FirstOrDefault(m => m.Location.SpanStart == declaration.Span.Start);
        }

        /// <summary>
        /// A method only known from metadata, or a local function, which the fast mode reports as
        /// <see cref="LocalFunction"/> rather than as a method.
        /// </summary>
        private static Method FromSymbol(IMethodSymbol definition, SymbolOrigin origin)
        {
            var sourceLocation = definition.Locations.FirstOrDefault(l => l.IsInSource);
            var filePath = sourceLocation?.SourceTree?.FilePath ?? origin.Path;
            var containingType = definition.ContainingType?.ToDisplayString(QualifiedFormat);

            return new Method
            {
                Name = definition.Name,
                FullQualifiedName = containingType is null ? definition.Name : $"{containingType}.{definition.Name}",
                ReturnParameter = definition.ReturnType.ToDisplayString(TypeFormat),
                Parameters = definition.Parameters.Select(p => ToParameter(p, filePath)).ToImmutableList(),
                TypeParameters = definition.TypeParameters
                                           .Select(t => new TypeParameter(t.Name,
                                                                          t.Variance switch
                                                                          {
                                                                              Microsoft.CodeAnalysis.VarianceKind.In => VarianceKind.In,
                                                                              Microsoft.CodeAnalysis.VarianceKind.Out => VarianceKind.Out,
                                                                              _ => VarianceKind.None
                                                                          },
                                                                          t.ConstraintTypes.Select(c => c.ToDisplayString(TypeFormat)).ToImmutableList(),
                                                                          ImmutableList<Attribute>.Empty))
                                           .ToImmutableList(),
                Modifiers = ModifiersOf(definition),
                Accessibility = AccessibilityOf(definition.DeclaredAccessibility),
                SyntaxTree = definition.ToDisplayString(),
                FilePath = filePath,
                Location = sourceLocation is null ? CodeLocation.None : ToCodeLocation(sourceLocation)
            };
        }

        private static SymbolOrigin OriginOf(IMethodSymbol definition, Workspace workspace, CSharpCompilation compilation)
        {
            if (definition.Locations.Any(l => l.IsInSource))
            {
                return SymbolOrigin.Source;
            }

            var assembly = definition.ContainingAssembly;

            if (assembly is null || compilation.GetMetadataReference(assembly) is not PortableExecutableReference { FilePath: { } path })
            {
                return SymbolOrigin.Unknown;
            }

            var package = workspace.PackageOf(path);

            return package is null
                       ? new SymbolOrigin(SymbolOriginKind.Framework, assembly.Identity.Name, assembly.Identity.Version.ToString(), path)
                       : new SymbolOrigin(SymbolOriginKind.Package, package.PackageId, package.Version, path);
        }

        private static ImmutableList<Modifier> ModifiersOf(IMethodSymbol method)
        {
            return new (bool Applies, Modifier Modifier)[]
                   {
                       (method.IsStatic, Modifier.Static),
                       (method.IsAbstract, Modifier.Abstract),
                       (method.IsVirtual, Modifier.Virtual),
                       (method.IsOverride, Modifier.Override),
                       (method.IsSealed, Modifier.Sealed),
                       (method.IsExtern, Modifier.Extern),
                       (method.IsAsync, Modifier.Async)
                   }.Where(m => m.Applies)
                    .Select(m => m.Modifier)
                    .ToImmutableList();
        }

        private static Accessibility AccessibilityOf(RoslynAccessibility accessibility)
        {
            return accessibility switch
            {
                RoslynAccessibility.Private => Accessibility.Private,
                RoslynAccessibility.ProtectedAndInternal => Accessibility.ProtectedAndInternal,
                RoslynAccessibility.Protected => Accessibility.Protected,
                RoslynAccessibility.Internal => Accessibility.Internal,
                RoslynAccessibility.ProtectedOrInternal => Accessibility.ProtectedOrInternal,
                RoslynAccessibility.Public => Accessibility.Public,
                _ => Accessibility.NotApplicable
            };
        }

        private static CodeLocation ToCodeLocation(Location location)
        {
            var span = location.GetLineSpan();

            return new CodeLocation(span.Path,
                                    span.StartLinePosition.Line + 1,
                                    span.StartLinePosition.Character + 1,
                                    span.EndLinePosition.Line + 1,
                                    span.EndLinePosition.Character + 1,
                                    location.SourceSpan.Start,
                                    location.SourceSpan.Length);
        }
    }
}
