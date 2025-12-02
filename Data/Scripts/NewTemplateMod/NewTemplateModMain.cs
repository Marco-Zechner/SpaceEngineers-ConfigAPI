using System.Collections.Generic;
using mz.Config;
using mz.Config.Core;
using mz.Config.Domain;
using mz.Config.SeImpl;
using mz.Logging;
using VRage.Game.Components;

namespace mz.NewTemplateMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]  
    public partial class NewTemplateModMain : MySessionComponentBase
    {
        private static readonly List<ConfigBase> _configs = new List<ConfigBase>();
        private SimpleConfig _simpleConfig;
        // private IntermediateConfig _intermediateConfig;
        // private CollectionConfig _collectionConfig;
        // private MigrationConfig _migrationConfig;
        // private AdvancedConfig _advancedConfig;

        public override void LoadData()
        {
            ConfigStorage.Debug = new Debug(); //TODO do this better
            _simpleConfig = ConfigStorage.Load<SimpleConfig>(ConfigLocationType.Local, "CoolName");
            _configs.Add(_simpleConfig);
            
            /* TODO:
             * Implement Multiplayer support (client-server config sync)
             * Remember which files the user loaded as custom configs, so we can load them again next time by default.
             * Auto save configs on change?
             * Add comments to config files?
             * Test & add support for more complex config structures (dictionaries, nested collections, etc.)
             */
            
            
            // _intermediateConfig = ConfigStorage.Register<IntermediateConfig>(ConfigStorageKind.Local);
            // _configs.Add(_intermediateConfig);
            // _collectionConfig = ConfigStorage.Register<CollectionConfig>(ConfigStorageKind.Local);
            // _configs.Add(_collectionConfig);
            // _migrationConfig = ConfigStorage.Register<MigrationConfig>(ConfigStorageKind.Local);
            // _configs.Add(_migrationConfig);
            // _advancedConfig = ConfigStorage.Register<AdvancedConfig>(ConfigStorageKind.Local);
            // _configs.Add(_advancedConfig);

            base.LoadData();
        }

        private void HandleCommands(ulong sender, string[] arguments)
        {
            switch (arguments[0].ToLowerInvariant())
            {
                case "get":
                    Chat.TryLine($"SimpleConfig.SomeValue = {_simpleConfig.SomeValue}", "NewTemplateMod");
                    // Chat.TryLine($"IntermediateConfig.CurrentMode = {_intermediateConfig.CurrentMode}", "New TemplateMod");
                    // Chat.TryLine($"CollectionConfig.Tags = {string.Join(", ", _collectionConfig.Tags.Value)}", "NewTemplateMod");
                    // Chat.TryLine($"MigrationConfig.DisplayName = {_migrationConfig.DisplayName}", "NewTemplateMod");
                    // Chat.TryLine($"AdvancedConfig.Settings.Display.Height = {_advancedConfig.Settings.Value.Display.Value.Height}", "NewTemplateMod");
                    break;
                case "set":
                    _simpleConfig.SomeValue = 999;
                    ConfigStorage.Save<SimpleConfig>(ConfigLocationType.Local);
                    // _intermediateConfig.CurrentMode = IntermediateConfig.Mode.Expert;
                    // _collectionConfig.Tags.Value.Add("gamma");
                    // _migrationConfig.DisplayName = "Updated Name";
                    // _advancedConfig.Settings.Value.Display.Value.Height = 999;
                    break;

                default:
                    Chat.TryLine($"Unknown command: {arguments[0]}", "NewTemplateMod");
                    break;
            }
        }
    }
}