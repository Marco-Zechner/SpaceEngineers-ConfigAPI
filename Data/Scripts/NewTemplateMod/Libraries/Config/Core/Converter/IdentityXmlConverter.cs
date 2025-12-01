using mz.Config.Abstractions;
using mz.Config.Abstractions.Converter;

namespace mz.Config.Core.Converter
{
    /// <summary>
    /// Trivial converter: external format == internal XML.
    /// Useful for XML-only configs or tests.
    /// </summary>
    public sealed class IdentityXmlConverter : IXmlConverter
    {
        public string GetExtension => ".xml";

        public string ToExternal(IConfigDefinition definition, string xmlContent)
        {
            return xmlContent ?? string.Empty;
        }

        public string ToInternal(IConfigDefinition definition, string externalContent)
        {
            return externalContent ?? string.Empty;
        }
    }
}