using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Xaml;
using static Solution.Parser.Xaml.Test.ParseHelper;

namespace Solution.Parser.Xaml.Test
{
    /// <summary>
    /// Attribute values. The old parser decided what a value was by searching it for a substring and
    /// then split it on commas, which cost two whole files to an exception and misread several more.
    /// </summary>
    [TestClass]
    public class MarkupExtensionTest
    {
        private const string TokenizerFix =
            "Check MarkupExtensionTokenizer, a value is markup only when it starts with an unescaped brace, "
            + "and its arguments split on top level commas";

        private static PropertyValue? ValueOf(string attributes, string propertyName)
        {
            return ParseFragment($"<TextBlock {attributes} />").OfTypeName("TextBlock").Single()[propertyName]?.PropertyValue;
        }

        [TestMethod]
        public void A_Colon_In_Plain_Text_Is_Not_Markup()
        {
            var value = ValueOf("""ToolTip="Note: press F1" """, "ToolTip");

            Assert.That.AreEqual("Note: press F1",
                                 value?.ValueText,
                                 because: "a colon in a caption is text, and the old parser took the whole file down over it",
                                 fix: TokenizerFix);

            Assert.That.IsNotOfType<MarkupExtension>(value,
                                                     because: "nothing about this value is a markup extension",
                                                     fix: TokenizerFix);
        }

        [TestMethod]
        public void A_Comma_Inside_A_Format_String_Does_Not_Split_The_Binding()
        {
            var value = ValueOf("""Text="{Binding Amount, StringFormat={0:#,##0.00}}" """, "Text");
            var binding = value as Binding;

            Assert.That.IsNotNull(binding,
                                  because: "the value is a binding, and the old parser threw IndexOutOfRange on the format string's comma",
                                  fix: TokenizerFix);

            Assert.That.AreEqual("Amount",
                                 binding.Path?.ValueText,
                                 because: "the path is the first positional argument",
                                 fix: TokenizerFix);

            Assert.That.AreEqual("{0:#,##0.00}",
                                 binding.StringFormat?.ValueText,
                                 because: "the format string keeps its grouping separator",
                                 fix: TokenizerFix);
        }

        [TestMethod]
        public void The_Brace_Escape_Marks_A_Literal_Rather_Than_Markup()
        {
            var value = ValueOf("""ContentStringFormat="{}{0:N2}" """, "ContentStringFormat");

            Assert.That.IsOfType<StringFormat>(value,
                                               because: "the {} prefix is XAML's escape for a literal, "
                                                        + "and the old ordering made this a MarkupExtension named N2",
                                               fix: "Check that PropertyValueParser handles the escape before it looks for markup");

            Assert.That.AreEqual("{0:N2}",
                                 value?.ValueText,
                                 because: "the escape prefix is not part of the format string",
                                 fix: "Check MarkupExtensionTokenizer.Unescape");
        }

        [TestMethod]
        public void Only_The_First_Equals_Sign_Separates_Name_From_Value()
        {
            var binding = ValueOf("""Text="{Binding Path=A=B}" """, "Text") as Binding;

            Assert.That.AreEqual("A=B",
                                 binding?.Path?.ValueText,
                                 because: "everything after the first equals sign is the value, and the old split dropped the rest",
                                 fix: TokenizerFix);
        }

        [TestMethod]
        public void A_Nested_Extension_Keeps_Its_Own_Shape()
        {
            var binding = ValueOf("""Text="{Binding Amount, Converter={StaticResource MoneyConverter}}" """, "Text") as Binding;

            Assert.That.IsNotNull(binding,
                                  because: "the outer value is a binding",
                                  fix: TokenizerFix);

            Assert.That.AreEqual("MoneyConverter",
                                 (binding.Converter as StaticResource)?.ResourceKey,
                                 because: "a converter reached through a resource key is exactly what a rule about converters looks for",
                                 fix: "Nested arguments must go back through the value parser");
        }

        [TestMethod]
        public void RelativeSource_Keeps_Its_Mode_And_Ancestor_Apart()
        {
            var binding = ValueOf("""Text="{Binding Value, RelativeSource={RelativeSource AncestorType=Window, Mode=FindAncestor}}" """, "Text") as Binding;
            var relativeSource = binding?.RelativeSource;

            Assert.That.IsNotNull(relativeSource,
                                  because: "the binding names a relative source",
                                  fix: "Check RelativeSourceParser");

            Assert.That.AreEqual("FindAncestor",
                                 relativeSource.Mode,
                                 because: "the old parser took the last space separated word and reported Mode=FindAncestor} as the mode",
                                 fix: "Read the mode from the Mode argument or the positional one");

            Assert.That.AreEqual("Window",
                                 relativeSource.AncestorType,
                                 because: "which ancestor is searched for is the point of the extension",
                                 fix: "Check RelativeSourceParser");
        }

        [TestMethod]
        public void RelativeSource_Self_Is_Read_From_The_Positional_Argument()
        {
            var binding = ValueOf("""Text="{Binding Value, RelativeSource={RelativeSource Self}}" """, "Text") as Binding;

            Assert.That.AreEqual("Self",
                                 binding?.RelativeSource?.Mode,
                                 because: "the shorthand puts the mode in the positional argument",
                                 fix: "Check RelativeSourceParser.Mode");
        }

        [TestMethod]
        public void XStatic_Reports_The_Member_It_Reads()
        {
            var value = ValueOf("""Text="{x:Static local:Texts.Title}" """, "Text");
            var xStatic = value as XStaticMarkupExtension;

            Assert.That.IsNotNull(xStatic,
                                  because: "the old parser returned an XTypeMarkupExtension here, so XStaticMarkupExtension was unreachable",
                                  fix: "Check XStaticParser");

            Assert.That.AreEqual("Texts",
                                 xStatic.Type,
                                 because: "the declaring type is named before the dot, with the prefix stripped",
                                 fix: "Check XStaticParser");

            Assert.That.AreEqual("Title",
                                 xStatic.Member,
                                 because: "the member is the point of the extension",
                                 fix: "Check XStaticParser");
        }

        [TestMethod]
        public void XNull_Only_Matches_The_Extension_Itself()
        {
            Assert.That.IsOfType<NullExtension>(ValueOf("""Tag="{x:Null}" """, "Tag"),
                                                because: "an explicit null must be distinguishable from an unset property",
                                                fix: "Check XNullParser");

            Assert.That.IsNotOfType<NullExtension>(ValueOf("""ToolTip="pass x:Null to reset" """, "ToolTip"),
                                                   because: "the old predicate matched any value merely containing the text x:Null",
                                                   fix: "Match on the tokenised extension name, not on a substring");
        }

        [TestMethod]
        public void An_Empty_Binding_Binds_To_The_Data_Context_Itself()
        {
            var binding = ValueOf("""Text="{Binding}" """, "Text") as Binding;

            Assert.That.AreEqual(".",
                                 binding?.Path?.ValueText,
                                 because: "the bare form binds to the data context, which XAML writes as a dot",
                                 fix: "Check BindingParser.Path");
        }

        [TestMethod]
        public void An_Unknown_Extension_Keeps_Its_Name_And_Arguments()
        {
            var value = ValueOf("""Tag="{local:Translate Key=Save, Fallback=Speichern}" """, "Tag");
            var markup = value as MarkupExtension;

            Assert.That.AreEqual("Translate",
                                 markup?.Name,
                                 because: "a custom extension is still readable even though the parser has no model for it",
                                 fix: "Check GenericMarkupExtensionParser");

            Assert.That.AreEqual("Save",
                                 markup?["Key"]?.PropertyValue?.ValueText,
                                 because: "its arguments are what a rule over a custom extension inspects",
                                 fix: "Check MarkupTokenExtensions.ToProperties");
        }

        [TestMethod]
        public void Malformed_Markup_Costs_A_Diagnostic_Rather_Than_The_File()
        {
            var tree = ParseFragment("""<TextBlock Text="{Binding Foo" ToolTip="fine" />""");
            var textBlock = tree.OfTypeName("TextBlock").Single();

            Assert.That.IsTrue(tree.HasDiagnostics,
                               because: "an unbalanced brace is worth reporting",
                               fix: "Check PropertyValueParser, it reports MalformedMarkupExtension when tokenising fails");

            Assert.That.AreEqual("fine",
                                 textBlock["ToolTip"]?.PropertyValue?.ValueText,
                                 because: "one broken attribute must not cost the rest of the file",
                                 fix: "Collect diagnostics instead of throwing out of the value parser");

            Assert.That.IsOfType<UnknownPropertyValue>(textBlock["Text"]?.PropertyValue,
                                                       because: "the raw text is kept so a rule can still report what was written",
                                                       fix: "Return UnknownPropertyValue when tokenising fails");
        }

        [TestMethod]
        public void A_Well_Formed_File_Reports_No_Diagnostics()
        {
            var tree = ParseFragment("""
                <StackPanel>
                  <TextBlock Text="{Binding Amount, StringFormat={0:#,##0.00}, Converter={StaticResource C}}" ToolTip="Note: press F1" />
                  <Button Content="{x:Static local:Texts.Save}" Tag="{x:Null}" Margin="0,0,5,0" Width="12.5" />
                </StackPanel>
                """);

            Assert.That.HasCount(0,
                                 tree.Diagnostics,
                                 because: "everything in this fragment is ordinary markup a parser must handle",
                                 fix: "Read the reported diagnostic, it names the value the tokenizer choked on");
        }
    }
}
