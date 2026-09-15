using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Breaks <c>{Name argument, Property=Value, Property={Nested a, b}}</c> into a
    /// <see cref="MarkupToken"/>, honouring nested braces, single quotes and backslash escapes.
    /// </summary>
    /// <remarks>
    /// This replaces the old approach of deciding by substring and then splitting on <c>,</c> and
    /// <c>=</c>. That approach mistook any value containing a colon for a markup extension, and it
    /// tore <c>{Binding Amount, StringFormat={0:#,##0.00}}</c> apart at the format string's comma.
    /// </remarks>
    internal static class MarkupExtensionTokenizer
    {
        private const string EscapePrefix = "{}";

        /// <summary>
        /// True when the value is markup rather than text: an opening brace followed by the start of an
        /// extension name. The <c>{}</c> escape is not markup, and neither is a composite format item
        /// such as <c>{0:N2}</c>, because an extension name never starts with a digit.
        /// </summary>
        internal static bool IsMarkupExtension(string? value)
        {
            var trimmed = value?.TrimStart();

            if (trimmed is null || !trimmed.StartsWith('{') || trimmed.StartWith(EscapePrefix))
            {
                return false;
            }

            var name = trimmed[1..].TrimStart();

            return name.Length > 0 && (char.IsLetter(name[0]) || name[0].Equals('_'));
        }

        /// <summary>True when the value is escaped with the <c>{}</c> prefix.</summary>
        internal static bool IsEscapedLiteral(string? value)
        {
            return value?.TrimStart().StartWith(EscapePrefix) == true;
        }

        /// <summary>Strips the <c>{}</c> escape prefix, leaving the literal the author meant.</summary>
        internal static string Unescape(string value)
        {
            var trimmed = value.TrimStart();

            return trimmed.StartWith(EscapePrefix) ? trimmed[EscapePrefix.Length..] : value;
        }

        /// <summary>
        /// Tokenises a markup extension. Returns false for unbalanced braces or a missing name rather
        /// than throwing, so one broken attribute costs a diagnostic instead of the whole file.
        /// </summary>
        internal static bool TryTokenize(string value, out MarkupToken token)
        {
            token = null!;

            var trimmed = value.Trim();
            if (trimmed.Length < 2 || !trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
            {
                return false;
            }

            var inner = trimmed[1..^1];
            if (!IsBalanced(inner))
            {
                return false;
            }

            var nameLength = NameLength(inner);
            var fullName = inner[..nameLength].Trim();
            if (fullName.Length.Equals(0))
            {
                return false;
            }

            var separator = fullName.IndexOf(':');
            var prefix = separator < 0 ? string.Empty : fullName[..separator];
            var name = StripExtensionSuffix(separator < 0 ? fullName : fullName[(separator + 1)..]);

            var arguments = SplitArguments(inner[nameLength..]).Select(ToArgument).ToImmutableList();

            token = new MarkupToken(value, prefix, name, arguments);

            return true;
        }

        /// <summary>
        /// <c>{StaticResourceExtension Foo}</c> and <c>{StaticResource Foo}</c> are the same thing, so
        /// routing by name only has to know one spelling.
        /// </summary>
        private static string StripExtensionSuffix(string name)
        {
            const string suffix = "Extension";

            return name.Length > suffix.Length && name.EndWith(suffix) ? name[..^suffix.Length] : name;
        }

        private static int NameLength(string inner)
        {
            for (var i = 0; i < inner.Length; i++)
            {
                if (char.IsWhiteSpace(inner[i]) || inner[i].Equals(','))
                {
                    return i;
                }
            }

            return inner.Length;
        }

        private static MarkupArgument ToArgument(string argument)
        {
            var separator = TopLevelEqualsIndex(argument);
            if (separator < 0)
            {
                return new MarkupArgument(null, Clean(argument));
            }

            return new MarkupArgument(Clean(argument[..separator]), Clean(argument[(separator + 1)..]));
        }

        /// <summary>
        /// The first <c>=</c> outside braces and quotes. Only the first one separates name from value,
        /// so <c>Path=A=B</c> keeps <c>A=B</c> instead of losing everything after the second sign.
        /// </summary>
        private static int TopLevelEqualsIndex(string argument)
        {
            var depth = 0;
            var quoted = false;

            for (var i = 0; i < argument.Length; i++)
            {
                var current = argument[i];

                if (current.Equals('\\'))
                {
                    i++;
                    continue;
                }

                if (current.Equals('\''))
                {
                    quoted = !quoted;
                    continue;
                }

                if (quoted)
                {
                    continue;
                }

                switch (current)
                {
                    case '{':
                        depth++;
                        break;
                    case '}':
                        depth--;
                        break;
                    case '=' when depth.Equals(0):
                        return i;
                }
            }

            return -1;
        }

        /// <summary>Splits on commas that are not inside braces or quotes.</summary>
        private static IEnumerable<string> SplitArguments(string arguments)
        {
            if (arguments.Trim().Length.Equals(0))
            {
                yield break;
            }

            var builder = new StringBuilder();
            var depth = 0;
            var quoted = false;

            for (var i = 0; i < arguments.Length; i++)
            {
                var current = arguments[i];

                if (current.Equals('\\') && i + 1 < arguments.Length)
                {
                    builder.Append(current).Append(arguments[i + 1]);
                    i++;
                    continue;
                }

                if (current.Equals('\''))
                {
                    quoted = !quoted;
                    builder.Append(current);
                    continue;
                }

                if (!quoted)
                {
                    switch (current)
                    {
                        case '{':
                            depth++;
                            break;
                        case '}':
                            depth--;
                            break;
                        case ',' when depth.Equals(0):
                            yield return builder.ToString();
                            builder.Clear();
                            continue;
                    }
                }

                builder.Append(current);
            }

            yield return builder.ToString();
        }

        /// <summary>Trims, removes one layer of single quotes and resolves backslash escapes.</summary>
        private static string Clean(string value)
        {
            var trimmed = value.Trim();

            if (trimmed.Length > 1 && trimmed.StartsWith('\'') && trimmed.EndsWith('\''))
            {
                trimmed = trimmed[1..^1];
            }

            if (!trimmed.Contains('\\'))
            {
                return trimmed;
            }

            var builder = new StringBuilder(trimmed.Length);
            for (var i = 0; i < trimmed.Length; i++)
            {
                if (trimmed[i].Equals('\\') && i + 1 < trimmed.Length)
                {
                    builder.Append(trimmed[i + 1]);
                    i++;
                    continue;
                }

                builder.Append(trimmed[i]);
            }

            return builder.ToString();
        }

        private static bool IsBalanced(string inner)
        {
            var depth = 0;
            var quoted = false;

            for (var i = 0; i < inner.Length; i++)
            {
                var current = inner[i];

                if (current.Equals('\\'))
                {
                    i++;
                    continue;
                }

                if (current.Equals('\''))
                {
                    quoted = !quoted;
                    continue;
                }

                if (quoted)
                {
                    continue;
                }

                if (current.Equals('{'))
                {
                    depth++;
                }
                else if (current.Equals('}'))
                {
                    depth--;

                    // A closing brace at depth zero would have ended the outer extension early, which
                    // means the trailing brace we cut off was not its partner.
                    if (depth < 0)
                    {
                        return false;
                    }
                }
            }

            return depth.Equals(0) && !quoted;
        }
    }
}
