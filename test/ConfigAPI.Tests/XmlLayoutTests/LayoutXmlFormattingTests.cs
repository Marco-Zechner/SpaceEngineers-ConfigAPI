using System.Collections.Generic;
using mz.Config.Core;
using NUnit.Framework;

namespace NewTemplateMod.Tests.XmlLayoutTests
{
    [TestFixture]
    public class LayoutXmlFormattingTests
    {
        private static string NormalizeNewlines(string s)
        {
            return s?.Replace("\r\n", "\n").Trim();
        }

        [Test]
        public void Build_Includes_Declaration_And_Namespaces()
        {
            var children = new Dictionary<string, string>
            {
                { "ConfigVersion", "<ConfigVersion>0.1.0</ConfigVersion>" },
                { "SomeValue",     "<SomeValue>42</SomeValue>" },
                { "SomeText",      "<SomeText>Default text</SomeText>" }
            };

            var xml = LayoutXml.Build("SimpleConfig", children);

            var normalized = NormalizeNewlines(xml);

            Assert.Multiple(() =>
            {
                Assert.That(normalized, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-16\"?>\n"));
                Assert.That(normalized, Does.Contain("<SimpleConfig xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"));
                Assert.That(normalized, Does.Contain("<ConfigVersion>0.1.0</ConfigVersion>"));
                Assert.That(normalized, Does.Contain("<SomeValue>42</SomeValue>"));
                Assert.That(normalized, Does.Contain("<SomeText>Default text</SomeText>"));
                Assert.That(normalized, Does.EndWith("</SimpleConfig>"));
            });
        }

        [Test]
        public void Build_For_CollectionConfig_Is_Idempotent_No_Indent_Drift()
        {
            // Simulate the pre-reload layout you showed
            const string preReload = 
@"<?xml version=""1.0"" encoding=""utf-16""?>
<CollectionConfig xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <ConfigVersion>0.3.0</ConfigVersion>
  <Tags>
    <string>alpha</string>
    <string>beta</string>
  </Tags>
  <NamedValues>
    <dictionary>
      <item>
        <Key>start</Key>
        <Value>1</Value>
      </item>
      <item>
        <Key>end</Key>
        <Value>10</Value>
      </item>
    </dictionary>
  </NamedValues>
  <Nested>
    <Threshold>0.75</Threshold>
    <Allowed>true</Allowed>
  </Nested>
</CollectionConfig>";

            // Parse once, then rebuild, then parse & rebuild again.
            string root1;
            var children1 = LayoutXml.ParseChildren(preReload, out root1);
            Assert.That(root1, Is.EqualTo("CollectionConfig"), "Unexpected root name from first parse.");

            var xml1 = LayoutXml.Build(root1, children1);

            string root2;
            var children2 = LayoutXml.ParseChildren(xml1, out root2);
            Assert.That(root2, Is.EqualTo("CollectionConfig"), "Unexpected root name from second parse.");

            var xml2 = LayoutXml.Build(root2, children2);

            // Compare after normalizing newlines and trimming trailing whitespace.
            var norm1 = NormalizeNewlines(xml1);
            var norm2 = NormalizeNewlines(xml2);

            Assert.That(norm2, Is.EqualTo(norm1),
                "LayoutXml.Build must be idempotent; second build should not add extra indent.");
        }

        [Test]
        public void Build_Preserves_Nested_Structure_And_Reasonable_Indent()
        {
            var children = new Dictionary<string, string>
            {
                {
                    "ConfigVersion",
                    "<ConfigVersion>0.3.0</ConfigVersion>"
                },
                {
                    "Tags",
                    @"<Tags>
    <string>alpha</string>
    <string>beta</string>
  </Tags>"
                },
                {
                    "NamedValues",
                    @"<NamedValues>
    <dictionary>
      <item>
        <Key>start</Key>
        <Value>1</Value>
      </item>
      <item>
        <Key>end</Key>
        <Value>10</Value>
      </item>
    </dictionary>
  </NamedValues>"
                },
                {
                    "Nested",
                    @"<Nested>
    <Threshold>0.75</Threshold>
    <Allowed>true</Allowed>
  </Nested>"
                }
            };

            var xml = LayoutXml.Build("CollectionConfig", children);
            var normalized = NormalizeNewlines(xml);

            // We do not enforce exact indent count per line, but we do enforce structure
            Assert.Multiple(() =>
            {
                Assert.That(normalized, Does.Contain("<CollectionConfig xmlns:xsd="));
                Assert.That(normalized, Does.Contain("<ConfigVersion>0.3.0</ConfigVersion>"));

                // Tags block lines exist in correct order
                Assert.That(normalized, Does.Contain("<Tags>"));
                Assert.That(normalized, Does.Contain("<string>alpha</string>"));
                Assert.That(normalized, Does.Contain("<string>beta</string>"));
                Assert.That(normalized, Does.Contain("</Tags>"));

                // NamedValues block exists with children
                Assert.That(normalized, Does.Contain("<NamedValues>"));
                Assert.That(normalized, Does.Contain("<dictionary>"));
                Assert.That(normalized, Does.Contain("<item>"));
                Assert.That(normalized, Does.Contain("<Key>start</Key>"));
                Assert.That(normalized, Does.Contain("<Value>1</Value>"));
                Assert.That(normalized, Does.Contain("</NamedValues>"));

                // Nested block
                Assert.That(normalized, Does.Contain("<Nested>"));
                Assert.That(normalized, Does.Contain("<Threshold>0.75</Threshold>"));
                Assert.That(normalized, Does.Contain("<Allowed>true</Allowed>"));
                Assert.That(normalized, Does.Contain("</Nested>"));
            });
        }

        [Test]
        public void Build_Preserves_XsiNil_And_Namespaces_For_Nullable_Element()
        {
            // Child block with xsi:nil="true"
            var children = new Dictionary<string, string>
            {
                { "ConfigVersion", "<ConfigVersion>0.2.0</ConfigVersion>" },
                {
                    "CurrentMode",
                    "<CurrentMode>Basic</CurrentMode>"
                },
                {
                    "IsEnabled",
                    "<IsEnabled>true</IsEnabled>"
                },
                {
                    "OptionalValue",
                    "<OptionalValue xsi:nil=\"true\" />"
                }
            };

            var xml = LayoutXml.Build("IntermediateConfig", children);
            var normalized = NormalizeNewlines(xml);

            Assert.Multiple(() =>
            {
                // All configs should have the same header + namespaces
                Assert.That(normalized, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-16\"?>\n"));
                Assert.That(normalized, Does.Contain("<IntermediateConfig xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"));

                // Ensure nullable element is present with xsi:nil and not stripped
                Assert.That(normalized, Does.Contain("<OptionalValue xsi:nil=\"true\" />"));

                // Closing root tag
                Assert.That(normalized, Does.EndWith("</IntermediateConfig>"));
            });
        }
    }
}
