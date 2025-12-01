using mz.Config.Domain;

namespace mz.Config.Abstractions
{
    /// <summary>
    /// Instance-based abstraction for a config storage engine.
    /// The actual implementation in the mod may be a static ConfigStorage
    /// that fronts these semantics.
    /// </summary>
    public interface IConfigStorage
    {
        /// <summary>
        /// Register a config type for a given location.
        /// This must be called before any other operations for the type.
        /// You can optionally provide an initial file name, otherwise the typename will be used.
        /// Will return the in-memory instance (on create a new one if non exists for this type & location).
        /// </summary>
        T RegisterConfig<T>(ConfigLocationType location, string initialFileName = null) where T : ConfigBase, new();

        /// <summary>
        /// Get or create the in-memory instance for a given type and location.
        /// </summary>
        T GetOrCreate<T>(ConfigLocationType location) where T : ConfigBase, new();

        /// <summary>
        /// Get the current file name for a given type and location.
        /// If none was set yet, a default name is assigned.
        /// </summary>
        string GetCurrentFileName(ConfigLocationType location, string typeName);

        /// <summary>
        /// Set the current file name for a given type and location.
        /// This does NOT load or save, it only changes which file is
        /// considered "current" for subsequent operations.
        /// </summary>
        void SetCurrentFileName(ConfigLocationType location, string typeName, string fileName);

        /// <summary>
        /// Load a config from a given file (external format) into memory.
        /// Handles conversion, layout migration, defaults-history update,
        /// backup creation, and deserialization.
        /// </summary>
        bool Load(ConfigLocationType location, string typeName, string fileName);

        /// <summary>
        /// Save the current in-memory instance of a config to a given file
        /// in external format, and update the per-type defaults file.
        /// </summary>
        bool Save(ConfigLocationType location, string typeName, string fileName);

        /// <summary>
        /// Return the current in-memory config as text in external format,
        /// without writing it to disk. This is used for "show" commands.
        /// </summary>
        string GetConfigAsText(ConfigLocationType location, string typeName);

        /// <summary>
        /// Return the raw contents of a file (external format), or null if
        /// the file cannot be read. This is used for "showfile" commands.
        /// </summary>
        string GetFileAsText(ConfigLocationType location, string fileName);
    }
}