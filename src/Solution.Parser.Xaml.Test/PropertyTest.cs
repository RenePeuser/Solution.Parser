using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Xaml;
using static Solution.Parser.Xaml.Test.ParseHelper;

namespace Solution.Parser.Xaml.Test
{
    [TestClass]
    public class PropertyTest
    {
        [TestMethod]
        public void A_Directive_Is_Distinguishable_From_A_Property_Of_The_Same_Name()
        {
            var textBlock = ParseFragment("""<TextBlock x:Name="Title" Name="Other" />""")
                .OfTypeName("TextBlock").Single();

            var directive = textBlock.Properties.Single(p => p.IsXamlDirective);
            var plain = textBlock.Properties.Single(p => !p.IsXamlDirective);

            Assert.That.AreEqual("x",
                                 directive.Prefix,
                                 because: "the old parser dropped the prefix, so both attributes were simply called Name",
                                 fix: "Check XamlPropertyParser.PrefixOf and Property.IsXamlDirective");

            Assert.That.AreEqual("Title",
                                 directive.PropertyValue?.ValueText,
                                 because: "the directive carries the x:Name value",
                                 fix: "Check XamlPropertyParser");

            Assert.That.AreEqual("Other",
                                 plain.PropertyValue?.ValueText,
                                 because: "the plain Name property is a different property and keeps its own value",
                                 fix: "Check XamlPropertyParser");

            Assert.That.AreEqual("Title",
                                 textBlock.XName,
                                 because: "x:Name wins over a plain Name, which is the order WPF resolves them in",
                                 fix: "Check ElementBuilder.NameOf");
        }

        [TestMethod]
        public void An_Attached_Property_Is_Named_Like_Any_Other_Property()
        {
            var button = ParseFragment("""<Button Grid.Row="1" />""").OfTypeName("Button").Single();
            var attached = button.AttachedProperties.Single();

            Assert.That.AreEqual("Row",
                                 attached.Name,
                                 because: "the old parser called it RowProperty, so a lookup by the written name never matched",
                                 fix: "Check XamlPropertyParser, Name is the part behind the dot");

            Assert.That.AreEqual("Grid",
                                 attached.OwnerTypeName,
                                 because: "the owning type is what tells Grid.Row from Canvas.Row",
                                 fix: "Check XamlPropertyParser");

            Assert.That.AreEqual("Grid.Row",
                                 attached.FullQualifiedName,
                                 because: "the full qualified name must read like the attribute is written",
                                 fix: "Check AttachedProperty.FullQualifiedName");

            Assert.That.AreEqual("RowProperty",
                                 attached.DependencyPropertyName,
                                 because: "the backing field name is still available for whoever needs it",
                                 fix: "Check AttachedProperty.DependencyPropertyName");

            Assert.That.IsNotNull(button["Grid.Row"],
                                  because: "looking an attached property up by how it is written is the obvious thing to do, "
                                           + "and it returned nothing before",
                                  fix: "Check the dotted name branch of the ElementBase indexer");
        }

        [TestMethod]
        public void Xmlns_Declarations_Are_Collected_On_The_Root()
        {
            var tree = ParseFragment("<Grid />");
            var local = tree.Xmlns("local");

            Assert.That.IsNotNull(local,
                                  because: "mapping a prefix back to a CLR namespace needs the declaration",
                                  fix: "Check ElementBuilder.BuildRoot");

            Assert.That.AreEqual("local",
                                 local.Alias,
                                 because: "the alias was always empty before, because the prefix never reached the value parser",
                                 fix: "Check XamlPropertyParser.ToXamlUsing");

            Assert.That.AreEqual("My.Sample",
                                 local.Namespace,
                                 because: "the CLR namespace is what a rule resolving local: types needs",
                                 fix: "Check XamlPropertyParser.ToXamlUsing");

            Assert.That.AreEqual("My.Sample",
                                 local.Assembly,
                                 because: "the assembly is named after the semicolon",
                                 fix: "Check XamlPropertyParser.ToXamlUsing");

            Assert.That.IsFalse(tree.Root.Properties.Any(p => p.Name == "local"),
                                because: "a namespace declaration is not a property of the element",
                                fix: "Filter IsNamespaceDeclaration out of the property list");
        }

        [TestMethod]
        public void A_Data_Context_Is_Only_Reported_When_It_Is_Set()
        {
            var tree = ParseFragment("""
                <Grid DataContext="{x:Static local:Locator.Main}">
                  <Button />
                </Grid>
                """);

            Assert.That.IsNull(tree.OfTypeName("Button").Single().DataContext,
                               because: "the button sets no data context, and the old parser gave every element an empty one",
                               fix: "Check ElementBuilder.ToDataContext, a missing property means null");

            Assert.That.IsNull(tree.Root.DataContext,
                               because: "the user control sets no data context either",
                               fix: "Check ElementBuilder.ToDataContext");

            Assert.That.IsNotNull(tree.OfTypeName("Grid").Single().DataContext,
                                  because: "the Grid does set one",
                                  fix: "Check ElementBuilder.ToDataContext");
        }

        [TestMethod]
        public void A_Data_Context_Set_From_A_Type_Reports_That_Type()
        {
            var tree = ParseFragment("""<Grid DataContext="{x:Type local:MainViewModel}" />""");

            Assert.That.AreEqual("MainViewModel",
                                 tree.OfTypeName("Grid").Single().DataContext?.FullQualifiedName,
                                 because: "a rule pairing a view with its view model reads the type from here",
                                 fix: "Check ElementBuilder.ToDataContext");
        }

        [TestMethod]
        public void A_Location_Points_At_The_Line_The_Element_Is_Written_On()
        {
            var tree = ParseXaml("""
                <UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                  <Grid>
                    <Button />
                  </Grid>
                </UserControl>
                """);

            var button = tree.OfTypeName("Button").Single();

            Assert.That.AreEqual(3,
                                 button.Location.Line,
                                 because: "a rule reports where the violation sits, and line info was parsed but never used",
                                 fix: "Check XamlLocationExtensions.ToXamlLocation");

            Assert.That.AreEqual($"{FilePath}(3,6)",
                                 button.Location.ToString(),
                                 because: "the MSBuild form is what a test runner turns into a clickable link",
                                 fix: "Check XamlLocation.ToString");
        }
    }
}
