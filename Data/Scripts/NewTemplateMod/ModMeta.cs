using System;
using mz.SemanticVersioning;
using Sandbox.ModAPI;

namespace mz.NewTemplateMod
{
    public static class ModMeta
    {
        // A nice human-readable name
        public const string Name = "NewTemplateMod";

        // Version you bump manually
        public static readonly SemanticVersion Version = "0.1.0";

        //a global command prefix for all your mods
        public const string DevCommandPrefix = "/mz";
        public const string ModCommandPrefix = "/NewTemplateMod";

        /// <summary>
        /// Event invoked when a mod command is received.
        /// The first parameter is the sender's ID, and the second parameter is the array of command arguments (without the command prefix).
        /// </summary>
        public static Action<ulong, string[]> OnModCommand;

        public static void CheckForCommands(ulong sender, string command, ref bool sendToOthers) {
            string potentialCommand = command.Trim();
            if (potentialCommand.Equals($"/{DevCommandPrefix} mods", StringComparison.OrdinalIgnoreCase)) {
                sendToOthers = false;
                MyAPIGateway.Utilities.ShowMessage($"{DevCommandPrefix}", $"{Name} v{Version}");
                return;
            }

            if (potentialCommand.StartsWith($"/{ModCommandPrefix} ", StringComparison.OrdinalIgnoreCase)) {
                sendToOthers = false;
                string[] arguments = potentialCommand.Substring($"/{ModCommandPrefix} ".Length).Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                OnModCommand?.Invoke(sender, arguments);
                return;
            }
        }
    }
}