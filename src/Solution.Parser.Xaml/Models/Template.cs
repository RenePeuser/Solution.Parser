using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A <c>DataTemplate</c>, <c>ControlTemplate</c>, <c>ItemsPanelTemplate</c> or
    /// <c>HierarchicalDataTemplate</c>. Which one it is reads from <see cref="ElementBase.TypeName"/>.
    /// </summary>
    [DebuggerDisplay("{FullTypeName,nq} Key:{XKey}")]
    public record Template : ElementBase
    {
        public override ElementKind Kind => ElementKind.Template;

        /// <summary>The value of <c>DataType</c> or <c>TargetType</c>, empty when the template declares neither.</summary>
        public string TargetTypeName => ((this["DataType"] ?? this["TargetType"])?.PropertyValue).ToTypeName();
    }
}
