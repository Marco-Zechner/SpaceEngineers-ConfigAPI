using mz.SemanticVersioning;

namespace mz.Config
{
    /// <summary>
    /// Base type for all config classes. 
    /// Users only inherit this; no extra code needed.
    /// </summary>
    public abstract class ConfigBase 
    {
        /// <summary>
        /// Optional override for filename (without extension).
        /// Default = class name.
        /// </summary>
        public virtual string ConfigNameOverride { get; }
        /// <summary>
        /// Code version of this config definition.
        /// Example: public override SemanticVersion ConfigVersion => "0.1.0";
        /// </summary>
        public abstract SemanticVersion ConfigVersion { get; }

        /// <summary>
        /// DO NOT MODIFY MANUALLY.
        /// This is written by ConfigStorage and used for version checks.
        /// </summary>
        public string StoredVersion { get; set; }

        /// <summary>
        /// DO NOT MODIFY MANUALLY.
        /// Hash of (StoredVersion + '|' + filename), used to detect tampering.
        /// </summary>
        public string StoredVersionHash { get; set; }

    }
}