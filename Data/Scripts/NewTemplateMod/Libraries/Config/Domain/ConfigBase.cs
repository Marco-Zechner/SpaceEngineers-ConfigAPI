using System.Collections.Generic;
using System.Xml.Serialization;
using mz.SemanticVersioning;

namespace mz.Config.Domain
{
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

        public virtual string ConfigNameOverride => GetType().Name;
        
        // Name -> description, per *property* name
        public virtual IReadOnlyDictionary<string, string> VariableDescriptions
            => _emptyDescriptions;

        private static readonly IReadOnlyDictionary<string, string> _emptyDescriptions
            = new Dictionary<string, string>();
    }
}