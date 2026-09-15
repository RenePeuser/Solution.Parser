using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Solution.Parser.Xaml;

namespace SampleApp.Wpf.Test.Rules
{
    /// <summary>
    /// Every StaticResource key used anywhere in the application has to be declared somewhere in it.
    /// </summary>
    /// <remarks>
    /// A missing resource key throws at run time, when the view is first shown. Finding it needs the
    /// keys of one file matched against the references of another, so the rule works over the whole
    /// application rather than over a single view.
    /// </remarks>
    internal static class ResourceRule
    {
        internal static ImmutableList<string> FindUndeclaredKeys(IEnumerable<XamlSyntaxTree> views)
        {
            var allViews = views.ToImmutableList();

            var declared = allViews.SelectMany(v => v.AllElements())
                                   .Select(e => e.XKey)
                                   .Where(key => key.Length > 0)
                                   .ToImmutableHashSet();

            return allViews.SelectMany(view => view.AllMarkupExtensions()
                                                   .OfType<StaticResource>()
                                                   .Where(r => !declared.Contains(r.ResourceKey))
                                                   .Select(r => $"{view.FilePath}: no resource named {r.ResourceKey}"))
                           .ToImmutableList();
        }
    }
}
