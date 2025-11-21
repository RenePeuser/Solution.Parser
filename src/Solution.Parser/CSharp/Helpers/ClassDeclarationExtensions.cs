using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Extensions.Pack;

namespace Solution.Parser.CSharp.Helpers
{
    public static class ClassDeclarationExtensions
    {
        public static string BuildServiceRegistration(this Class @class, ImmutableList<Class> classes)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"internal static class Add{@class.Name}Extension");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine($"internal static void Add{@class.Name}(this IServiceCollection services)");
            stringBuilder.AppendLine("{");

            foreach (var parameter in @class.Parameters)
            {
                // Injection of multiple services
                if (parameter.Type.StartWith("IEnumerable<"))
                {
                    var genericType = GlobalRegex.GetGenericTypeRegex().Match(parameter.Type).Groups[1].Value;
                    var allTypes = classes.Where(c => c.BaseTypes.Any(b => b.TypeName.EqualsTo(genericType))).ToImmutableList();
                    foreach (var type in allTypes)
                    {
                        stringBuilder.AppendLine($"services.Add{type}();");
                    }
                }
                else
                {
                    var type = parameter.Type.Take(2).All(c => char.IsUpper(c)) && parameter.Type[0].EqualsTo('I') ? parameter.Type[1..] : parameter.Type;

                    stringBuilder.AppendLine($"services.Add{type}();");
                }
            }

            stringBuilder.AppendLine();

            var selfInterface = @class.BaseTypes.FirstOrDefault(b => b.TypeName.Contains(@class.Name) ||
                                                                     b.TypeName.Take(2).All(c => char.IsUpper(c) && b.TypeName[0].EqualsTo('I')));

            if (@class.BaseTypes.Any() && selfInterface.IsNotNull())
            {
                stringBuilder.AppendLine($"services.AddSingletonIfNotExists<{selfInterface.TypeName}, {@class.Name}>();");
            }
            else
            {
                stringBuilder.AppendLine($"services.AddSingletonIfNotExists<{@class.Name}>();");
            }

            stringBuilder.AppendLine("}");
            stringBuilder.AppendLine("}");

            var code = stringBuilder.ToString();
            var formatedCode = code.FormatSyntaxTree();
            return formatedCode;
        }
    }
}
