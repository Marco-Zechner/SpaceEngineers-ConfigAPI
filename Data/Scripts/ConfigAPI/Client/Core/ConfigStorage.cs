using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Client.Core
{
    public class ConfigStorage
    {
        public static T Get<T>(LocationType location, string name = null)
            where T : ConfigBase, new()
        {
            return null;
        }

        public static CfgSync<T> World<T>(string defaultFile = null)
            where T : ConfigBase, new()
        {
            return null;
        }
    }
}