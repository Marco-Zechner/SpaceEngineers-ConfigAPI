using System.Collections.Generic;
using System.Xml.Serialization;

namespace MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain
{
    /// <summary>
    /// Variables in your config should either be ALL public properties with getters and setters, or ALL public fields.
    /// Mixing fields and properties can lead to the XML serializer reordering stuff.
    /// </summary>
    public abstract class ConfigBase
    {
        [XmlIgnore]
        public abstract string ConfigVersion { get; }
        
        [XmlElement("ConfigVersion")]
        public string ConfigVersionSerialized
        {
            get { return ConfigVersion; }
            set { /* Needed for XML serialization */ }
        }

        public virtual string ConfigNameOverride => GetType().Name;
        
        // Name -> description, per *property* name
        public virtual IReadOnlyDictionary<string, string> VariableDescriptions
            => _emptyDescriptions;

        private static readonly IReadOnlyDictionary<string, string> _emptyDescriptions
            = new Dictionary<string, string>();

        /// <summary>
        /// Called when the framework creates a default instance.
        /// Implementations should apply default values to collections etc.
        /// Simple types like int, bool, string can be initialized inline, but Collections cause issues with XML serialization.
        /// So these should be initialized here. (Simple types can also be initialized here if desired.)
        /// </summary>
        public virtual void ApplyDefaults()
        {
            
        }
    }
}