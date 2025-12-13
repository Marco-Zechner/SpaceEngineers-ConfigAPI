using System.Collections.Generic;
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
            var found = _files.TryGetValue(key, out content);
            var text = content != null ? string.Join("\n\t", content.Split('\n')) : "null";
            Logger.Log($"TryRead ({found}): " + location + "/" + fileName + "\n\t" + text);
            return found;
        }

        public void WriteFile(ConfigLocationType location, string fileName, string content)
        {
            var text = content != null ? string.Join("\n\t", content.Split('\n')) : "null";
            Logger.Log("Write: " + location + "/" + fileName + "\n\t" + text);
            var key = MakeKey(location, fileName);
            _files[key] = content;
        }

        private static string MakeKey(ConfigLocationType location, string fileName)
        {
            return (int)location + "|" + fileName;
        }
        
        public bool Exists(ConfigLocationType location, string fileName)
        {
            var key = MakeKey(location, fileName);
            var exists = _files.ContainsKey(key);
            Logger.Log("Exists: " + location + "/" + fileName + " = " + exists);
            return exists;
        }
    }
}