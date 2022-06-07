using System.Collections.Immutable;
using System.Diagnostics;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Class : Interface
    {
        internal Class(
            NameSpace nameSpace,
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
            string syntaxtTree)
            : base(nameSpace, name, modifiers, properties, methods, attributes, baseTypes, events, eventFields,
                nestedClasses, nestedStructs, nestedEnums, nestedInterfaces)
        {
            Throw.IfNull(modifiers);
            Throw.IfNull(constructors);
            Throw.IfNull(fields);

            Constructors = constructors;
            Fields = fields;
            Interfaces = interfaces;
            SyntaxtTree = syntaxtTree;
        }

        public IImmutableList<Interface> Interfaces { get; }

        public IImmutableList<Constructor> Constructors { get; }

        public IImmutableList<Field> Fields { get; }

        public string SyntaxtTree { get; }
    }
}
