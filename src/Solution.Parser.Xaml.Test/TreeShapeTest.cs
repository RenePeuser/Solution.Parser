using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Xaml;
using static Solution.Parser.Xaml.Test.ParseHelper;

namespace Solution.Parser.Xaml.Test
{
    /// <summary>
    /// The shape of the element tree. Every builder used to iterate <c>Descendants()</c> and then
    /// recurse into each descendant again, which flattened the tree, duplicated every node and made
    /// the work exponential in the nesting depth.
    /// </summary>
    [TestClass]
    public class TreeShapeTest
    {
        private const string BuilderFix =
            "Check ElementBuilder, it must walk Elements() once per node rather than Descendants() per node";

        private const string Nested =
            """
            <UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
              <Grid>
                <StackPanel>
                  <Border>
                    <Grid>
                      <TextBlock Text="hi" />
                    </Grid>
                  </Border>
                </StackPanel>
              </Grid>
            </UserControl>
            """;

        [TestMethod]
        public void Children_Are_Only_The_Directly_Declared_Elements()
        {
            var root = ParseXaml(Nested).Root;

            Assert.That.HasCount(1,
                                 root.Children,
                                 because: "the user control declares exactly one child, the outer Grid",
                                 fix: BuilderFix);

            Assert.That.AreEqual("Grid",
                                 root.Children.Single().TypeName,
                                 because: "the outer Grid is the only element written directly inside the user control",
                                 fix: BuilderFix);

            Assert.That.AreEqual("StackPanel",
                                 root.Children.Single().Children.Single().TypeName,
                                 because: "nesting must be preserved instead of being flattened onto the root",
                                 fix: BuilderFix);
        }

        [TestMethod]
        public void Every_Element_Is_Built_Exactly_Once()
        {
            var tree = ParseXaml(Nested);

            Assert.That.HasCount(6,
                                 tree.AllElements(),
                                 because: "the file declares six elements, and a rule counting controls must not see a node twice",
                                 fix: BuilderFix);
        }

        [TestMethod]
        public void Deep_Nesting_Does_Not_Multiply_The_Node_Count()
        {
            const int depth = 16;

            var open = string.Concat(Enumerable.Repeat("<Grid>", depth));
            var close = string.Concat(Enumerable.Repeat("</Grid>", depth));
            var xaml = $"""<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">{open}<TextBlock />{close}</UserControl>""";

            var tree = ParseXaml(xaml);

            Assert.That.HasCount(depth + 2,
                                 tree.AllElements(),
                                 because: "a file with eighteen elements has eighteen nodes however deeply they nest, "
                                          + "and the old builder produced 131072 of them here",
                                 fix: BuilderFix);
        }

        [TestMethod]
        public void Every_Element_Knows_Its_Parent()
        {
            var tree = ParseXaml(Nested);
            var textBlock = tree.OfTypeName("TextBlock").Single();

            Assert.That.IsNull(tree.Root.Parent,
                               because: "the document element is declared in nothing",
                               fix: BuilderFix);

            Assert.That.AreEqual<string>(["Grid", "Border", "StackPanel", "Grid", "UserControl"],
                                         textBlock.Ancestors().Select(a => a.TypeName),
                                         because: "walking up from an element is how a rule reports where a violation sits, "
                                                  + "and the old builder passed null as the parent everywhere",
                                         fix: BuilderFix);
        }

        [TestMethod]
        public void Property_Elements_Are_Properties_Rather_Than_Children()
        {
            var tree = ParseFragment("""
                <Grid>
                  <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition />
                  </Grid.RowDefinitions>
                  <Button />
                </Grid>
                """);

            var grid = tree.OfTypeName("Grid").Single();

            Assert.That.AreEqual<string>(["Button"],
                                         grid.Children.Select(c => c.TypeName),
                                         because: "XAML treats <Grid.RowDefinitions> as a property of the Grid, not as a child of it, "
                                                  + "and the old parser turned it into a control named Grid.RowDefinitions",
                                         fix: "Check ElementBuilder.IsPropertyElement and ToPropertyElement");

            var rowDefinitions = grid["Grid.RowDefinitions"] as PropertyElement;

            Assert.That.IsNotNull(rowDefinitions,
                                  because: "the property element must be reachable by its full qualified name",
                                  fix: "Check the dotted name branch of the ElementBase indexer");

            Assert.That.HasCount(2,
                                 rowDefinitions.Children,
                                 because: "both row definitions are written inside the property element",
                                 fix: "Check ElementBuilder.ToPropertyElement");

            Assert.That.AreEqual("Grid",
                                 rowDefinitions.Children[0].Parent?.TypeName,
                                 because: "a row definition is declared in the Grid, so that is what it must point back to",
                                 fix: "Check ElementBuilder.Reparent, it must cover property element children too");
        }

        [TestMethod]
        public void Text_Content_Is_Kept()
        {
            var tree = ParseFragment("<Button>Click me</Button>");

            Assert.That.AreEqual("Click me",
                                 tree.OfTypeName("Button").Single().Content,
                                 because: "the caption of a button written as content is what a rule about hard coded text looks at, "
                                          + "and the old parser dropped it",
                                 fix: "Check ElementBuilder.ContentOf");

            Assert.That.IsNull(tree.OfTypeName("UserControl").Single().Content,
                               because: "an element with no text of its own must not report the text of its children",
                               fix: "Read only the direct XText nodes, not the descendant text");
        }
    }
}
