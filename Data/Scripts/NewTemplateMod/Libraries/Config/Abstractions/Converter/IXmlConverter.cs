namespace mz.Config.Abstractions.Converter
{
    /// <summary>
    /// Converts between internal XML representation and an external file format.
    /// The external format can be XML itself (identity) or TOML, JSON, etc.
    ///
    /// IMPORTANT:
    /// - This interface does NOT know about defaults or layout migration.
    /// - It always operates on a single config document (one type, one instance).
    /// - Layout/default handling is done separately by the layout migrator.
    /// </summary>
    public interface IXmlConverter
    {
        /// <summary>
        /// Convert an internal XML document (for a single config instance)
        /// to the external format that is written to disk.
        /// </summary>
        string ToExternal(IConfigDefinition definition, string xmlContent);

        /// <summary>
        /// Convert external format (read from disk) back into the internal XML
        /// document for a single config instance.
        /// </summary>
        string ToInternal(IConfigDefinition definition, string externalContent);
    }
}