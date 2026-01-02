using MarcoZechner.ConfigAPI.Main.Core.Migrator;
using NUnit.Framework;

namespace NewTemplateMod.Tests.XmlLayoutTests
{
    [TestFixture]
    public class XmlLayoutMigratorIntegrationTests
    {
        private ConfigLayoutMigrator _migrator;

        [SetUp]
        public void SetUp()
        {
            _migrator = new ConfigLayoutMigrator();
        }

        private static string NormalizeNewlines(string s)
        {
            return s?.Replace("\r\n", "\n").Trim();
        }

        [Test]
        public void Normalize_With_No_Logical_Changes_Preserves_Layout_Idempotently()
        {
            const string xmlInput =
                @"<?xml version=""1.0"" encoding=""utf-16""?>
<CollectionConfigLayoutTest xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
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
</CollectionConfigLayoutTest>";

            // First normalization: allowed to change formatting, but not semantics.
            var result1 = _migrator.Normalize(
                "CollectionConfigLayoutTest",
                xmlInput,
                xmlInput,  // old defaults (identical)
                xmlInput); // current defaults (identical)

            var xml1 = result1.NormalizedXml;

            // Second normalization on its own output must be *idempotent*.
            var result2 = _migrator.Normalize(
                "CollectionConfigLayoutTest",
                xml1,
                xml1,
                xml1);

            var xml2 = result2.NormalizedXml;

            var norm1 = NormalizeNewlines(xml1);
            var norm2 = NormalizeNewlines(xml2);

            Assert.That(norm2, Is.EqualTo(norm1),
                "Migrator + LayoutXml must be idempotent; running Normalize twice should not keep changing indent.");
        }
    }
}
