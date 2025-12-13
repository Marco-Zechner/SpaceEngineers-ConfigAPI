using mz.Config.Domain;

namespace mz.Config.Abstractions.SE
{
    /// <summary>
    /// Abstracts file access for configs. Implementation decides where and how
    /// files are stored in Space Engineers' folders.
    /// </summary>
    public interface IConfigFileSystem
    {
        /// <summary>
        /// Try to read a text file for a given location and file name.
        /// Returns true on success and outputs the content.
        /// </summary>
        bool TryReadFile(ConfigLocationType location, string fileName, out string content);

        /// <summary>
        /// Write a text file for a given location and file name, overwriting
        /// any existing contents.
        /// </summary>
        void WriteFile(ConfigLocationType location, string fileName, string content);

        /// <summary>
        /// Check if a file exists at the given location with the given file name.
        /// </summary>
        bool Exists(ConfigLocationType location, string fileName);
    }
}