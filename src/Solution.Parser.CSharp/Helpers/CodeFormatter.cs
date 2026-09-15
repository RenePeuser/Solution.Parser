using Extensions.Pack;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Solution.Parser.CSharp
{
    public static class AddCodeFormatterExtension
    {
        public static void AddCodeFormatter(this IServiceCollection services)
        {
            services.AddSingletonIfNotExists<ICodeFormatter, CodeFormatter>();
        }
    }

    internal sealed class CodeFormatter : ICodeFormatter
    {
        public string FormatCode(string code)
        {
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var formattedNode = syntaxTree.GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
            return formattedNode;
        }
    }

    public interface ICodeFormatter
    {
        string FormatCode(string code);
    }
}
