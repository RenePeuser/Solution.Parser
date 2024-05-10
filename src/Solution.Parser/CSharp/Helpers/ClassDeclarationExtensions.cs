using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions.Pack;

namespace Solution.Parser.CSharp.Helpers
{
    public static class ClassDeclarationExtensions
    {
        public static string BuildServiceRegistration(this Class @class)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"internal static class Add{@class.Name}Extension");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine($"internal static void Add{@class.Name}(this IServiceCollection services)");
            stringBuilder.AppendLine("{");

            foreach (var parameter in @class.Parameters)
            {
                var type = parameter.Type.Take(2).All(c => char.IsUpper(c)) && parameter.Type[0] == 'I' ? parameter.Type.Substring(1) : parameter.Type;

                stringBuilder.AppendLine($"services.Add{type}();");
            }

            stringBuilder.AppendLine();

            var selfInterface = @class.BaseTypes.FirstOrDefault(b => b.TypeName.Contains(@class.Name) ||
                                                                     b.TypeName.Take(2).All(c => char.IsUpper(c) && b.TypeName[0] == 'I'));

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
