using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Xaml;
using static Solution.Parser.Xaml.Test.ParseHelper;

namespace Solution.Parser.Xaml.Test
{
    [TestClass]
    public class QueryTest
    {
        private const string Markup =
            """
            <UserControl.Resources>
              <Style x:Key="OkButton" TargetType="Button">
                <Setter Property="Background" Value="{StaticResource Accent}" />
              </Style>
            </UserControl.Resources>
            <Grid>
              <TextBlock x:Name="Title" Text="{Binding Header, Converter={StaticResource Upper}}" />
              <Button x:Name="Ok" Content="{Binding OkCaption}" Style="{StaticResource OkButton}" />
            </Grid>
            """;

        [TestMethod]
        public void FindByName_Reaches_A_Named_Element_Anywhere_In_The_File()
        {
            var tree = ParseFragment(Markup);

            Assert.That.AreEqual("Button",
                                 tree.FindByName("Ok")?.TypeName,
                                 because: "naming an element is how markup and code behind refer to each other",
                                 fix: "Check QueryExtensions.FindByName");

            Assert.That.IsNull(tree.FindByName("Missing"),
                               because: "a name the file does not declare must not match anything",
                               fix: "Check QueryExtensions.FindByName");
        }

        [TestMethod]
        public void FindByKey_Reaches_A_Resource()
        {
            Assert.That.IsOfType<Style>(ParseFragment(Markup).FindByKey("OkButton"),
                                        because: "a resource is looked up by its key",
                                        fix: "Check QueryExtensions.FindByKey");
        }

        [TestMethod]
        public void AllBindings_Finds_Bindings_Behind_Property_Elements_Too()
        {
            var tree = ParseFragment(Markup);

            Assert.That.AreEqual<string>(["Header", "OkCaption"],
                                         tree.AllBindings().Select(b => b.Path?.ValueText ?? string.Empty),
                                         because: "a rule over bound paths has to see every binding in the file",
                                         fix: "Check QueryExtensions.AllBindings and the descendant walk");
        }

        [TestMethod]
        public void AllMarkupExtensions_Includes_The_Nested_Ones()
        {
            var keys = ParseFragment(Markup)
                       .AllMarkupExtensions()
                       .OfType<StaticResource>()
                       .Select(r => r.ResourceKey)
                       .Order();

            Assert.That.AreEqual<string>(["Accent", "OkButton", "Upper"],
                                         keys,
                                         because: "a rule checking that every referenced resource exists must see a key "
                                                  + "used inside a binding as well as one used on its own",
                                         fix: "Check QueryExtensions.SelfAndNestedValues");
        }

        [TestMethod]
        public void AllResources_Collects_Every_Resources_Block()
        {
            Assert.That.HasCount(1,
                                 ParseFragment(Markup).AllResources(),
                                 because: "the file declares one resource",
                                 fix: "Check QueryExtensions.AllResources");
        }

        [TestMethod]
        public void OfTypeName_Answers_The_Common_Question_About_A_Control_Type()
        {
            Assert.That.HasCount(1,
                                 ParseFragment(Markup).OfTypeName("Button"),
                                 because: "the file declares one button, the style merely targets the type",
                                 fix: "Check QueryExtensions.OfTypeName");
        }
    }
}
