using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game.Components;

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

   /// <summary>
    /// Central runtime so chat flush + file close can be done once per mod load/unload.
    /// Idempotent and safe even if multiple loggers exist.
    /// </summary>
    public static class LoggingLiteRuntime
    {
        private static readonly object _lock = new object();
        private static readonly List<ILogInternal> _logs = new List<ILogInternal>();

        internal interface ILogInternal
        {
            void FlushChatInternal();
            void CloseWriterInternal();
        }

        internal static void Register(ILogInternal log)
        {
            lock (_lock) _logs.Add(log);
        }

        public static void FlushAll()
        {
            lock (_lock)
            {
                foreach (var t in _logs)
                    t.FlushChatInternal();
            }
        }

        public static void CloseAll()
        {
            lock (_lock)
            {
                foreach (var t in _logs)
                    t.CloseWriterInternal();
            }
        }

        /// <summary>
        /// Add this component once (e.g., in your shared/common assembly).
        /// It flushes queued chat as soon as MyAPIGateway.Utilities becomes available,
        /// and closes all writers on unload.
        /// </summary>
        [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
        public sealed class LoggingLiteSession : MySessionComponentBase
        {
            public override void BeforeStart()
            {
                FlushAll();
            }

            protected override void UnloadData()
            {
                base.UnloadData();
                CloseAll();
            }
        }
    }

    /// <summary>
    /// CRTP logger base. Derived class only overrides:
    /// - Configure(LogConfig cfg)
    /// - FileName
    ///
    /// Safety: if someone writes ExampleLog : LogBase&lt;CfgLog>, it throws immediately.
    /// </summary>
    public abstract class LogBase<TSelf> : LoggingLiteRuntime.ILogInternal
        where TSelf : LogBase<TSelf>, new()
    {
        private static readonly TSelf _inst = new TSelf();

        public static LogConfig Config => _inst._cfg;

        public static void Info(string msg, bool forceChat = false) => _inst.InfoInstance(msg, forceChat);
        public static void Warning(string msg, bool forceChat = false) => _inst.WarningInstance(msg, forceChat);
        public static void Error(string msg, bool forceChat = false) => _inst.ErrorInstance(msg, forceChat);
        public static void Error(string msg, Exception ex, bool forceChat = false) => _inst.ErrorInstance(msg, ex, forceChat);
        public static void Debug(Func<string> msgFactory, bool forceChat = false) => _inst.DebugInstance(msgFactory, forceChat);

        // Optional: allow manual flushing/close, but typically the session does it.
        public static void TryFlushChat() => _inst.FlushChat();
        public static void Close() => _inst.CloseWriter();

        private readonly LogConfig _cfg;

        private TextWriter _writer;
        private bool _writerFailed;

        private struct ChatEntry
        {
            public string Sender;
            public string Message;
        }

        private readonly Queue<ChatEntry> _chatQueue = new Queue<ChatEntry>();

        protected LogBase()
        {
            // Runtime generic safety guard: kills the exact wrong-TSelf mistake immediately.
            if (GetType() != typeof(TSelf))
                throw new InvalidOperationException(
                    "LoggingLite: TSelf mismatch. " +
                    "You must inherit as: sealed class " + GetType().Name + " : LogBase<" + GetType().Name + ">");

            _cfg = new LogConfig();
            Configure(_cfg);

            LoggingLiteRuntime.Register(this);
        }

        /// <summary>Derived sets defaults here.</summary>
        protected abstract void Configure(LogConfig cfg);

        /// <summary>Derived chooses file name here.</summary>
        protected abstract string FileName { get; }

        /// <summary>
        /// Storage identity for SE local storage. Default: the logger type itself.
        /// Override only if you have a specific reason.
        /// </summary>
        protected virtual Type StorageType => typeof(TSelf);

        // -------------------------
        // Internal lifecycle (runtime/session)
        // -------------------------
        void LoggingLiteRuntime.ILogInternal.FlushChatInternal() => FlushChat();
        void LoggingLiteRuntime.ILogInternal.CloseWriterInternal() => CloseWriter();

        private void CloseWriter()
        {
            try { _writer?.Close(); }
            catch { /* ignored */ }
            _writer = null;
        }

        // -------------------------
        // Instance logging API
        // -------------------------
        private void InfoInstance(string message, bool forceChat)
        {
            WriteFile("INFO", message);
            if (_cfg.InfoInChat || forceChat) WriteChat("INFO", message);
        }

        private void WarningInstance(string message, bool forceChat)
        {
            WriteFile("WARN", message);
            if (_cfg.WarningInChat || forceChat) WriteChat("WARN", message);
        }

        private void ErrorInstance(string message, bool forceChat)
        {
            WriteFile("ERROR", message);
            if (_cfg.ErrorInChat || forceChat) WriteChat("ERROR", message);
        }

        private void ErrorInstance(string message, Exception ex, bool forceChat)
        {
            if (message == null) message = "";

            ErrorInstance(message, forceChat);

            if (ex == null) return;

            WriteFile("ERROR", BuildExceptionBlock(ex));

            if (!_cfg.ErrorInChat && !forceChat) return;

            var shortMsg = ex.GetType().Name + ": " + (ex.Message ?? "null");
            WriteChat("ERROR", shortMsg);
        }

        private void DebugInstance(Func<string> messageFactory, bool forceChat)
        {
            if (!_cfg.DebugEnabled) return;
            if (messageFactory == null) return;

            string msg;
            try { msg = messageFactory() ?? "null"; }
            catch (Exception ex)
            {
                msg = "Debug messageFactory threw: " + ex.GetType().Name + ": " + (ex.Message ?? "null");
            }

            WriteFile("DEBUG", msg);

            if (_cfg.DebugInChat || forceChat)
                WriteChat("DEBUG", msg);
        }

        // -------------------------
        // Core write helpers
        // -------------------------
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
            var sender = _cfg.ChatName ?? "Log";
            var msg = (message ?? "");

            msg = msg.Replace("\r\n", " ").Replace("\n", " ");
            msg = TrimIfNeeded(msg);

            var final = type + ": " + msg;

            if (MyAPIGateway.Utilities == null)
            {
                _chatQueue.Enqueue(new ChatEntry { Sender = sender, Message = final });
                return;
            }

            FlushChatConsidered();
            MyAPIGateway.Utilities.ShowMessage(sender, final);
        }

        private void FlushChat()
        {
            if (MyAPIGateway.Utilities == null) return;

            while (_chatQueue.Count > 0)
            {
                var e = _chatQueue.Dequeue();
                MyAPIGateway.Utilities.ShowMessage(e.Sender, e.Message);
            }
        }

        // flush queued messages right before showing new ones
        private void FlushChatConsidered()
        {
            if (_chatQueue.Count == 0) return;
            FlushChat();
        }

        private string TrimIfNeeded(string s)
        {
            if (s == null) return "null";
            var max = _cfg.MaxLineChars;
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