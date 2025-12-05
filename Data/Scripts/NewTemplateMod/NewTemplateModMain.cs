using System;
using System.Collections.Generic;
using mz.Config;
using mz.Config.Core;
using mz.Config.Domain;
using mz.Config.SeImpl;
using mz.Logging;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace mz.NewTemplateMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]  
    public partial class NewTemplateModMain : MySessionComponentBase
    {
        private static readonly List<ConfigBase> _configs = new List<ConfigBase>();
        
        private SimpleConfig _simpleConfig;
        private IntermediateConfig _intermediateConfig;
        private CollectionConfig _collectionConfig;
        private MigrationConfig _migrationConfig;
        private AdvancedConfig _advancedConfig;
        private KeybindConfig _keybindConfig;

        private const string COMMAND_PREFIX = "/ntcfg";
        
        public override void LoadData()
        {
            base.LoadData();
            
            /* TODO:
             * Implement Multiplayer support (client-server config sync)
             * qol Remember which files the user loaded as custom configs, so we can load them again next time by default.
             * qol Auto save configs on change?
             * nullable fields in toml
             * qol deserialize old xml and save as toml?
             */
            
            _simpleConfig       = ConfigStorage.Load<SimpleConfig>(ConfigLocationType.Local, "CoolName");
            _intermediateConfig = ConfigStorage.Load<IntermediateConfig>(ConfigLocationType.Local);
            _collectionConfig   = ConfigStorage.Load<CollectionConfig>(ConfigLocationType.Local);
            _migrationConfig    = ConfigStorage.Load<MigrationConfig>(ConfigLocationType.Local);
            _advancedConfig     = ConfigStorage.Load<AdvancedConfig>(ConfigLocationType.Local);
            _keybindConfig      = ConfigStorage.Load<KeybindConfig>(ConfigLocationType.Local);

            _configs.Clear();
            _configs.Add(_simpleConfig);
            _configs.Add(_intermediateConfig);
            _configs.Add(_collectionConfig);
            _configs.Add(_migrationConfig);
            _configs.Add(_advancedConfig);
            _configs.Add(_keybindConfig);
            
        }

        public override void BeforeStart()
        {
            base.BeforeStart();
            if (MyAPIGateway.Utilities != null)
            {
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            }
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Utilities != null)
            {
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            }

            base.UnloadData();
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (string.IsNullOrEmpty(messageText))
                return;

            if (!messageText.StartsWith(COMMAND_PREFIX, StringComparison.OrdinalIgnoreCase))
                return;

            // Swallow the message; this is a mod command.
            sendToOthers = false;

            var remainder = messageText.Substring(COMMAND_PREFIX.Length).Trim();
            if (string.IsNullOrEmpty(remainder))
            {
                PrintHelp();
                return;
            }

            var parts = remainder.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                PrintHelp();
                return;
            }

            HandleRootCommand(parts);
        }

        private void HandleRootCommand(string[] args)
        {
            var section = args[0].ToLowerInvariant();

            switch (section)
            {
                case "help":
                    PrintHelp();
                    break;

                case "simple":
                    HandleSimpleCommand(args);
                    break;

                case "intermediate":
                    HandleIntermediateCommand(args);
                    break;

                case "collection":
                    HandleCollectionCommand(args);
                    break;

                case "migration":
                    HandleMigrationCommand(args);
                    break;

                case "advanced":
                    HandleAdvancedCommand(args);
                    break;

                case "keybind":
                    if (args.Length > 1 && args[1] == "reload")
                    {
                        string fileName = null;
                        if (args.Length > 2 && args[2] != null)
                        {
                            fileName = args[2];
                        }
                        _keybindConfig = ConfigStorage.Load<KeybindConfig>(ConfigLocationType.Local, fileName);
                        Chat.TryLine("KeybindConfig reloaded. \nSelect = " +
                                     _keybindConfig.Keybinds.Select.Modifier + "+" +
                                     _keybindConfig.Keybinds.Select.Action + ", Toggle=" +
                                     _keybindConfig.Keybinds.Select.Toggle +
                                     "\nThrow = " +
                                     _keybindConfig.Keybinds.Throw.Modifier + "+" +
                                     _keybindConfig.Keybinds.Throw.Action + ", Toggle=" +
                                     _keybindConfig.Keybinds.Throw.Toggle,
                            "NewTemplateMod");
                    }
                    break;

                default:
                    Chat.TryLine("Unknown section. Use '/ntcfg help' for usage.", "NewTemplateMod");
                    break;
            }
        }

        // ---------------- SIMPLE ----------------

        private void HandleSimpleCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Chat.TryLine("Usage: /ntcfg simple get|set <value>|reload", "NewTemplateMod");
                return;
            }

            var cmd = args[1].ToLowerInvariant();

            switch (cmd)
            {
                case "get":
                    Chat.TryLine("SimpleConfig.SomeValue = " + _simpleConfig.SomeValue +
                                 ", SomeText = '" + _simpleConfig.SomeText + "'", "NewTemplateMod");
                    break;

                case "set":
                    if (args.Length < 3)
                    {
                        Chat.TryLine("Usage: /ntcfg simple set <intValue>", "NewTemplateMod");
                        return;
                    }

                    int newVal;
                    if (!int.TryParse(args[2], out newVal))
                    {
                        Chat.TryLine("Invalid int value: " + args[2], "NewTemplateMod");
                        return;
                    }

                    _simpleConfig.SomeValue = newVal;
                    ConfigStorage.Save<SimpleConfig>(ConfigLocationType.Local);
                    Chat.TryLine("SimpleConfig.SomeValue set to " + newVal + " and saved.", "NewTemplateMod");
                    break;

                case "reload":
                    _simpleConfig = ConfigStorage.Load<SimpleConfig>(ConfigLocationType.Local, "CoolName");
                    Chat.TryLine("SimpleConfig reloaded. SomeValue = " + _simpleConfig.SomeValue +
                                 ", SomeText = '" + _simpleConfig.SomeText + "'", "NewTemplateMod");
                    break;

                default:
                    Chat.TryLine("Unknown simple-command. Use '/ntcfg simple get|set|reload'.", "NewTemplateMod");
                    break;
            }
        }

        // ---------------- INTERMEDIATE ----------------

        private void HandleIntermediateCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Chat.TryLine("Usage: /ntcfg intermediate get|setmode <Basic|Advanced|Expert>|reload", "NewTemplateMod");
                return;
            }

            var cmd = args[1].ToLowerInvariant();

            switch (cmd)
            {
                case "get":
                    Chat.TryLine(
                        "IntermediateConfig: IsEnabled=" + _intermediateConfig.IsEnabled +
                        ", OptionalValue=" + (_intermediateConfig.OptionalValue.HasValue
                            ? _intermediateConfig.OptionalValue.Value.ToString()
                            : "null") +
                        ", CurrentMode=" + _intermediateConfig.CurrentMode,
                        "NewTemplateMod");
                    break;

                case "setmode":
                    if (args.Length < 3)
                    {
                        Chat.TryLine("Usage: /ntcfg intermediate setmode <Basic|Advanced|Expert>", "NewTemplateMod");
                        return;
                    }

                    IntermediateConfig.Mode mode;
                    if (!Enum.TryParse(args[2], true, out mode))
                    {
                        Chat.TryLine("Invalid mode. Use Basic, Advanced or Expert.", "NewTemplateMod");
                        return;
                    }

                    _intermediateConfig.CurrentMode = mode;
                    ConfigStorage.Save<IntermediateConfig>(ConfigLocationType.Local);
                    Chat.TryLine("IntermediateConfig.CurrentMode set to " + mode + " and saved.", "NewTemplateMod");
                    break;

                case "reload":
                    _intermediateConfig = ConfigStorage.Load<IntermediateConfig>(ConfigLocationType.Local);
                    Chat.TryLine(
                        "IntermediateConfig reloaded: IsEnabled=" + _intermediateConfig.IsEnabled +
                        ", OptionalValue=" + (_intermediateConfig.OptionalValue.HasValue
                            ? _intermediateConfig.OptionalValue.Value.ToString()
                            : "null") +
                        ", CurrentMode=" + _intermediateConfig.CurrentMode,
                        "NewTemplateMod");
                    break;

                default:
                    Chat.TryLine("Unknown intermediate-command. Use '/ntcfg intermediate get|setmode|reload'.", "NewTemplateMod");
                    break;
            }
        }

        // ---------------- COLLECTION ----------------

        private void HandleCollectionCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Chat.TryLine("Usage: /ntcfg collection get|addtag <tag>|reload", "NewTemplateMod");
                return;
            }

            var cmd = args[1].ToLowerInvariant();

            switch (cmd)
            {
                case "get":
                    Chat.TryLine(
                        "CollectionConfig: Tags=[" + string.Join(", ", _collectionConfig.Tags.ToArray()) + "]",
                        "NewTemplateMod");

                    if (_collectionConfig.NamedValues != null && _collectionConfig.NamedValues.Dictionary.Count > 0)
                    {
                        var pairs = new List<string>();
                        foreach (var kv in _collectionConfig.NamedValues.Dictionary)
                        {
                            pairs.Add(kv.Key + "=" + kv.Value);
                        }
                        Chat.TryLine("NamedValues: " + string.Join(", ", pairs.ToArray()), "NewTemplateMod");
                    }
                    else
                    {
                        Chat.TryLine("NamedValues: (null or empty)", "NewTemplateMod");
                    }

                    if (_collectionConfig.Nested != null)
                    {
                        Chat.TryLine(
                            "Nested: Threshold=" + _collectionConfig.Nested.Threshold +
                            ", Allowed=" + _collectionConfig.Nested.Allowed,
                            "NewTemplateMod");
                    }
                    else
                    {
                        Chat.TryLine("Nested: null", "NewTemplateMod");
                    }

                    break;

                case "addtag":
                    if (args.Length < 3)
                    {
                        Chat.TryLine("Usage: /ntcfg collection addtag <tag>", "NewTemplateMod");
                        return;
                    }

                    var tag = args[2];
                    _collectionConfig.Tags.Add(tag);
                    ConfigStorage.Save<CollectionConfig>(ConfigLocationType.Local);
                    Chat.TryLine("Added tag '" + tag + "' to CollectionConfig.Tags and saved.", "NewTemplateMod");
                    break;

                case "reload":
                    _collectionConfig = ConfigStorage.Load<CollectionConfig>(ConfigLocationType.Local);
                    Chat.TryLine(
                        "CollectionConfig reloaded. Tags=[" + string.Join(", ", _collectionConfig.Tags.ToArray()) + "]",
                        "NewTemplateMod");
                    break;

                default:
                    Chat.TryLine("Unknown collection-command. Use '/ntcfg collection get|addtag|reload'.", "NewTemplateMod");
                    break;
            }
        }

        // ---------------- MIGRATION ----------------

        private void HandleMigrationCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Chat.TryLine("Usage: /ntcfg migration get|setname <name>|reload", "NewTemplateMod");
                return;
            }

            var cmd = args[1].ToLowerInvariant();

            switch (cmd)
            {
                case "get":
                    Chat.TryLine(
                        "MigrationConfig: ConfigVersion=" + _migrationConfig.ConfigVersion +
                        ", RefreshIntervalSeconds=" + _migrationConfig.RefreshIntervalSeconds +
                        ", DisplayName='" + _migrationConfig.DisplayName + "'",
                        "NewTemplateMod");
                    break;

                case "setname":
                    if (args.Length < 3)
                    {
                        Chat.TryLine("Usage: /ntcfg migration setname <newName>", "NewTemplateMod");
                        return;
                    }

                    var newName = args[2];
                    _migrationConfig.DisplayName = newName;
                    ConfigStorage.Save<MigrationConfig>(ConfigLocationType.Local);
                    Chat.TryLine("MigrationConfig.DisplayName set to '" + newName + "' and saved.", "NewTemplateMod");
                    break;

                case "reload":
                    _migrationConfig = ConfigStorage.Load<MigrationConfig>(ConfigLocationType.Local);
                    Chat.TryLine(
                        "MigrationConfig reloaded. DisplayName='" + _migrationConfig.DisplayName + "', RefreshIntervalSeconds=" +
                        _migrationConfig.RefreshIntervalSeconds,
                        "NewTemplateMod");
                    break;

                default:
                    Chat.TryLine("Unknown migration-command. Use '/ntcfg migration get|setname|reload'.", "NewTemplateMod");
                    break;
            }
        }

        // ---------------- ADVANCED ----------------

        private void HandleAdvancedCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Chat.TryLine("Usage: /ntcfg advanced get|reload", "NewTemplateMod");
                return;
            }

            var cmd = args[1].ToLowerInvariant();

            switch (cmd)
            {
                case "get":
                    if (_advancedConfig.Settings != null && _advancedConfig.Settings.Display != null)
                    {
                        var disp = _advancedConfig.Settings.Display;
                        Chat.TryLine(
                            "AdvancedConfig.Display: " +
                            "Width=" + disp.Width + ", Height=" + disp.Height +
                            ", Theme='" + disp.Theme + "', Dpi=" +
                            (disp.Dpi.HasValue ? disp.Dpi.Value.ToString() : "null"),
                            "NewTemplateMod");
                    }
                    else
                    {
                        Chat.TryLine("AdvancedConfig.Settings.Display is null.", "NewTemplateMod");
                    }

                    if (_advancedConfig.Settings != null && _advancedConfig.Settings.Network != null)
                    {
                        var net = _advancedConfig.Settings.Network;
                        Chat.TryLine(
                            "AdvancedConfig.Network: Host='" + net.Host + "', Port=" + net.Port +
                            ", UseTls=" + net.UseTls,
                            "NewTemplateMod");
                    }
                    else
                    {
                        Chat.TryLine("AdvancedConfig.Settings.Network is null.", "NewTemplateMod");
                    }

                    // NOTE: Plugins list (IPluginConfig) is deliberately not exercised here,
                    // because polymorphic/interface serialization is fragile with your current XML/TOML bridge.
                    break;

                case "reload":
                    _advancedConfig = ConfigStorage.Load<AdvancedConfig>(ConfigLocationType.Local);
                    Chat.TryLine("AdvancedConfig reloaded. Use '/ntcfg advanced get' to inspect.", "NewTemplateMod");
                    break;

                default:
                    Chat.TryLine("Unknown advanced-command. Use '/ntcfg advanced get|reload'.", "NewTemplateMod");
                    break;
            }
        }

        // ---------------- HELP ----------------

        private static void PrintHelp()
        {
            Chat.TryLine("Config test commands:", "NewTemplateMod");
            Chat.TryLine("/ntcfg simple get|set <int>|reload", "NewTemplateMod");
            Chat.TryLine("/ntcfg intermediate get|setmode <Basic|Advanced|Expert>|reload", "NewTemplateMod");
            Chat.TryLine("/ntcfg collection get|addtag <tag>|reload", "NewTemplateMod");
            Chat.TryLine("/ntcfg migration get|setname <name>|reload", "NewTemplateMod");
            Chat.TryLine("/ntcfg advanced get|reload", "NewTemplateMod");
        }
    }
}