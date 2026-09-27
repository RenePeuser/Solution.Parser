using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Solution.Parser.CSharp;
using Solution.Parser.Sln;
using Solution.Parser.Xaml;

namespace SampleApp.Wpf.Test
{
    /// <summary>
    /// Finds the sample application and parses it, using the library on itself: the solution parser
    /// locates the project, the project hands out its files, and the language packages turn them into
    /// the typed views.
    /// </summary>
    internal static class SampleApp
    {
        private static readonly Lazy<Parsed> Lazy = new(Load);

        internal static ImmutableList<XamlSyntaxTree> Views => Lazy.Value.Views;

        internal static ImmutableList<TypeDeclaration> Types => Lazy.Value.Types;

        internal static XamlSyntaxTree View(string fileName)
        {
            return Views.Single(v => Path.GetFileName(v.FilePath) == fileName);
        }

        private static Parsed Load()
        {
            var solution = new SolutionFileName("Solution.Parser.sln")
                           .FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory))
                           .Parse();

            var sampleApp = solution.Projects.Single(p => p.ProjectFileInfo.FileNameWithoutExtenion == "SampleApp.Wpf");

            return new Parsed(sampleApp.SourceFiles.GetXamlFiles().Select(x => x.Parse()).ToImmutableList(),
                              sampleApp.SourceFiles.GetCSharpFiles().Select(c => c.Parse()).AllTypes());
        }

        private sealed record Parsed(ImmutableList<XamlSyntaxTree> Views, ImmutableList<TypeDeclaration> Types);
    }
}
