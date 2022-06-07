using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{


    [DebuggerDisplay("{Name}")]
    public record Record : Class
    {
        internal Record(NameSpace nameSpace,
                        string name,
                        IImmutableList<Modifier> modifiers,
                        IImmutableList<Constructor> constructors,
                        IImmutableList<Property> properties,
                        IImmutableList<Method> methods,
                        IImmutableList<Attribute> attributes,
                        IImmutableList<Field> fields,
                        IImmutableList<Interface> interfaces,
                        IImmutableList<BaseType> baseTypes,
                        IImmutableList<Event> events,
                        IImmutableList<EventField> eventFields,
                        IImmutableList<Class> nestedClasses,
                        IImmutableList<Struct> nestedStructs,
                        IImmutableList<Enum> nestedEnums,
                        IImmutableList<Interface> nestedInterfaces,
                        string syntaxTree) : base(nameSpace, name, modifiers, constructors, properties, methods, attributes, fields, interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums, nestedInterfaces, syntaxTree)
        {
        }
    }
}
