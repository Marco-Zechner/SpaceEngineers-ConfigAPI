using System;
using mz.Config;
using mz.Logging;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public static class ModMeta
    {
        // A nice human-readable name
        public const string NAME = "NewTemplateMod";

        // Version you bump manually
        public static readonly SemanticVersion Version = "0.1.0";

        //a global command prefix for all your mods. DO NOT use "/" as part of the command itself, it is added automatically.
        public const string DEV_NAME = "mz";
        public const string MOD_COMMAND = "ntm";
    
        /// <summary>
        /// Event invoked when a mod command is received.
        /// The first parameter is the sender's ID, and the second parameter is the array of command arguments (without the command prefix).
        /// </summary>
        public static Action<ulong, string[]> OnModCommand;

        public static void CheckForCommands(ulong sender, string command, ref bool sendToOthers) {
            // ConfigCommands.HandleConfigCommands(MOD_COMMAND, sender, command, ref sendToOthers);
            if (!sendToOthers)
                return;

            var potentialCommand = command.Trim();
            if (potentialCommand.Equals($"/{DEV_NAME.ToLowerInvariant()} mods", StringComparison.OrdinalIgnoreCase)) {
                sendToOthers = false;
                Chat.TryLine($"{NAME} v{Version} -> /{MOD_COMMAND}", $"{DEV_NAME.ToLowerInvariant()}");
                Chat.TryLine($"{NAME} v{Version} -> /{MOD_COMMAND}-cfg", $"{DEV_NAME.ToLowerInvariant()}");
                return;
            }

            if (potentialCommand.StartsWith($"/{MOD_COMMAND.ToLowerInvariant()} ", StringComparison.OrdinalIgnoreCase) || 
                potentialCommand.Equals($"/{MOD_COMMAND.ToLowerInvariant()}", StringComparison.OrdinalIgnoreCase)) {
                sendToOthers = false;
                var arguments = potentialCommand.Substring($"/{MOD_COMMAND.ToLowerInvariant()}".Length).Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                OnModCommand?.Invoke(sender, arguments);
                return;
            }
            Chat.TryLine($"No matching command found for: {potentialCommand}", "ModMeta");
        }
    }
}