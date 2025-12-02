using System.Xml.Serialization;

namespace mz.Config.Domain
{
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
    }
}