using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Xaml;
using static Solution.Parser.Xaml.Test.ParseHelper;

namespace Solution.Parser.Xaml.Test
{
    /// <summary>
    /// The element types the parser recognises. No builder ever produced a <c>Style</c> or a
    /// <c>Trigger</c> before, so both were unreachable and the <c>Styles</c> list was always empty.
    /// </summary>
    [TestClass]
    public class ElementKindTest
    {
        private const string Resources =
            """
            <UserControl.Resources>
              <Style x:Key="OkButton" TargetType="Button" BasedOn="{StaticResource BaseButton}">
                <Setter Property="Background" Value="Red" />
                <Style.Triggers>
                  <DataTrigger Binding="{Binding IsBusy}" Value="True">
                    <Setter Property="Opacity" Value="0.5" />
                  </DataTrigger>
                </Style.Triggers>
              </Style>
              <DataTemplate x:Key="RowTemplate" DataType="{x:Type local:Row}">
                <TextBlock Text="{Binding Name}" />
              </DataTemplate>
            </UserControl.Resources>
            """;

        [TestMethod]
        public void A_Style_Is_A_Style()
        {
            var tree = ParseFragment(Resources);

            Assert.That.HasCount(1,
                                 tree.AllStyles(),
                                 because: "the file declares one style, and the old parser produced none at all",
                                 fix: "Check ElementBuilder.CreateEmpty");

            var style = tree.AllStyles().Single();

            Assert.That.AreEqual("OkButton",
                                 style.XKey,
                                 because: "the resource key is how a style is referred to",
                                 fix: "Check ElementBuilder, XKey comes from the x:Key directive");

            Assert.That.AreEqual("Button",
                                 style.TargetTypeName,
                                 because: "a rule about styling a control type reads the target type",
                                 fix: "Check Style.TargetTypeName");

            Assert.That.AreEqual("BaseButton",
                                 style.BasedOn,
                                 because: "style inheritance is resolved through the resource key",
                                 fix: "Check Style.BasedOn");
        }

        [TestMethod]
        public void Setters_And_Triggers_Are_Reachable_From_The_Style()
        {
            var style = ParseFragment(Resources).AllStyles().Single();

            Assert.That.HasCount(1,
                                 style.Setters,
                                 because: "the style sets one property directly, the trigger's setter belongs to the trigger",
                                 fix: "Check Style.Setters");

            Assert.That.AreEqual("Background",
                                 style.Setters.Single().PropertyName,
                                 because: "which property a setter writes is the point of it",
                                 fix: "Check Setter.PropertyName");

            Assert.That.HasCount(1,
                                 style.Triggers,
                                 because: "triggers are written in a <Style.Triggers> property element and must still be reachable",
                                 fix: "Check Style.Triggers, it reads through the property element");

            Assert.That.AreEqual("DataTrigger",
                                 style.Triggers.Single().TypeName,
                                 because: "which flavour of trigger it is reads from the type name",
                                 fix: "Check ElementBuilder.CreateEmpty");

            Assert.That.AreEqual("Opacity",
                                 style.Triggers.Single().Setters.Single().PropertyName,
                                 because: "a trigger's setters belong to the trigger",
                                 fix: "Check Trigger.Setters");
        }

        [TestMethod]
        public void A_Template_Is_A_Template()
        {
            var tree = ParseFragment(Resources);
            var template = tree.AllDataTemplates().Single();

            Assert.That.AreEqual("RowTemplate",
                                 template.XKey,
                                 because: "the template is declared under a resource key",
                                 fix: "Check ElementBuilder");

            Assert.That.AreEqual("Row",
                                 template.TargetTypeName,
                                 because: "which type a data template renders is what a rule pairs against a view model",
                                 fix: "Check Template.TargetTypeName");
        }

        [TestMethod]
        public void Resources_Read_Through_The_Property_Element()
        {
            var tree = ParseFragment(Resources);

            Assert.That.HasCount(2,
                                 tree.Root.Resources,
                                 because: "the style and the template are both declared in <UserControl.Resources>",
                                 fix: "Check ElementBase.Resources");
        }

        [TestMethod]
        public void Resources_Read_Through_A_Nested_Resource_Dictionary_The_Same_Way()
        {
            var tree = ParseFragment("""
                <UserControl.Resources>
                  <ResourceDictionary>
                    <Style x:Key="OkButton" TargetType="Button" />
                  </ResourceDictionary>
                </UserControl.Resources>
                """);

            Assert.That.HasCount(1,
                                 tree.Root.Resources,
                                 because: "both spellings of a resources block mean the same thing to the author",
                                 fix: "Check ElementBase.Resources, it flattens a lone nested ResourceDictionary");

            Assert.That.IsOfType<Style>(tree.Root.Resources.Single(),
                                        because: "the style is what was declared",
                                        fix: "Check ElementBuilder.CreateEmpty");
        }

        [TestMethod]
        public void The_Document_Element_Decides_The_Root_Type()
        {
            var window = ParseXaml("""
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        x:Class="My.Sample.MainWindow" />
                """).Root;

            Assert.That.IsOfType<Window>(window,
                                         because: "the document element is a Window",
                                         fix: "Check ElementBuilder.CreateEmptyRoot");

            Assert.That.IsNotOfType<UserControl>(window,
                                                 because: "a window is not a user control, which is what the old hierarchy claimed",
                                                 fix: "Window and UserControl are siblings below Root");

            Assert.That.AreEqual("My.Sample.MainWindow",
                                 window.FullQualifiedName,
                                 because: "x:Class names the code behind type a rule pairs the markup with",
                                 fix: "Check ElementBuilder.BuildRoot");

            Assert.That.AreEqual(RootKind.Window,
                                 window.RootKind,
                                 because: "switching on the root kind is the alternative to comparing type names",
                                 fix: "Check Root.RootKind");
        }

        [TestMethod]
        public void A_Resource_Dictionary_File_Falls_Back_To_Its_File_Name()
        {
            var root = ParseXaml("""<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" />""").Root;

            Assert.That.IsOfType<ResourceDictionaryRoot>(root,
                                                         because: "the document element is a ResourceDictionary",
                                                         fix: "Check ElementBuilder.CreateEmptyRoot");

            Assert.That.AreEqual("Sample",
                                 root.FullQualifiedName,
                                 because: "a dictionary declares no x:Class, so its file name is the only name it has",
                                 fix: "Check XamlParser.FallbackName");
        }
    }
}
