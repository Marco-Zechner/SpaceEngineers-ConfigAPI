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

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            TestConfig = ConfigStorage.Register<TestConfig>(ConfigStorageKind.Local);
            ConfigStorage.OnAnyConfigChanged += Change;
            TestConfig.RespondToHello.Changed += Change;
            TestConfig.RespondToHello2.Changed += Change;

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
            }
        }

        private void Change()
        {
            MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", "A configuration value has changed.");
        }

        private void Change<T>(T oldValue, T newValue)
        {
            MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", $"A configuration value has changed from '{oldValue}' to '{newValue}'.");
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
            }
        }

        private void HandleCommands(ulong sender, string[] arguments)
        {
            MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", $"Received command from {sender}: {string.Join(" ", arguments)}");

            if (arguments.Length == 0)
                return;

            switch (arguments[0].ToLowerInvariant())
            {
                case "help":
                    MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", "Available commands: help, greet, greet2, version, sethello, togglehello, togglehello2");
                    break;
                case "greet":
                    if (TestConfig.RespondToHello)
                    {
                        MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", TestConfig.GreetingMessage);
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", "Greeting is disabled in the config.");
                    }
                    break;
                case "greet2":
                    if (TestConfig.RespondToHello2)
                    {
                        MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", TestConfig.GreetingMessage);
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", "Greeting2 is disabled in the config.");
                    }
                    break;

                case "version":
                    MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", $"Mod version: {ModMeta.Version}");
                    break;

                case "sethello":
                    if (arguments.Length < 2)
                    {
                        MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", "Usage: sethello <greetingString>");
                        break;
                    }

                    var greeting = string.Join(" ", arguments.Skip(1));
                    TestConfig.GreetingMessage.Value = greeting;
                    MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", $"Greeting message set to: {greeting}");
                    break;

                case "togglehello":
                    TestConfig.RespondToHello.Value = !TestConfig.RespondToHello;
                    MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", $"RespondToHello set to: {TestConfig.RespondToHello}");
                    break;

                case "togglehello2":
                    TestConfig.RespondToHello2.Value = !TestConfig.RespondToHello2;
                    MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", $"RespondToHello2 set to: {TestConfig.RespondToHello2}");
                    break;

                default:
                    MyAPIGateway.Utilities.ShowMessage("NewTemplateMod", $"Unknown command: {arguments[0]}");
                    break;
            }
        }
    }
}