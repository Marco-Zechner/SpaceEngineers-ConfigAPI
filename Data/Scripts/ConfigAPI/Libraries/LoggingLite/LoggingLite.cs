using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;

namespace MarcoZechner.LoggingLite
{
    public sealed class LogConfig
    {
        public bool WarningInChat = true;
        public bool ErrorInChat = true;
        public bool InfoInChat = false;
        public bool DebugInChat = false;

        public bool DebugEnabled = false;

        public string ChatName = "Log";
        public int MaxLineChars = 0;
    }

    public abstract class LogBase<TSelf>
        where TSelf : LogBase<TSelf>, new()
    {
        // Static singleton per derived type
        private static readonly TSelf _inst = new TSelf();
        
        private LogConfig ConfigInstance { get; }
        
        protected LogBase()
        {
            ConfigInstance = new LogConfig();
            ChangeConfig(ConfigInstance);
            if (ConfigInstance == null) throw new InvalidOperationException("ChangeConfig() returned null.");
        }
        
        protected abstract void ChangeConfig(LogConfig defaultConfig);

        // Static surface
        /// <summary>
        /// THIS IS NULL DURING THE CONSTRUCTOR OF THE DERIVED CLASS
        /// </summary>
        public static LogConfig Config => _inst?.ConfigInstance;

        public static void Info(string msg, bool forceChat = false) => _inst.InfoInstance(msg, forceChat);
        public static void Warning(string msg, bool forceChat = false) => _inst.WarningInstance(msg, forceChat);
        public static void Error(string msg, bool forceChat = false) => _inst.ErrorInstance(msg, forceChat);
        public static void Error(string msg, Exception ex, bool forceChat = false) => _inst.ErrorInstance(msg, ex, forceChat);
        public static void Debug(Func<string> msgFactory, bool forceChat = false) => _inst.DebugInstance(msgFactory, forceChat);

        public static void Close() => _inst.CloseWriter();
        public static void TryFlushChat() => _inst.FlushChat();

        // ------------------------------------------------------------
        // Required mod-specific identity (implemented by derived class)
        // ------------------------------------------------------------
        protected abstract string FileName { get; }

        // Use derived type as local storage identity
        private static Type StorageType => typeof(TSelf);

        // ------------------------------------------------------------
        // Instance state
        // ------------------------------------------------------------

        private TextWriter _writer;
        private bool _writerFailed;

        private struct ChatEntry
        {
            public string Sender;
            public string Message;
        }

        private readonly Queue<ChatEntry> _chatQueue = new Queue<ChatEntry>();

        public void CloseWriter()
        {
            try { _writer?.Close(); }
            catch
            {
                // ignored
            }

            _writer = null;
        }

        // ------------------------------------------------------------
        // Instance logging API (called by static wrappers)
        // ------------------------------------------------------------
        protected void InfoInstance(string message, bool forceChat)
        {
            WriteFile("INFO", message);

            if (ConfigInstance.InfoInChat || forceChat)
                WriteChat("INFO", message);
        }

        protected void WarningInstance(string message, bool forceChat)
        {
            WriteFile("WARN", message);

            if (ConfigInstance.WarningInChat || forceChat)
                WriteChat("WARN", message);
        }

        protected void ErrorInstance(string message, bool forceChat)
        {
            WriteFile("ERROR", message);

            if (ConfigInstance.ErrorInChat || forceChat)
                WriteChat("ERROR", message);
        }

        protected void ErrorInstance(string message, Exception ex, bool forceChat)
        {
            if (message == null) message = "";

            // main line
            ErrorInstance(message, forceChat);

            if (ex == null) return;

            // file: full exception block
            WriteFile("ERROR", BuildExceptionBlock(ex));

            // chat: short summary only
            if (!ConfigInstance.ErrorInChat && !forceChat) return;
            
            var shortMsg = ex.GetType().Name + ": " + (ex.Message ?? "null");
            WriteChat("ERROR", shortMsg);
        }

        protected void DebugInstance(Func<string> messageFactory, bool forceChat)
        {
            if (!ConfigInstance.DebugEnabled) return;
            if (messageFactory == null) return;

            string msg;
            try { msg = messageFactory() ?? "null"; }
            catch (Exception ex)
            {
                msg = "Debug messageFactory threw: " + ex.GetType().Name + ": " + (ex.Message ?? "null");
            }

            WriteFile("DEBUG", msg);

            if (ConfigInstance.DebugInChat || forceChat)
                WriteChat("DEBUG", msg);
        }

        // ------------------------------------------------------------
        // Core write helpers
        // ------------------------------------------------------------
        private void EnsureWriter()
        {
            if (_writer != null || _writerFailed) return;

            try
            {
                if (MyAPIGateway.Utilities == null)
                    return;

                var file = FileName;
                if (string.IsNullOrEmpty(file))
                    file = "Log.log";

                _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(file, StorageType);
            }
            catch
            {
                _writerFailed = true;
                _writer = null;
            }
        }

        private void WriteFile(string type, string message)
        {
            EnsureWriter();
            if (_writer == null) return;

            var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var prefix = ts + " " + type + " ";
            var prefixPad = new string(' ', prefix.Length);

            if (message == null) message = "";
            message = message.Replace("\r\n", "\n");

            var start = 0;
            var lineIndex = 0;

            for (var i = 0; i <= message.Length; i++)
            {
                var end = (i == message.Length);
                if (!end && message[i] != '\n') continue;

                var len = i - start;
                if (len < 0) len = 0;

                var line = (len == 0) ? "" : message.Substring(start, len);
                line = TrimIfNeeded(line);

                if (lineIndex == 0) _writer.WriteLine(prefix + line);
                else _writer.WriteLine(prefixPad + line);

                lineIndex++;
                start = i + 1;
            }

            _writer.Flush();
        }

        private void WriteChat(string type, string message)
        {
            var sender = ConfigInstance.ChatName ?? "Log";
            var msg = (message ?? "");

            msg = msg.Replace("\r\n", " ").Replace("\n", " ");
            msg = TrimIfNeeded(msg);

            var final = type + ": " + msg;

            if (MyAPIGateway.Utilities == null)
            {
                _chatQueue.Enqueue(new ChatEntry { Sender = sender, Message = final });
                return;
            }

            // Before writing, flush anything queued
            FlushChat();

            MyAPIGateway.Utilities.ShowMessage(sender, final);
        }

        protected void FlushChat()
        {
            if (MyAPIGateway.Utilities == null) return;

            while (_chatQueue.Count > 0)
            {
                var e = _chatQueue.Dequeue();
                MyAPIGateway.Utilities.ShowMessage(e.Sender, e.Message);
            }
        }

        private string TrimIfNeeded(string s)
        {
            if (s == null) return "null";
            var max = ConfigInstance.MaxLineChars;
            if (max <= 0 || s.Length <= max) return s;
            return s.Substring(0, max) + " … (trimmed, len=" + s.Length + ")";
        }

        private static string BuildExceptionBlock(Exception ex)
        {
            var msg = ex.Message ?? "null";
            var type = ex.GetType().FullName ?? ex.GetType().Name;

            var stack = ex.StackTrace;
            if (string.IsNullOrEmpty(stack))
                return "Exception: " + type + ": " + msg;

            return "Exception: " + type + ": " + msg + "\n" + stack;
        }
    }
}
