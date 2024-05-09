using System;
using System.Linq;
using System.Text;
using Extensions.Pack;

namespace Solution.Parser.CSharp
{
    public static partial class RecordExtensions
    {
        private static readonly CodeFormatter CodeFormatter = new();

        public static readonly string[] ForbiddenListTypes = new[]
        {
            "IEnumerable",
            "List",
            "Collection",
            "HashSet",
            "Dictionary",
            "Queue",
            "Stack",
            "LinkedList",
            "ObservableCollection",
            "BindingList",
            "SortedList",
            "ConcurrentBag",
            "ConcurrentQueue",
            "ConcurrentStack",
            "ConcurrentDictionary",
            "BlockingCollection",
            "[]"
        };

        public static string ToPropertyDeclaration(this Record record)
        {
            var stringBuilder = new StringBuilder();
            var modifiers = record.Modifiers.Select(m => m.ToString().ToLowerInvariant()).Flatten(" ");
            stringBuilder.AppendLine($"{modifiers} record {record.Name}");
            stringBuilder.AppendLine("{");
            foreach (var recordParameter in record.Parameters)
            {
                stringBuilder.AppendLine($"{modifiers} required {recordParameter.Type} {recordParameter.Name} {{ get; init; }}");
            }

            foreach (var recordParameter in record.Properties)
            {
                stringBuilder.AppendLine(recordParameter.SyntaxTree);
            }
            stringBuilder.AppendLine("}");
            var rawSyntaxTree = stringBuilder.ToString();
            var formattedCode = CodeFormatter.FormatCode(rawSyntaxTree);
            return formattedCode;
        }

        public static string Normalize(this Record record)
        {
            // 1. Fix list types
            var result = record.ToImmutableListProperties();

            // 2. Fix partial modifiers
            var newRecord = result.Replace("partial ", string.Empty, StringComparison.OrdinalIgnoreCase);

            return newRecord;
        }

        public static string ToImmutableListProperties(this Record record)
        {
            var stringBuilder = new StringBuilder();
            var modifiers = record.Modifiers.Select(m => m.ToString().ToLowerInvariant()).Flatten(" ");
            stringBuilder.AppendLine($"{modifiers} record {record.Name}");
            stringBuilder.AppendLine("{");
            foreach (var recordParameter in record.Parameters)
            {
                var genericType = GlobalRegex.GetGenericTypeRegex().Match(recordParameter.Type).Groups[1].Value;
                genericType = recordParameter.Type.Contains("[]") ? recordParameter.Type.Replace("[]", string.Empty) : genericType;
                genericType = genericType.Trim('?');
                var newType = ForbiddenListTypes.Any(type => recordParameter.Type.Contains(type)) ? $"IImmutableList<{genericType}>" : recordParameter.Type;
                var listInitializer = newType.Contains("IImmutableList") ? $" = ImmutableList<{genericType}>.Empty;" : string.Empty;
                stringBuilder.AppendLine($"{modifiers} required {newType} {recordParameter.Name} {{ get; init; }} {listInitializer}");
            }

            foreach (var recordParameter in record.Properties)
            {
                var genericType = GlobalRegex.GetGenericTypeRegex().Match(recordParameter.Type).Groups[1].Value;
                genericType = recordParameter.Type.Contains("[]") ? recordParameter.Type.Replace("[]", string.Empty) : genericType;
                genericType = genericType.Trim('?');
                var newType = ForbiddenListTypes.Any(type => recordParameter.Type.Contains(type)) ? $"IImmutableList<{genericType}>" : recordParameter.Type;
                var listInitializer = newType.Contains("IImmutableList") ? $" = ImmutableList<{genericType}>.Empty;" : string.Empty;
                stringBuilder.AppendLine($"{modifiers} {newType} {recordParameter.Name} {{ get; init; }}{listInitializer}");
            }
            stringBuilder.AppendLine("}");
            var rawSyntaxTree = stringBuilder.ToString();

            var formattedCode = CodeFormatter.FormatCode(rawSyntaxTree);
            return formattedCode;
        }
    }
}
