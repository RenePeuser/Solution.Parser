using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Solution.Parser.CSharp
{
    internal static class LocalVariableExtensions
    {
        /// <summary>
        /// The variables declared inside the node, in source order. Like the calls, those of a lambda
        /// belong to the member, while a local function reports its own, so the search stops there.
        /// </summary>
        internal static ImmutableList<LocalVariable> ToLocalVariables(this SyntaxNode? node, string filePath)
        {
            if (node is null)
            {
                return ImmutableList<LocalVariable>.Empty;
            }

            return node.BodyNodes().Select(n => n.ToLocalVariableOrNull(filePath)).OfType<LocalVariable>().ToImmutableList();
        }

        /// <summary>
        /// The variable the node declares; null for a node that declares none. With
        /// <paramref name="memberInvocations"/>, the calls of an initializer are taken from those
        /// rather than collected again.
        /// </summary>
        internal static LocalVariable? ToLocalVariableOrNull(this SyntaxNode node, string filePath, IReadOnlyList<Invocation>? memberInvocations = null)
        {
            return node switch
            {
                VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } declarator => declarator.ToLocalVariable(declaration, filePath, memberInvocations),
                ForEachStatementSyntax forEach => forEach.ToLocalVariable(filePath),
                CatchDeclarationSyntax { Identifier.ValueText.Length: > 0 } catchDeclaration => catchDeclaration.ToLocalVariable(filePath),
                SingleVariableDesignationSyntax designation => designation.ToLocalVariable(filePath),
                _ => null
            };
        }

        private static LocalVariable ToLocalVariable(this VariableDeclaratorSyntax declarator, VariableDeclarationSyntax declaration, string filePath, IReadOnlyList<Invocation>? memberInvocations)
        {
            var (kind, isUsing, isAwaitUsing, isConst, source) = declaration.Parent switch
            {
                LocalDeclarationStatementSyntax statement => (LocalVariableKind.Declaration,
                                                              statement.UsingKeyword.IsKind(SyntaxKind.UsingKeyword),
                                                              statement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword),
                                                              statement.IsConst,
                                                              (SyntaxNode)statement),
                UsingStatementSyntax usingStatement => (LocalVariableKind.UsingStatement,
                                                        true,
                                                        usingStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword),
                                                        false,
                                                        declaration),
                ForStatementSyntax => (LocalVariableKind.For, false, false, false, declaration),
                FixedStatementSyntax => (LocalVariableKind.Fixed, false, false, false, declaration),
                _ => (LocalVariableKind.Declaration, false, false, false, declaration)
            };

            var initializer = declarator.Initializer?.Value;

            return Create(declarator.Identifier.ValueText,
                          declaration.Type.ToTypeReference(filePath),
                          kind,
                          source.ToSourceCode(),
                          declarator.ToCodeLocation(filePath),
                          filePath) with
            {
                Initializer = initializer?.ToSourceCode(),
                InitializerExpression = initializer.ToExpressionOrNull(filePath),
                Invocations = initializer is null
                                  ? ImmutableList<Invocation>.Empty
                                  : memberInvocations is null
                                      ? initializer.ToInvocations(filePath)
                                      : SpanSearch.InSpan(memberInvocations, i => i.Location.SpanStart, initializer.SpanStart, initializer.Span.End),
                IsUsing = isUsing,
                IsAwaitUsing = isAwaitUsing,
                IsConst = isConst,
                IsPartOfMultiVariableDeclaration = declaration.Variables.Count > 1
            };
        }

        private static LocalVariable ToLocalVariable(this ForEachStatementSyntax forEach, string filePath)
        {
            return Create(forEach.Identifier.ValueText,
                          forEach.Type.ToTypeReference(filePath),
                          LocalVariableKind.ForEach,
                          forEach.SourceBetween(forEach.Type.Span.Start, forEach.Identifier.Span.End),
                          forEach.Identifier.ToCodeLocation(filePath),
                          filePath);
        }

        private static LocalVariable ToLocalVariable(this CatchDeclarationSyntax catchDeclaration, string filePath)
        {
            return Create(catchDeclaration.Identifier.ValueText,
                          catchDeclaration.Type.ToTypeReference(filePath),
                          LocalVariableKind.Catch,
                          catchDeclaration.SourceBetween(catchDeclaration.Type.Span.Start, catchDeclaration.Identifier.Span.End),
                          catchDeclaration.Identifier.ToCodeLocation(filePath),
                          filePath);
        }

        /// <summary>
        /// A designation is the name part of <c>out var x</c>, <c>is Foo x</c> and <c>var (a, b)</c>. The
        /// type sits on the node that holds it, for <c>var (a, b)</c> one or more levels up.
        /// </summary>
        private static LocalVariable? ToLocalVariable(this SingleVariableDesignationSyntax designation, string filePath)
        {
            var holder = designation.Ancestors().FirstOrDefault(a => a is not ParenthesizedVariableDesignationSyntax);

            var (type, kind) = holder switch
            {
                DeclarationExpressionSyntax { Parent: ArgumentSyntax argument } expression when argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword)
                    => (expression.Type.ToTypeReference(filePath), LocalVariableKind.OutArgument),
                DeclarationExpressionSyntax { Parent: ForEachVariableStatementSyntax } expression
                    => (expression.Type.ToTypeReference(filePath), LocalVariableKind.ForEach),
                DeclarationExpressionSyntax expression => (expression.Type.ToTypeReference(filePath), LocalVariableKind.Deconstruction),
                DeclarationPatternSyntax pattern => (pattern.Type.ToTypeReference(filePath), LocalVariableKind.Pattern),
                RecursivePatternSyntax pattern => (pattern.Type.ToTypeReference(filePath), LocalVariableKind.Pattern),
                VarPatternSyntax pattern => (Var(pattern.VarKeyword, filePath), LocalVariableKind.Pattern),
                _ => ((TypeReference?)null, LocalVariableKind.Pattern)
            };

            if (type is null || holder is null)
            {
                return null;
            }

            return Create(designation.Identifier.ValueText, type, kind, holder.ToSourceCode(), designation.ToCodeLocation(filePath), filePath);
        }

        private static LocalVariable Create(string name, TypeReference type, LocalVariableKind kind, string source, CodeLocation location, string filePath)
        {
            return new LocalVariable
            {
                Name = name,
                FullyQualifiedName = name,
                Type = type.SourceCode,
                TypeReference = type,
                Kind = kind,
                SourceCode = source,
                FilePath = filePath,
                Location = location
            };
        }

        private static TypeReference Var(SyntaxToken varKeyword, string filePath)
        {
            return new TypeReference { Name = "var", SourceCode = "var", Location = varKeyword.ToCodeLocation(filePath) };
        }

        /// <summary>The source from <paramref name="start"/> to <paramref name="end"/>, for a declaration that is only a part of its node, like <c>var x</c> in a foreach.</summary>
        private static string SourceBetween(this SyntaxNode node, int start, int end)
        {
            return node.SyntaxTree.GetText().ToString(TextSpan.FromBounds(start, end));
        }
    }
}
