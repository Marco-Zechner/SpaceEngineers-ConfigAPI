using mz.Config.Abstractions;
using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using mz.SemanticVersioning;
using NUnit.Framework;

namespace NewTemplateMod.Tests.XmlLayoutTests
{
    [TestFixture]
    public class XmlLayoutMigratorIntegrationTests
    {
        private ConfigLayoutMigrator _migrator;
        private IConfigDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _migrator = new ConfigLayoutMigrator();
            _definition = new ConfigDefinition<CollectionConfigLayoutTest>();
        }

        private static string NormalizeNewlines(string s)
        {
            return s?.Replace("\r\n", "\n").Trim();
        }

        // Simple test config only for this test; it is never instantiated.
        private class CollectionConfigLayoutTest : ConfigBase
        {
            public override SemanticVersion ConfigVersion => "0.3.0";
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
                _definition,
                xmlInput,
                xmlInput,  // old defaults (identical)
                xmlInput); // current defaults (identical)

            var xml1 = result1.NormalizedXml;

            // Second normalization on its own output must be *idempotent*.
            var result2 = _migrator.Normalize(
                _definition,
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
