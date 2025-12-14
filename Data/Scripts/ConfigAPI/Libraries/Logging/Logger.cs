using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;

namespace MarcoZechner.Logging
{
    public static class Logger
    {
        private static string _dateSuffix;
        static Logger()
        {
            var now = DateTime.Now;
            _dateSuffix = $"{now:YYYYMMdd_HHmmssffff}";
        }
        
        private static class Cache<TTopic> where TTopic : struct
        {
            internal static readonly Dictionary<string, Logger<TTopic>> ByFile
                = new Dictionary<string, Logger<TTopic>>(StringComparer.OrdinalIgnoreCase);
        }

        public static Logger<TTopic> Get<TTopic>(string chatName, string fileName)
            where TTopic : struct
        {
            return Get<TTopic>(chatName, fileName, null);
        }

        public static Logger<TTopic> Get<TTopic>(
            string chatName,
            string fileName,
            Action<LogConfig<TTopic>> initConfig)
            where TTopic : struct
        {
            if (string.IsNullOrEmpty(chatName)) chatName = "Log";
            if (string.IsNullOrEmpty(fileName)) fileName = "Log.log";
            
            var ext = Path.GetExtension(fileName);
            fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{_dateSuffix}{ext}";
            
            Logger<TTopic> existing;
            if (Cache<TTopic>.ByFile.TryGetValue(fileName, out existing))
                return existing;
            
            TextWriter writer = null;
            try
            {
                // Local storage is per-mod. Using typeof(Logger) is stable inside the mod assembly.
                if (MyAPIGateway.Utilities != null)
                    writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(Logger));
            }
            catch
            {
                writer = null; // never crash because of logging
            }

            var cfg = new LogConfig<TTopic>();
            if (initConfig != null)
            {
                try { initConfig(cfg); }
                catch { /* swallow: config init must never crash the mod */ }
            }

            var created = new Logger<TTopic>(chatName, writer, cfg);
            Cache<TTopic>.ByFile[fileName] = created;
            return created;
        }
    }
}