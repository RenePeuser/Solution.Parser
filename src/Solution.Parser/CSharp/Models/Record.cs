using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{


    [DebuggerDisplay("{Name}")]
    public record Record(NameSpace NameSpace,
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
                         string FullQualifiedName,
                         string SyntaxTree) : Class(NameSpace, Name, Modifiers, Constructors, Properties, Methods, Attributes, Fields, Interfaces, BaseTypes, Events, EventFields, NestedClasses, NestedStructs, NestedEnums, NestedInterfaces,
        SyntaxTree, FullQualifiedName);
}
