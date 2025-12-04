using System;
using System.Text.RegularExpressions;
using mz.Config.Abstractions.SE;
using mz.Config.Core;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using Sandbox.ModAPI;

namespace mz.Config.SeImpl
{
    public class ConfigXmlSerializer : IConfigXmlSerializer
    {
        public string SerializeToXml(ConfigBase config)
        {
            return MyAPIGateway.Utilities.SerializeToXML(config);
        }
        
        private const string INVALID_ENUM_PATTERN = @"'(?<value>[^']+)' is not a valid value for (?<enum>\w+)";

        public T DeserializeFromXml<T>(string xml) where T :
            ConfigBase, new()
        {
            try
            {
                return MyAPIGateway.Utilities.SerializeFromXML<T>(xml);
            }
            catch (Exception ex) // catch invalid enum deserialization
            {
                var message = ex.InnerException?.Message;
                if (message == null) throw;
                var match = Regex.Match(message, INVALID_ENUM_PATTERN);
                ConfigStorage.Debug?.Log(message);
                if (!match.Success) throw;
                
                var enumName = match.Groups["enum"].Value;
                var attemptedValue = match.Groups["value"].Value;
                ConfigStorage.Debug?.Log(
                    $"Deserialization error in ConfigType {typeof(T).Name}: '{attemptedValue}' is not a valid value for enum '{enumName}'.");
                return null;
            }
        }
    }
}
