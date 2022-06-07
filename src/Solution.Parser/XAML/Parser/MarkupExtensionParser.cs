using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal class MarkupExtensionParser : PropertyValueParserBase
    {
        private readonly IXamlPropertyValueParser _xamlPropertyValueParser;

        public MarkupExtensionParser()
        {
            // fast workaround to reduce recursion.    
            _xamlPropertyValueParser = new XamlPropertyValueParser(new ParserSelector(new IPropertyValueParserBase[]
            {
                new XamlUsingParser(), new UrlValueParser(), new SimpleValueResourceParser(),
                new StaticResourceParser(), new DynamicResourceParser(), new XTypeMarkupExtensionParser(),
                new XStaticMarkupExtensionParser(), new RelativeSourceParser(), new BindingParser(),
                new AttachedPropertyParser(), new StringFormatParser(), this,
                // Workaround till all conditions are done
                new UnknownPropertyValueParser()
            }));
        }

        public override Predicate<string> IsThisTheCorrectParserFor { get; } = item =>
            item.Contains(':') && !(item.ToUpperInvariant().EqualsTo("X:NULL") ||
                                    item.ToUpperInvariant().EqualsTo("{X:NULL}"));

        public override PropertyValue Parse(string value, int lineNumber)
        {
            // ToDo fix it. We need faster and better parse logic.

            var markupExtension = value[1..^1];
            var bindingInfo = markupExtension.Split(',');

            if (bindingInfo.Length == 1 &&
                !markupExtension.Contains(' '))
            {
                return new MarkupExtension(value, bindingInfo[0].Split(':').Last());
            }

            var markupFullName = bindingInfo[0][..bindingInfo[0].IndexOf(' ')];
            var name = markupFullName.Split(':').Last();
            var allPropertyInfos = bindingInfo.ToArray();
            allPropertyInfos[0] = bindingInfo[0].Replace(markupFullName, string.Empty);

            var evaluatedMarkupExtension = EvaluateMarkupExtension(allPropertyInfos).ToImmutableList();
            if (evaluatedMarkupExtension.Any(s => !s.Contains('=')))
            {
                throw new CanNotParseMarkupException(
                    $"Could not parse markupextension:'{value}', because of missing equal expression:\r\nSample:{{MyMarkup Property1=Value1, Property2=Value2}}");
            }

            var properties = ParseToProperties(evaluatedMarkupExtension, lineNumber).ToImmutableList();

            return new MarkupExtension(value, name, properties);
        }

        private IEnumerable<Property> ParseToProperties(IImmutableList<string> evaluatedMarkupExtension, int lineNumber)
        {
            // Damm hell workaround
            var firstItem = evaluatedMarkupExtension[0];
            if (evaluatedMarkupExtension.Count == 1)
            {
                if (!firstItem.Contains('='))
                {
                    yield return new Property(lineNumber, "Unknown", _xamlPropertyValueParser.Parse(firstItem, lineNumber));
                    yield break;
                }
            }

            for (var i = 0; i < evaluatedMarkupExtension.Count; i++)
            {
                var declaration = evaluatedMarkupExtension.ElementAt(i);

                for (var j = i + 1; j < evaluatedMarkupExtension.Count; j++)
                {
                    var nextDeclaration = evaluatedMarkupExtension.ElementAt(j);
                    if (!nextDeclaration.Contains('='))
                    {
                        declaration = declaration + "," + nextDeclaration;
                        i = j;
                    }
                    else
                    {
                        break;
                    }
                }

                var indexOf = declaration.IndexOf('=');
                indexOf = indexOf == -1 ? declaration.IndexOf(':') : indexOf;
                var propertyName = declaration[..indexOf].Trim().Split(':').Last();
                var nextStartIndex = indexOf + 1;
                var propertyValue = declaration[nextStartIndex..];
                yield return new Property(lineNumber, propertyName,
                    _xamlPropertyValueParser.Parse(propertyValue, lineNumber));
            }
        }

        private IEnumerable<string> EvaluateMarkupExtension(string[] markupExtension)
        {
            for (var i = 0; i < markupExtension.Length; i++)
            {
                var expression = markupExtension[i];

                var openBrackets = expression.Count(c => c == '{');
                var closeBrackets = expression.Count(c => c == '}');

                if (openBrackets > closeBrackets)
                {
                    for (var j = i + 1; j < markupExtension.Length; j++)
                    {
                        var nextExpression = markupExtension[j];
                        expression = $"{expression},{nextExpression}";
                        var count = expression.Count(c => c == '}');

                        if (count == openBrackets)
                        {
                            i = j;
                            break;
                        }
                    }
                }

                yield return expression;
            }
        }
    }
}
