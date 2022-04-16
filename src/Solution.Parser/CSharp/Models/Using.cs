namespace Solution.Parser.CSharp
{
    public class Using : ImmutableSemanticType<string>
    {
        internal Using(string value) : base(value)
        {
        }
    }
}
