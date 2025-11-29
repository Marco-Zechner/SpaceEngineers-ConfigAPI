using System;
using mz.Logging;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace mz.NewTemplateMod
{
    public partial class NewTemplateModMain : MySessionComponentBase
    {
        private bool _initialized;

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
                Chat.TryLine("Failed to initialize NewTemplateModMain.", "NewTemplateMod");
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
                Chat.TryLine("Failed to unload NewTemplateModMain.", "NewTemplateMod");
            }
        }
    }
}