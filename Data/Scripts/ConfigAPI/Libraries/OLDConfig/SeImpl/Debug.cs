using System;
using System.IO;
using mz.Config.Abstractions;
using mz.Config.Core;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace mz.Config.SeImpl
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MySandboxLoggerProvider : MySessionComponentBase
    {
        private static TextWriter _logWriter = null;
        
        public override void LoadData()
        {
            GetLogWriter();
            ConfigStorage.Debug = new Debug();
            base.LoadData();
        }

        public static TextWriter GetLogWriter()
        {
            if (_logWriter == null)
            {
                _logWriter = MyAPIGateway.Utilities.WriteFileInLocalStorage($"Log_{DateTime.Now:yyyyMMdd_hhmmssfff}.log", typeof(MySandboxLoggerProvider));
            }
            
            return _logWriter;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            _logWriter?.Dispose();
            _logWriter = null;
        }
    }
    
    public class Debug : IDebug
    {
        public void Log(string message, string source = "cfg")
        {
            message = message.Replace("\r\n", "\n");
            var lines = message.Split('\n');
            var prefix = GetPrefix();
            var sender = $"[{source}] ";
            var prefixEmpty = new string(' ', prefix.Length + sender.Length);
            var writer = MySandboxLoggerProvider.GetLogWriter();
            for (var i = 0; i < lines.Length && writer != null; i++)
            {
                if (i == 0)
                    writer.WriteLine(prefix + sender + lines[i]);
                else
                    writer.WriteLine(prefixEmpty + lines[i]);
            }
        }
        
        private static string GetPrefix()
        {
            var now = DateTime.Now;
            return $"[{now:HH:mm:ss.ffff}] ";
        }
    }
}