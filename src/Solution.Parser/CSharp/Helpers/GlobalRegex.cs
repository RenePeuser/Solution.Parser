using System.Text.RegularExpressions;

namespace Solution.Parser.CSharp;

internal static partial class GlobalRegex
{
    [GeneratedRegex("([V])\\d")]
    internal static partial Regex VersionRegex();

    [GeneratedRegex(@"<([^>]*)>")]
    internal static partial Regex GetGenericTypeRegex();

    [GeneratedRegex(@"MapGroup\(\""(.*?)\""")]
    internal static partial Regex MapGroupRegex();

    [GeneratedRegex(@"\.MapToApiVersion\((?<version>\d+)\)")]
    internal static partial Regex MapToApiVersionRegex();

    [GeneratedRegex(@"\.WithTags\(""(?<tag>[^""]+)""\)")]
    internal static partial Regex WithTagsRegex();

    [GeneratedRegex(@"\.Produces\((?<code>\d+), typeof\((?<type>[^\)]+)\)\)")]
    internal static partial Regex ProducesRegex();

    [GeneratedRegex(@"\.WithName\(""(?<name>[^""]+)""\)")]
    internal static partial Regex WithNameRegex();

    [GeneratedRegex(@"\.Map(?<action>\w+)\(")]
    internal static partial Regex MapRegex();

    [GeneratedRegex(@"\((.*?)\)\s*=>")]
    internal static partial Regex LambdaExpressionRegex();
}
