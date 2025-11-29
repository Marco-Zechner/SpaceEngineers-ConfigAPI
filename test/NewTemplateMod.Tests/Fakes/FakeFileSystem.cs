using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace NewTemplateMod.Tests
{
    public class FakeFileSystem : IConfigFileSystem
    {
        private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        public bool TryReadFile(ConfigLocationType location, string fileName, out string content)
        {
            string key = MakeKey(location, fileName);
            if (_files.TryGetValue(key, out content))
                return true;

            content = null;
            return false;
        }

        public void WriteFile(ConfigLocationType location, string fileName, string content)
        {
            string key = MakeKey(location, fileName);
            _files[key] = content;
        }

        public string GetDefaultFileName(IConfigDefinition definition)
        {
            return definition.TypeName + "Default.toml";
        }

        private static string MakeKey(ConfigLocationType location, string fileName)
        {
            return ((int)location).ToString() + "|" + fileName;
        }
    }
}
