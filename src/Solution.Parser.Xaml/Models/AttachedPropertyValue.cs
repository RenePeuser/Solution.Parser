namespace Solution.Parser.Xaml
{
    /// <summary>
    /// An attached property named inside a value, as in <c>{Binding Path=(Grid.Row)}</c>.
    /// </summary>
    public record AttachedPropertyValue : PropertyValue
    {
        internal AttachedPropertyValue(string rawValue) : base(rawValue)
        {
        }

        public required string OwnerTypeName { get; init; }

        public required string PropertyName { get; init; }

        public string FullQualifiedName => $"{OwnerTypeName}.{PropertyName}";
    }
}
