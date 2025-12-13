using System.Collections.Generic;
using Sandbox.ModAPI;

namespace mz.Logging
{
    public class Chat
    {
        private struct LogEntry
        {
            public string Message;
            public string Sender;
        }
        public static string Sender = null;
        private static readonly Queue<LogEntry> _logQueue = new Queue<LogEntry>();

        /// <summary>
        /// Logs a message to the in-game chat window, queued until the client is loaded.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sender"></param>
        public static void TryLine(string msg, string sender = null)
        {
            if (Sender != null && sender == null)
                sender = Sender;
            if (sender == null)
                sender = "Log";
            _logQueue.Enqueue(new LogEntry { Message = msg, Sender = sender });
            if (!ModSession.IsClientLoaded) return;
            while (_logQueue.Count > 0)
            {
                var queuedMsg = _logQueue.Dequeue();
                MyAPIGateway.Utilities?.ShowMessage(queuedMsg.Sender, queuedMsg.Message);
            }
        }

        public void ThisTryLine(string msg, string sender = null)
        {
            if (InstanceSender != null && sender == null)
                sender = InstanceSender;
            TryLine(msg, sender);
        }

        public string InstanceSender = null;
        public Chat(string sender)
        {
            InstanceSender = sender;
        }
    }
}