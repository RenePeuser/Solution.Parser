using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Interface(NameSpace NameSpace,
                            string Name,
                            IImmutableList<Modifier> Modifiers,
                            IImmutableList<Property> Properties,
                            IImmutableList<Method> Methods,
                            IImmutableList<Attribute> Attributes,
                            IImmutableList<BaseType> BaseTypes,
                            IImmutableList<Event> Events,
                            IImmutableList<EventField> EventFields,
                            IImmutableList<Class> NestedClasses,
                            IImmutableList<Struct> NestedStructs,
                            IImmutableList<Enum> NestedEnums,
                            IImmutableList<Interface> NestedInterfaces,
                            string FullQualifiedName,
                            string SyntaxTree) : DeclarationWithModifiers(Name, FullQualifiedName, Modifiers,SyntaxTree);
}
