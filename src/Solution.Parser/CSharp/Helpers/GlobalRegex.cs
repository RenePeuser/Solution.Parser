using System.Text.RegularExpressions;

namespace Solution.Parser.CSharp;

internal static partial class GlobalRegex
{
    [GeneratedRegex("([V])\\d")]
    internal static partial Regex VersionRegex();

    [GeneratedRegex(@"<([^>]*)>")]
    internal static partial Regex GetGenericTypeRegex();
}
