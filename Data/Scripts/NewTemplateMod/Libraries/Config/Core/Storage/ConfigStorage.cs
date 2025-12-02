using System;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Converter;
using mz.Config.Abstractions.Layout;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Converter;
using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using mz.Config.SeImpl;

namespace mz.Config.Core
{
    public static class ConfigStorage
    {
        private static bool _initialized;
        public static IDebug Debug { get; set; }

        public static void CustomInitialize(
            IConfigLayoutMigrator layoutMigrator,
            IXmlConverter xmlConverter)
        {
            if (_initialized)
                throw new InvalidOperationException("ConfigStorage has already been initialized.");

            IConfigFileSystem fileSystem = new ConfigFileSystem();
            IConfigXmlSerializer xmlSerializer = new ConfigXmlSerializer();
            
            InternalConfigStorage.Initialize(fileSystem, xmlSerializer, layoutMigrator, xmlConverter);
            _initialized = true;
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            var layout = new ConfigLayoutMigrator();
            var converter = new TomlXmlConverter();
            CustomInitialize(layout, converter);
            _initialized = true;
        }

        public static T Load<T>(ConfigLocationType location, string fileName = null)
            where T : ConfigBase, new()
        {
            EnsureInitialized();

            //TODO: handle invalid fileName when provided
            if (fileName != null)
            {
                if (string.IsNullOrEmpty(fileName) || string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException(nameof(fileName));
            }
            
            InternalConfigStorage.Register<T>(location, fileName);
            var typeName = typeof(T).Name;
            Debug?.Log("Loading config of type " + typeName + " from location " + location, "ConfigStorage.Load");
            var currentFile = InternalConfigStorage.GetCurrentFileName(location, typeName);
            Debug?.Log("Using file name: " + (fileName ?? currentFile), "ConfigStorage.Load");
            InternalConfigStorage.Load(location, typeName, fileName ?? currentFile);
            return InternalConfigStorage.GetOrCreate<T>(location);
        }

        public static void Save<T>(ConfigLocationType location, string fileName = null)
            where T : ConfigBase, new()
        {
            EnsureInitialized();
            
            //TODO: handle invalid fileName when provided
            if (fileName != null)
            {
                if (string.IsNullOrEmpty(fileName) || string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException(nameof(fileName));
            }
            
            InternalConfigStorage.Register<T>(location, fileName);
            var typeName = typeof(T).Name;
            Debug?.Log("Saving config of type " + typeName + " to location " + location, "ConfigStorage.Save");
            var currentFile = InternalConfigStorage.GetCurrentFileName(location, typeName);
            Debug?.Log("Using file name: " + (fileName ?? currentFile), "ConfigStorage.Save");
            InternalConfigStorage.Save(location, typeName, fileName ?? currentFile);
        }

        public static T GetOrCreate<T>(ConfigLocationType location)
            where T : ConfigBase, new()
        {
            EnsureInitialized();
            InternalConfigStorage.Register<T>(location, null);
            return InternalConfigStorage.GetOrCreate<T>(location);
        }

        public static string GetConfigAsText<T>(ConfigLocationType location)
            where T : ConfigBase, new()
        {
            EnsureInitialized();
            InternalConfigStorage.Register<T>(location, null);
            return InternalConfigStorage.GetConfigAsText(location, typeof(T).Name);
        }

        public static string GetFileAsText(ConfigLocationType location, string fileName)
        {
            EnsureInitialized();
            return InternalConfigStorage.GetFileAsText(location, fileName);
        }
    }
}
