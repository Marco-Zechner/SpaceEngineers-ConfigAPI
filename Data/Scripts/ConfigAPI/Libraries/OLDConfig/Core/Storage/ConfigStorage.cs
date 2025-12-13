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
        private static bool Initialized => InternalConfigStorage.IsInitialized;
        public static IDebug Debug { get; set; }
        private static string StoragePrefix { get; set; } = "no_mod_context";

        public static void CustomInitialize(
            IConfigLayoutMigrator layoutMigrator,
            IXmlConverter xmlConverter)
        {
            if (Initialized)
                throw new InvalidOperationException("ConfigStorage has already been initialized.");

            IConfigFileSystem fileSystem = new ConfigFileSystem();
            IConfigXmlSerializer xmlSerializer = new ConfigXmlSerializer();
            
            InternalConfigStorage.Initialize(fileSystem, xmlSerializer, layoutMigrator, xmlConverter, StoragePrefix);
        }

        public static void InitModContext(string storagePrefix) => StoragePrefix = storagePrefix;

        private static void EnsureInitialized()
        {
            if (Initialized) return;
            
            if (StoragePrefix == "no_mod_context")
                Debug.Log("Please call ConfigStorage.InitModContext before using ConfigStorage.", "ConfigStorage.EnsureInitialized");
            
            var layout = new ConfigLayoutMigrator();
            var converter = new TomlXmlConverter();
            CustomInitialize(layout, converter);
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
    }
}
