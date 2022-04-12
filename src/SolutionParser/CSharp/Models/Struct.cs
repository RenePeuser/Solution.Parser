using System.Collections.Generic;
using System.Diagnostics;

namespace SolutionParser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public class Struct : Class
    {
        internal Struct(
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
            : base(nameSpace, name, modifiers, constructors, properties, methods, attributes, fields, interfaces,
                baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums, nestedInterfaces)
        {
        }
    }
}
