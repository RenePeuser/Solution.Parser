using System.Collections.Immutable;
using System.Diagnostics;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Interface : DeclarationWithModifiers
    {
        internal Interface(
            NameSpace nameSpace,
            string name,
            IImmutableList<Modifier> modifiers,
            IImmutableList<Property> properties,
            IImmutableList<Method> methods,
            IImmutableList<Attribute> attributes,
            IImmutableList<BaseType> baseTypes,
            IImmutableList<Event> events,
            IImmutableList<EventField> eventFields,
            IImmutableList<Class> nestedClasses,
            IImmutableList<Struct> nestedStructs,
            IImmutableList<Enum> nestedEnums,
            IImmutableList<Interface> nestedInterfaces) : base(name, modifiers)
        {
            Throw.IfNull(nameSpace);
            Throw.IfNull(name);
            Throw.IfNull(properties);
            Throw.IfNull(methods);
            Throw.IfNull(attributes);
            Throw.IfNull(baseTypes);
            Throw.IfNull(events);
            Throw.IfNull(eventFields);
            Throw.IfNull(nestedClasses);
            Throw.IfNull(nestedStructs);
            Throw.IfNull(nestedEnums);
            Throw.IfNull(nestedInterfaces);

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

        public IImmutableList<Event> Events { get; }

        public IImmutableList<EventField> EventFields { get; }

        public IImmutableList<Class> NestedClasses { get; }

        public IImmutableList<Struct> NestedStructs { get; }

        public IImmutableList<Enum> NestedEnums { get; }

        public IImmutableList<Interface> NestedInterfaces { get; }

        public IImmutableList<BaseType> BaseTypes { get; }

        public string FullQualifiedName { get; }

        public NameSpace NameSpace { get; }

        public IImmutableList<Property> Properties { get; }

        public IImmutableList<Method> Methods { get; }

        public IImmutableList<Attribute> Attributes { get; }
    }
}
