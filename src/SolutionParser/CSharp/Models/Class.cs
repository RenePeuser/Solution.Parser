using System.Collections.Generic;
using System.Diagnostics;
using Argument.Check;

namespace SolutionParser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public class Class : Interface
    {
        internal Class(
            NameSpace nameSpace,
            string name,
            IEnumerable<Modifier> modifiers,
            IEnumerable<Constructor> constructors,
            IEnumerable<Property> properties,
            IEnumerable<Method> methods,
            IEnumerable<Attribute> attributes,
            IEnumerable<Field> fields,
            IEnumerable<Interface> interfaces,
            IEnumerable<BaseType> baseTypes,
            IEnumerable<Event> events,
            IEnumerable<EventField> eventFields,
            IEnumerable<Class> nestedClasses,
            IEnumerable<Struct> nestedStructs,
            IEnumerable<Enum> nestedEnums,
            IEnumerable<Interface> nestedInterfaces)
            : base(nameSpace, name, modifiers, properties, methods, attributes, baseTypes, events, eventFields,
                nestedClasses, nestedStructs, nestedEnums, nestedInterfaces)
        {
            Throw.IfNull(() => modifiers);
            Throw.IfNull(() => constructors);
            Throw.IfNull(() => fields);

            Constructors = constructors;
            Fields = fields;
            Interfaces = interfaces;
        }

        public IEnumerable<Interface> Interfaces { get; }

        public IEnumerable<Constructor> Constructors { get; }

        public IEnumerable<Field> Fields { get; }
    }
}
