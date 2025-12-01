using System;
using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests
{
    public class FakeFileSystem : IConfigFileSystem
    {
        private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        public bool TryReadFile(ConfigLocationType location, string fileName, out string content)
        {
            Debug.Log("TryRead: " + location + "/" + fileName);
            var key = MakeKey(location, fileName);
            return _files.TryGetValue(key, out content);
        }

        public void WriteFile(ConfigLocationType location, string fileName, string content)
        {
            Debug.Log("Write: " + location + "/" + fileName);
            var key = MakeKey(location, fileName);
            _files[key] = content;
        }

        private static string MakeKey(ConfigLocationType location, string fileName)
        {
            return ((int)location) + "|" + fileName;
        }
    }
}