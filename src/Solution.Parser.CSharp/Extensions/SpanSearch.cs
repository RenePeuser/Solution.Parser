using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Cuts the items of one region out of a list sorted by position, so a statement or a variable can
    /// take its calls from the member's calls instead of collecting them again.
    /// </summary>
    internal static class SpanSearch
    {
        /// <summary>The items starting in [<paramref name="start"/>, <paramref name="end"/>), found by binary search.</summary>
        internal static ImmutableList<T> InSpan<T>(IReadOnlyList<T> sorted, Func<T, int> startOf, int start, int end)
        {
            int low = 0, high = sorted.Count;

            while (low < high)
            {
                var middle = (low + high) / 2;

                if (startOf(sorted[middle]) < start)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            if (low >= sorted.Count || startOf(sorted[low]) >= end)
            {
                return ImmutableList<T>.Empty;
            }

            var result = ImmutableList.CreateBuilder<T>();

            for (var i = low; i < sorted.Count && startOf(sorted[i]) < end; i++)
            {
                result.Add(sorted[i]);
            }

            return result.ToImmutable();
        }
    }
}
