using System;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;

namespace mz.Config.Abstractions
{
    /// <summary>
    /// Metadata and creation/deserialization logic for a single config type.
    /// Typically implemented as ConfigDefinition&lt;T&gt;.
    /// </summary>
    public interface IConfigDefinition
    {
        /// <summary>
        /// Logical type name used as identifier, usually typeof(T).Name.
        /// Also used as XML root and as section name in simple formats.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// The CLR type of the config.
        /// </summary>
        Type ConfigType { get; }

        /// <summary>
        /// Create a new instance with current default values (from code).
        /// </summary>
        ConfigBase CreateDefaultInstance();

        /// <summary>
        /// Deserialize an XML string into a ConfigBase instance using the
        /// provided XML serializer (SE wrapper).
        /// </summary>
        ConfigBase DeserializeFromXml(IConfigXmlSerializer xmlSerializer, string xml);
    }
}