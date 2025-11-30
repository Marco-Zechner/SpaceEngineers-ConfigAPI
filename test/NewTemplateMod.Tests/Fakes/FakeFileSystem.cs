using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;

namespace NewTemplateMod.Tests
{
    public class FakeFileSystem : IConfigFileSystem
    {
        private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        public bool TryReadFile(ConfigLocationType location, string fileName, out string content)
        {
            var key = MakeKey(location, fileName);
            return _files.TryGetValue(key, out content);
        }

        public void WriteFile(ConfigLocationType location, string fileName, string content)
        {
            var key = MakeKey(location, fileName);
            _files[key] = content;
        }

        public string GetDefaultFileName(IConfigDefinition definition)
        {
            return definition.TypeName + "Default.toml";
        }

        private static string MakeKey(ConfigLocationType location, string fileName)
        {
            return ((int)location) + "|" + fileName;
        }
    }
}
