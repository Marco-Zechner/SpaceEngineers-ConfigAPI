using System.Collections.Generic;
using System.Xml.Serialization;
using MarcoZechner.ConfigAPI.Shared.Abstractions;
using mz.SemanticVersioning; //TODO check if needed

namespace MarcoZechner.ConfigAPI.Shared.Domain
{
    /// <summary>
    /// Variables in your config should either be ALL public properties with getters and setters, or ALL public fields.
    /// Mixing fields and properties can lead to the XML serializer reordering stuff.
    /// </summary>
    public abstract class ConfigBase
    {
        [XmlIgnore]
        public abstract SemanticVersion ConfigVersion { get; }
        
        [XmlElement("ConfigVersion")]
        public string ConfigVersionSerialized
        {
            get { return ConfigVersion; }
            set { /* Needed for XML serialization */ }
        }

        public virtual string DefaultNameOverride => GetType().Name;
        
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
        
        // These will be wired by client layer; in tests you can replace the backend
        internal static IConfigClientBackend ClientBackend { get; set; }

        public bool TryLoad(string presetName = null)
        {
            var backend = ClientBackend;
            return backend != null && backend.TryLoad(this, presetName ?? DefaultNameOverride);
        }

        public void Save(string presetName = null)
        {
            var backend = ClientBackend;
            backend?.Save(this, presetName ?? DefaultNameOverride);
        }
        
        [XmlIgnore]
        public string CurrentFile {
            get
            {
                var backend = ClientBackend;
                return backend?.GetCurrentFileName(this);
            }
        }
    }
}