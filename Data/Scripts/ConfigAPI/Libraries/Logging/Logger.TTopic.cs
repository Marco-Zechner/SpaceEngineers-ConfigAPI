using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;

namespace MarcoZechner.Logging
{
    public sealed class Logger<TTopic> where TTopic : struct
    {
        private struct ChatEntry
        {
            public string Sender;
            public string Message;
        }

        private readonly Queue<ChatEntry> _chatQueue = new Queue<ChatEntry>();

        public string Source => _source;
        private readonly string _source;
        private TextWriter _writer;

        internal Logger(string source, TextWriter writer, LogConfig<TTopic> config)
        {
            _source = source ?? "Log";
            _writer = writer;
            Config = config ?? new LogConfig<TTopic>();
        }
        
        public void CloseWriter()
        {
            _writer?.Close();
            _writer = null;
        }

        public LogConfig<TTopic> Config { get; }

        /// <summary>
        /// Trace method entry without any topic.
        /// Put this as the first line of a method.
        /// Produces:
        ///   Trace: Foo()
        /// or:
        ///   Trace: Foo(a=1,b=2)
        /// </summary>
        public void Trace(string methodCall, string args = null)
        {
            if (string.IsNullOrEmpty(methodCall))
                methodCall = "?";

            string line;
            if (Config.TraceArgumentsEnabled && !string.IsNullOrEmpty(args))
                line = methodCall + "(" + args + ")";
            else
                line = methodCall + "()";

            WriteTrace(line);
        }

        public void Debug(TTopic topic, int detail, string message) 
            => Write(LogSeverity.Debug, topic, detail, message, hasDetail: true);

        public void Info(TTopic topic, int detail, string message) 
            => Write(LogSeverity.Info, topic, detail, message, hasDetail: true);

        public void Warning(TTopic topic, string message) 
            => Write(LogSeverity.Warning, topic, 0, message, hasDetail: false);

        public void Error(TTopic topic, string message) 
            => Write(LogSeverity.Error, topic, 0, message, hasDetail: false);

        /// <summary>
        /// Flush queued chat messages once the game/client is ready.
        /// Call from a MySessionComponentBase UpdateAfterSimulation.
        /// </summary>
        public void FlushChatIfReady(bool isClientReady)
        {
            if (!isClientReady) return;
            if (MyAPIGateway.Utilities == null) return;

            while (_chatQueue.Count > 0)
            {
                var e = _chatQueue.Dequeue();
                MyAPIGateway.Utilities.ShowMessage(e.Sender, e.Message);
            }
        }

        private void WriteTrace(string message)
        {
            // Route trace by its own policy (no topic rules involved)
            var output = Config.TraceOutput;
            if (output != LogOutput.File && output != LogOutput.FileAndChat)
                return;

            WriteFileNoTopic("Trace", message);

            if (output == LogOutput.FileAndChat)
                _chatQueue.Enqueue(new ChatEntry { Sender = _source, Message = message });
        }

        private void Write(LogSeverity sev, TTopic topic, int detail, string message, bool hasDetail)
        {
            if (message == null) message = "";

            LogOutput output;
            bool shouldLog;

            switch (sev)
            {
                case LogSeverity.Error:
                    output = Config.ErrorOutput;
                    shouldLog = Config.AlwaysLogErrors || Config.GetRule(topic).Enabled;
                    break;
                case LogSeverity.Warning:
                    output = Config.WarningOutput;
                    shouldLog = Config.AlwaysLogWarnings || Config.GetRule(topic).Enabled;
                    break;
                case LogSeverity.Debug:
                case LogSeverity.Info:
                default:
                    var rule = Config.GetRule(topic);
                    shouldLog = rule.Enabled && (!hasDetail || detail <= rule.MaxDetail);
                    output = rule.Output;
                    break;
            }

            if (!shouldLog) return;

            WriteFile(sev, topic, detail, message, hasDetail);

            // Policy:
            // - Errors always FileAndChat (by config)
            // - Warnings file-only (by config)
            // - Debug/Info chat only if topic rule Output == FileAndChat
            if (output == LogOutput.FileAndChat && sev != LogSeverity.Warning)
                _chatQueue.Enqueue(new ChatEntry { Sender = _source, Message = message });
        }

        private void WriteFile(LogSeverity sev, TTopic topic, int detail, string message, bool hasDetail)
        {
            if (_writer == null) return;

            var head = $"{Prefix}[{_source}/{topic}/{sev}{(hasDetail ? "/d" + detail : "")}] ";
            message = message.Replace("\r\n", "\n");
            var lines = message.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                if (i == 0) _writer.WriteLine(head + lines[i]);
                else _writer.WriteLine(new string(' ', head.Length) + lines[i]);
            }

            _writer.Flush();
        }

        private void WriteFileNoTopic(string sevTag, string message)
        {
            if (_writer == null) return;

            var head = $"{Prefix}[{_source}/{sevTag}] ";
            message = (message ?? "").Replace("\r\n", "\n");
            var lines = message.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                if (i == 0) _writer.WriteLine(head + lines[i]);
                else _writer.WriteLine(new string(' ', head.Length) + lines[i]);
            }

            _writer.Flush();
        }
        
        private static string Prefix => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}] ";
    }
}