using System.Collections.Generic;
using System.Diagnostics;
using Argument.Check;

namespace SolutionParser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public class Interface : DeclarationWithModifiers
    {
        internal Interface(
            NameSpace nameSpace,
            string name,
            IEnumerable<Modifier> modifiers,
            IEnumerable<Property> properties,
            IEnumerable<Method> methods,
            IEnumerable<Attribute> attributes,
            IEnumerable<BaseType> baseTypes,
            IEnumerable<Event> events,
            IEnumerable<EventField> eventFields,
            IEnumerable<Class> nestedClasses,
            IEnumerable<Struct> nestedStructs,
            IEnumerable<Enum> nestedEnums,
            IEnumerable<Interface> nestedInterfaces) : base(name, modifiers)
        {
            Throw.IfNull(() => nameSpace);
            Throw.IfNull(() => name);
            Throw.IfNull(() => properties);
            Throw.IfNull(() => methods);
            Throw.IfNull(() => attributes);
            Throw.IfNull(() => baseTypes);
            Throw.IfNull(() => events);
            Throw.IfNull(() => eventFields);
            Throw.IfNull(() => nestedClasses);
            Throw.IfNull(() => nestedStructs);
            Throw.IfNull(() => nestedEnums);
            Throw.IfNull(() => nestedInterfaces);

            NameSpace = nameSpace;
            FullQualifiedName = nameSpace.Name + "." + name;
            Properties = properties;
            Methods = methods;
            Attributes = attributes;
            BaseTypes = baseTypes;
            Events = events;
            EventFields = eventFields;
            NestedClasses = nestedClasses;
            NestedStructs = nestedStructs;
            NestedEnums = nestedEnums;
            NestedInterfaces = nestedInterfaces;
        }

        public IEnumerable<Event> Events { get; }

        public IEnumerable<EventField> EventFields { get; }

        public IEnumerable<Class> NestedClasses { get; }

        public IEnumerable<Struct> NestedStructs { get; }

        public IEnumerable<Enum> NestedEnums { get; }

        public IEnumerable<Interface> NestedInterfaces { get; }

        public IEnumerable<BaseType> BaseTypes { get; }

        public string FullQualifiedName { get; }

        public NameSpace NameSpace { get; }

        public IEnumerable<Property> Properties { get; }

        public IEnumerable<Method> Methods { get; }

        public IEnumerable<Attribute> Attributes { get; }
    }
}
