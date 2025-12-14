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

        private readonly string _source;
        private readonly TextWriter _writer;
        private readonly LogConfig<TTopic> _config;

        internal Logger(string source, TextWriter writer, LogConfig<TTopic> config)
        {
            _source = source ?? "Log";
            _writer = writer;
            _config = config ?? new LogConfig<TTopic>();
        }

        public LogConfig<TTopic> Config { get { return _config; } }

        // Trace: no detail; intended for “entered method X”
        public void Trace(TTopic topic, string message)
        {
            Write(LogSeverity.Trace, topic, 0, message, hasDetail: false);
        }

        public void Debug(TTopic topic, int detail, string message)
        {
            Write(LogSeverity.Debug, topic, detail, message, hasDetail: true);
        }

        public void Info(TTopic topic, int detail, string message)
        {
            Write(LogSeverity.Info, topic, detail, message, hasDetail: true);
        }

        public void Warning(TTopic topic, string message)
        {
            Write(LogSeverity.Warning, topic, 0, message, hasDetail: false);
        }

        public void Error(TTopic topic, string message)
        {
            Write(LogSeverity.Error, topic, 0, message, hasDetail: false);
        }

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

        private void Write(LogSeverity sev, TTopic topic, int detail, string message, bool hasDetail)
        {
            if (message == null) message = "";

            LogOutput output;
            bool shouldLog;

            if (sev == LogSeverity.Error)
            {
                output = _config.ErrorOutput;
                shouldLog = _config.AlwaysLogErrors || _config.GetRule(topic).Enabled;
            }
            else if (sev == LogSeverity.Warning)
            {
                output = _config.WarningOutput;
                shouldLog = _config.AlwaysLogWarnings || _config.GetRule(topic).Enabled;
            }
            else
            {
                var rule = _config.GetRule(topic);
                shouldLog = rule.Enabled && (!hasDetail || detail <= rule.MaxDetail);
                output = rule.Output;
            }

            if (!shouldLog) return;

            WriteFile(sev, topic, detail, message, hasDetail);

            // Policy:
            // - Errors always FileAndChat (by config)
            // - Warnings file-only (by config)
            // - Trace/Debug/Info chat only if topic rule Output == FileAndChat
            if (output == LogOutput.FileAndChat && sev != LogSeverity.Warning)
                _chatQueue.Enqueue(new ChatEntry { Sender = _source, Message = message });
        }

        private void WriteFile(LogSeverity sev, TTopic topic, int detail, string message, bool hasDetail)
        {
            if (_writer == null) return;

            var now = DateTime.Now;
            var prefix = "[" + now.ToString("HH:mm:ss.ffff") + "] ";
            var head = prefix
                       + "[" + _source
                       + "/" + topic
                       + "/" + sev
                       + (hasDetail ? ("/d" + detail) : "")
                       + "] ";

            message = message.Replace("\r\n", "\n");
            var lines = message.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) _writer.WriteLine(head + lines[i]);
                else _writer.WriteLine(new string(' ', head.Length) + lines[i]);
            }

            _writer.Flush();
        }
    }
}