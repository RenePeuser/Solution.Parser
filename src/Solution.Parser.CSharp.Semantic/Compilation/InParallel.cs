using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Solution.Parser.CSharp
{
    internal static class InParallel
    {
        /// <summary>
        /// <c>Select</c> on all cores, results in the order of the source. A failure surfaces as it would
        /// have without the parallelism, not wrapped in an AggregateException.
        /// </summary>
        internal static ImmutableList<TResult> SelectInOrder<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            try
            {
                return source.AsParallel().AsOrdered().Select(selector).ToImmutableList();
            }
            catch (AggregateException e) when (e.InnerExceptions.Count >= 1)
            {
                ExceptionDispatchInfo.Throw(e.InnerExceptions[0]);

                throw;
            }
        }
    }
}
