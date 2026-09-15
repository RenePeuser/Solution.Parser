using System;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    internal static class ComparableExtensions
    {
        internal static bool IsLessThan<T>(this T source, T target)
            where T : IComparable
        {
            Throw.IfNull<object>(source);
            Throw.IfNull<object>(target);

            return source.CompareTo(target) < 0;
        }

        internal static bool IsLessOrEqual<T>(this T source, T target)
            where T : IComparable
        {
            Throw.IfNull<object>(source);
            Throw.IfNull<object>(target);

            return !source.IsGreaterThan(target);
        }

        internal static bool IsGreaterThan<T>(this T source, T target)
            where T : IComparable
        {
            Throw.IfNull<object>(source);
            Throw.IfNull<object>(target);

            return source.CompareTo(target) > 0;
        }

        internal static bool IsGreaterOrEqual<T>(this T source, T target)
            where T : IComparable
        {
            Throw.IfNull<object>(source);
            Throw.IfNull<object>(target);

            return !source.IsLessThan(target);
        }

        internal static bool IsInRange<T>(this T source, T lowerLimit, T upperLimit)
            where T : IComparable
        {
            Throw.IfLessThan(upperLimit, lowerLimit);

            return source.IsLessOrEqual(upperLimit) && source.IsGreaterOrEqual(lowerLimit);
        }

        internal static bool IsOutOfRange<T>(this T source, T lowerLimit, T upperLimit)
            where T : IComparable
        {
            Throw.IfLessThan(upperLimit, lowerLimit);

            return !source.IsInRange(lowerLimit, upperLimit);
        }
    }
}
