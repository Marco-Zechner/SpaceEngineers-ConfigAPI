using System;
using mz.Config.Abstractions;
using mz.Logging;
using Sandbox.ModAPI;
using VRage.Utils;

namespace mz.Config.SeImpl
{
    public class Debug : IDebug
    {
        public Debug()
        {
            string existingContent;
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage("Log.log", typeof(Debug))) return;
            
            using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage("Log.log", typeof(Debug)))
            {
                existingContent = reader.ReadToEnd();
            }
            if (string.IsNullOrEmpty(existingContent)) return;
                
            using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage($"Log_{DateTime.Now:yyyyMMdd_hhmmssfff}.log", typeof(Debug)))
            {
                if (!string.IsNullOrEmpty(existingContent))
                {
                    writer.Write(existingContent);
                }
            }
            MyAPIGateway.Utilities.DeleteFileInLocalStorage("Log.log", typeof(Debug));
        }
        
        public void Log(string message, string source = "cfg")
        {
            var existingContent = "";
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage("Log.log", typeof(Debug)))
            {
                using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage("Log.log", typeof(Debug)))
                {
                    existingContent = reader.ReadToEnd();
                }
            }
            
            using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage("Log.log", typeof(Debug)))
            {
                if (!string.IsNullOrEmpty(existingContent))
                {
                    writer.Write(existingContent);
                }

                message = message.Replace("\r\n", "\n");
                var lines = message.Split('\n');
                var prefix = GetPrefix();
                var sender = $"[{source}] ";
                var prefixEmpty = new string(' ', prefix.Length + sender.Length);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (i == 0)
                        writer.WriteLine(prefix + sender + lines[i]);
                    else
                        writer.WriteLine(prefixEmpty + lines[i]);
                }
            }
            Chat.TryLine(message, source);
        }
        
        private static string GetPrefix()
        {
            var now = DateTime.Now;
            return $"[{now:HH:mm:ss.ffff}] ";
        }
    }
}