using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Class(NameSpace NameSpace,
                        string Name,
                        ImmutableList<Modifier> Modifiers,
                        ImmutableList<Constructor> Constructors,
                        ImmutableList<Property> Properties,
                        ImmutableList<Method> Methods,
                        ImmutableList<Attribute> Attributes,
                        ImmutableList<Field> Fields,
                        ImmutableList<Interface> Interfaces,
                        ImmutableList<BaseType> BaseTypes,
                        ImmutableList<Event> Events,
                        ImmutableList<EventField> EventFields,
                        ImmutableList<Class> NestedClasses,
                        ImmutableList<Struct> NestedStructs,
                        ImmutableList<Enum> NestedEnums,
                        ImmutableList<Interface> NestedInterfaces,
                        ImmutableList<Parameter> Parameters,
                        string FullQualifiedName,
                        string SyntaxTree, string FilePath) : Interface(NameSpace, Name, Modifiers, Properties, Methods, Attributes, BaseTypes, Events, EventFields,
        NestedClasses, NestedStructs, NestedEnums, NestedInterfaces, FullQualifiedName, SyntaxTree, FilePath);
}
