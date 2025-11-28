using System;
using System.Linq;
using mz.Config;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace mz.NewTemplateMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]  
    public class NewTemplateModMain : MySessionComponentBase
    {
        private bool _initialized;

        public static TestConfig TestConfig { get; private set; }
        public static UselessConfig UselessConfig { get; private set; }

        public override void LoadData()
        {
            base.LoadData();
            TestConfig = ConfigStorage.Register<TestConfig>(ConfigStorageKind.Local);
            UselessConfig = ConfigStorage.Register<UselessConfig>(ConfigStorageKind.World);
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            try
            {
                // Only do client-side stuff on clients
                if (MyAPIGateway.Utilities?.IsDedicated == true)
                    return;

                MyAPIGateway.Utilities.MessageEnteredSender += ModMeta.CheckForCommands;
                ModMeta.OnModCommand += HandleCommands;

                _initialized = true;
            }
            catch (Exception)
            {
                ConfigStorage.TryLog("Failed to initialize NewTemplateModMain.", "NewTemplateMod");
            }
        }


        protected override void UnloadData()
        {
            base.UnloadData();

            try
            {
                if (_initialized && MyAPIGateway.Utilities != null)
                {
                    ModMeta.OnModCommand -= HandleCommands;
                    MyAPIGateway.Utilities.MessageEnteredSender -= ModMeta.CheckForCommands;
                }
            }
            catch (Exception)
            {
                ConfigStorage.TryLog("Failed to unload NewTemplateModMain.", "NewTemplateMod");
            }
        }

        private void HandleCommands(ulong sender, string[] arguments)
        {
            if (arguments.Length == 0)
            {
                ConfigStorage.TryLog("Available commands: help, greet, version, sethello, togglehello", "NewTemplateMod");
                return;
            }

            switch (arguments[0].ToLowerInvariant())
            {
                case "help":
                    ConfigStorage.TryLog("Available commands: help, greet, version, sethello, togglehello", "NewTemplateMod");
                    break;
                case "greet":
                    if (TestConfig.RespondToHello)
                    {
                        ConfigStorage.TryLog(TestConfig.GreetingMessage, "NewTemplateMod");
                    }
                    else
                    {
                        ConfigStorage.TryLog("Greeting is disabled in the config.", "NewTemplateMod");
                    }
                    break;

                case "version":
                    ConfigStorage.TryLog($"Mod version: {ModMeta.Version}", "NewTemplateMod");
                    break;

                case "sethello":
                    if (arguments.Length < 2)
                    {
                        ConfigStorage.TryLog("Usage: sethello <greetingString>", "NewTemplateMod");
                        break;
                    }

                    var greeting = string.Join(" ", arguments.Skip(1));
                    TestConfig.GreetingMessage.Value = greeting;
                    ConfigStorage.TryLog($"Greeting message set to: {greeting}", "NewTemplateMod");
                    break;

                case "togglehello":
                    TestConfig.RespondToHello.Value = !TestConfig.RespondToHello;
                    ConfigStorage.TryLog($"RespondToHello set to: {TestConfig.RespondToHello}", "NewTemplateMod");
                    break;

                default:
                    ConfigStorage.TryLog($"Unknown command: {arguments[0]}", "NewTemplateMod");
                    break;
            }
        }
    }
}