using System.Collections.Immutable;

namespace Solution.Parser.CSharp
{
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
                            string SyntaxTree,
                            string FilePath) : DeclarationWithModifiers(Name, FullQualifiedName, Modifiers, Attributes, SyntaxTree, FilePath);
}
