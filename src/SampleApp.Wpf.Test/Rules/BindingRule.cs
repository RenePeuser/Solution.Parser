using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Extensions.Pack;
using Solution.Parser.CSharp;
using Solution.Parser.Xaml;

namespace SampleApp.Wpf.Test.Rules
{
    /// <summary>
    /// Every bound path has to exist on the view model the view declares.
    /// </summary>
    /// <remarks>
    /// A typo in a binding path is invisible at compile time and silently renders nothing at run time,
    /// which is exactly the kind of thing a code rule is for. The rule needs three things the parser
    /// only started providing recently: the prefix, so d:DataContext can be told from DataContext; a
    /// binding whose Path survived parsing; and a Location to report.
    /// </remarks>
    internal static class BindingRule
    {
        /// <summary>The view model a view declares through <c>d:DataContext</c>, empty when it declares none.</summary>
        internal static string DeclaredViewModel(this XamlSyntaxTree view)
        {
            var designContext = view.Root
                                    .Properties
                                    .FirstOrDefault(p => p.Prefix.EqualsTo("d") && p.Name.EqualsTo("DataContext"));

            var designInstance = designContext?.PropertyValue as MarkupExtension;

            return designInstance?["Type"]?.PropertyValue?.ValueText.Split(':').Last() ?? string.Empty;
        }

        internal static ImmutableList<string> FindUnresolvedPaths(XamlSyntaxTree view,
                                                                  IEnumerable<TypeDeclaration> types)
        {
            var viewModelName = view.DeclaredViewModel();
            if (viewModelName.IsNullOrEmpty())
            {
                return ImmutableList<string>.Empty;
            }

            var viewModel = types.FirstOrDefault(t => t.Name.EqualsTo(viewModelName));
            if (viewModel is null)
            {
                return ImmutableList.Create($"{view.Root.Location}: there is no type named {viewModelName}");
            }

            var known = viewModel.AllProperties().Select(p => p.Name).ToImmutableHashSet();

            return view.AllBindings()
                       .Select(b => b.Path?.ValueText ?? string.Empty)
                       // A dot binds to the data context itself and a nested path needs the semantic
                       // model to follow, which this parser deliberately does not build.
                       .Where(path => !path.IsNullOrEmpty() && !path.Contains('.'))
                       .Where(path => !known.Contains(path))
                       .Select(path => $"{view.FilePath}: {viewModelName} has no property {path}")
                       .ToImmutableList();
        }
    }
}
