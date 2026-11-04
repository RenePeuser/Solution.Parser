using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Writes a runtime type the way C# source writes it, so it can serve as a pattern for
    /// <see cref="TypeReference.Is(string)"/>: <c>System.Collections.Generic.List&lt;System.Int32&gt;</c>
    /// instead of <c>List`1</c>, <c>System.Int32?</c> for a <c>Nullable&lt;int&gt;</c>,
    /// <c>(System.Int32, System.String)</c> for a <c>ValueTuple</c>, <c>Outer.Inner</c> for a nested type.
    /// </summary>
    internal static class ClrTypeName
    {
        private static readonly ConcurrentDictionary<Type, string> Names = new();

        internal static string Of<T>()
        {
            return Of(typeof(T));
        }

        internal static string Of(Type type)
        {
            return Names.GetOrAdd(type, Format);
        }

        private static string Format(Type type)
        {
            if (type.ContainsGenericParameters)
            {
                throw new ArgumentException($"The type '{type}' is an open generic, which has no name in C# source. Pass the name as a string instead, as in Is(\"List\").", nameof(type));
            }

            if (type.IsArray)
            {
                return $"{Format(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
            }

            if (type.IsPointer)
            {
                return $"{Format(type.GetElementType()!)}*";
            }

            if (Nullable.GetUnderlyingType(type) is { } underlying)
            {
                return $"{Format(underlying)}?";
            }

            if (IsTuple(type))
            {
                return $"({string.Join(", ", TupleElements(type).Select(Format))})";
            }

            return Qualified(type, type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes);
        }

        /// <summary>
        /// The type arguments of a nested generic type list those of the containing types first, so
        /// each level takes its own share from the end of the list.
        /// </summary>
        private static string Qualified(Type type, Type[] arguments)
        {
            var arity = type.Name.IndexOf('`');
            var name = arity < 0 ? type.Name : type.Name[..arity];
            var ownCount = arity < 0 ? 0 : int.Parse(type.Name[(arity + 1)..]);
            var own = arguments[^ownCount..];
            var written = own.Length == 0 ? name : $"{name}<{string.Join(", ", own.Select(Format))}>";

            var prefix = type.DeclaringType is { } declaring
                             ? Qualified(declaring, arguments[..^ownCount])
                             : type.Namespace;

            return prefix is null ? written : $"{prefix}.{written}";
        }

        private static bool IsTuple(Type type)
        {
            return type is { IsGenericType: true, Namespace: "System" } &&
                   type.Name.StartsWith("ValueTuple`", StringComparison.Ordinal) &&
                   type.GetGenericArguments().Length >= 2;
        }

        /// <summary>A tuple of more than seven elements nests the rest in its eighth argument.</summary>
        private static IEnumerable<Type> TupleElements(Type tuple)
        {
            var arguments = tuple.GetGenericArguments();

            return arguments.Length == 8 && IsTuple(arguments[7])
                       ? arguments.Take(7).Concat(TupleElements(arguments[7]))
                       : arguments;
        }
    }
}
