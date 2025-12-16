using System;
using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Client.Core;
using MarcoZechner.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;
using MarcoZechner.ConfigAPI.Shared.Logging;
using Sandbox.ModAPI;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    /// <summary>
    /// UserMod-side hooks that ConfigAPIMod calls back into.
    /// This implementation assumes:
    /// - You register config definitions (typeKey -> serializer/default factory) somewhere central.
    /// - You want file IO in your mod's Storage folder, separated by LocationType.
    ///
    /// IMPORTANT:
    /// This is internal plumbing. User code should only touch ConfigStorage / CfgSync / ConfigBase.
    /// </summary>
    internal sealed class ConfigUserHooksImpl : IConfigUserHooks, IApiProvider
    {
        // typeKey -> definition (new default + xml serializer + descriptions)
        private static readonly Dictionary<string, IConfigDefinition> _defs
            = new Dictionary<string, IConfigDefinition>();

        // --------------------------------------------------------------------
        // Registration API (call from your boilerplate when the mod loads)
        // --------------------------------------------------------------------

        public static bool IsRegistered(string typeKey)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(IsRegistered)}", $"{nameof(typeKey)}={typeKey}");
            if (string.IsNullOrEmpty(typeKey))
                return false;

            return _defs.ContainsKey(typeKey);
        }
        
        public static void Register<T>() where T : ConfigBase, new()
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(Register)}");
            var key = typeof(T).FullName;
            if (key == null)
                throw new Exception("Type.FullName is null (unexpected in SE).");

            _defs[key] = new ConfigDefinition<T>();
        }
        
        public object NewDefault(string typeKey)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(NewDefault)}", $"{nameof(typeKey)}={typeKey}");
            var def = GetDef(typeKey);
            var obj = def.NewDefault();
            return obj;
        }

        public bool IsInstanceOf(string typeKey, object instance)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(IsInstanceOf)}", $"{nameof(typeKey)}={typeKey}");
            if (instance == null)
                return false;

            return instance.GetType().FullName == typeKey;
        }

        public string SerializeToInternalXml(string typeKey, object instance)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(SerializeToInternalXml)}", $"{nameof(typeKey)}={typeKey}");
            var def = GetDef(typeKey);
            return def.SerializeToInternalXml((ConfigBase)instance);
        }

        public object DeserializeFromInternalXml(string typeKey, string internalXml)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(DeserializeFromInternalXml)}", $"{nameof(typeKey)}={typeKey}");
            var def = GetDef(typeKey);
            return def.DeserializeFromInternalXml(internalXml);
        }

        public IReadOnlyDictionary<string, string> GetVariableDescriptions(string typeKey)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(GetVariableDescriptions)}", $"{nameof(typeKey)}={typeKey}");
            var def = GetDef(typeKey);
            return def.GetVariableDescriptions();
        }

        public string LoadFile(LocationType locationType, string filename)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(LoadFile)}", $"{nameof(locationType)}={locationType}, {nameof(filename)}={filename}");
            if (string.IsNullOrEmpty(filename))
                return null;

            var path = filename;
            if (locationType == LocationType.Local)
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(path, typeof(ConfigUserHooksImpl)))
                    return null;

                using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(path, typeof(ConfigUserHooksImpl)))
                    return reader.ReadToEnd();
            }
            
            if (!MyAPIGateway.Utilities.FileExistsInGlobalStorage(path))
                return null;

            using (var reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(path))
                return reader.ReadToEnd();
        }

        public void SaveFile(LocationType locationType, string filename, string content)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(SaveFile)}", $"{nameof(locationType)}={locationType}, {nameof(filename)}={filename},\n\t{nameof(content)}={content ?? "null"}");
            if (string.IsNullOrEmpty(filename))
                throw new Exception("SaveFile: filename is null/empty.");

            var path = filename;
            CfgLog.Logger.Debug(ConfigApiTopics.Callbacks, 0,$"{nameof(ConfigUserHooksImpl)}.{nameof(SaveFile)}: Writing to path: {path}");

            if (locationType == LocationType.Local)
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(path, typeof(ConfigUserHooksImpl)))
                    writer.Write(content ?? string.Empty);
                return;
            }
            
            using (var writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(path))
                writer.Write(content ?? string.Empty);
        }

        public void BackupFile(LocationType locationType, string filename)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(BackupFile)}", $"{nameof(locationType)}={locationType}, {nameof(filename)}={filename}");
            if (string.IsNullOrEmpty(filename))
                return;
            
            var srcPath = filename;
            if (locationType == LocationType.Local)
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(srcPath, typeof(ConfigUserHooksImpl)))
                    return;
            }
            else
            {
                if (!MyAPIGateway.Utilities.FileExistsInGlobalStorage(srcPath))
                    return;
            }


            var backupPath = filename + ".bak";

            string data;
            if (locationType == LocationType.Local)
            {
                using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(srcPath, typeof(ConfigUserHooksImpl)))
                    data = reader.ReadToEnd();

                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(backupPath, typeof(ConfigUserHooksImpl)))
                    writer.Write(data);
                return;
            }
            
            using (var reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(srcPath))
                data = reader.ReadToEnd();

            using (var writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(backupPath))
                writer.Write(data);

        }
        
        public Dictionary<string, Delegate> ConvertToDict()
        {
            return new Dictionary<string, Delegate>
            {
                { nameof(NewDefault), new Func<string, object>(NewDefault) },
                { nameof(IsInstanceOf), new Func<string, object, bool>(IsInstanceOf) },
                { nameof(SerializeToInternalXml), new Func<string, object, string>(SerializeToInternalXml) },
                { nameof(DeserializeFromInternalXml), new Func<string, string, object>(DeserializeFromInternalXml) },
                { nameof(GetVariableDescriptions), new Func<string, IReadOnlyDictionary<string, string>>(GetVariableDescriptions) },
                { nameof(LoadFile), new Func<int, string, string>(LoadFileInternal) },
                { nameof(SaveFile), new Action<int, string, string>(SaveFileInternal) },
                { nameof(BackupFile), new Action<int, string>(BackupFileInternal) },
            };
        }
        
        // ===============================================================
        // Internal conversion methods for delegate to custom types
        // ===============================================================
        
        private string LoadFileInternal(int locationTypeEnum, string filename) 
            => LoadFile((LocationType)locationTypeEnum, filename);
        
        private void SaveFileInternal(int locationTypeEnum, string filename, string content) 
            => SaveFile((LocationType)locationTypeEnum, filename, content);
        
        private void BackupFileInternal(int locationTypeEnum, string filename) 
            => BackupFile((LocationType)locationTypeEnum, filename);
        
        // --------------------------------------------------------------------
        // Internals
        // --------------------------------------------------------------------

        private static IConfigDefinition GetDef(string typeKey)
        {
            CfgLog.Logger.Trace($"{nameof(ConfigUserHooksImpl)}.{nameof(GetDef)}", $"{nameof(typeKey)}={typeKey}");
            if (string.IsNullOrEmpty(typeKey))
                throw new Exception("ConfigUserHooksImpl: typeKey is null/empty.");

            IConfigDefinition def;
            if (_defs.TryGetValue(typeKey, out def))
                return def;

            throw new Exception("ConfigUserHooksImpl: No config definition registered for typeKey: " + typeKey);
        }
    }
}