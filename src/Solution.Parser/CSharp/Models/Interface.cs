using System.Collections.Immutable;

namespace Solution.Parser.CSharp
{
    public record Interface(NameSpace NameSpace,
                            string Name,
                            ImmutableList<Modifier> Modifiers,
                            ImmutableList<Property> Properties,
                            ImmutableList<Method> Methods,
                            ImmutableList<Attribute> Attributes,
                            ImmutableList<BaseType> BaseTypes,
                            ImmutableList<Event> Events,
                            ImmutableList<EventField> EventFields,
                            ImmutableList<Class> NestedClasses,
                            ImmutableList<Struct> NestedStructs,
                            ImmutableList<Enum> NestedEnums,
                            ImmutableList<Interface> NestedInterfaces,
                            string FullQualifiedName,
                            string SyntaxTree,
                            string FilePath) : DeclarationWithModifiers(Name, FullQualifiedName, Modifiers, Attributes, SyntaxTree, FilePath);
}
