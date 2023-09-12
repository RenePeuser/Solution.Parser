using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Class(NameSpace NameSpace,
                        string Name,
                        IImmutableList<Modifier> Modifiers,
                        IImmutableList<Constructor> Constructors,
                        IImmutableList<Property> Properties,
                        IImmutableList<Method> Methods,
                        IImmutableList<Attribute> Attributes,
                        IImmutableList<Field> Fields,
                        IImmutableList<Interface> Interfaces,
                        IImmutableList<BaseType> BaseTypes,
                        IImmutableList<Event> Events,
                        IImmutableList<EventField> EventFields,
                        IImmutableList<Class> NestedClasses,
                        IImmutableList<Struct> NestedStructs,
                        IImmutableList<Enum> NestedEnums,
                        IImmutableList<Interface> NestedInterfaces,
                        IImmutableList<Parameter> Parameters,
                        string FullQualifiedName,
                        string SyntaxTree, string FilePath) : Interface(NameSpace, Name, Modifiers, Properties, Methods, Attributes, BaseTypes, Events, EventFields,
        NestedClasses, NestedStructs, NestedEnums, NestedInterfaces, FullQualifiedName, SyntaxTree, FilePath);
}
