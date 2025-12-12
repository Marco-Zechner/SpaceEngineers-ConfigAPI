using MarcoZechner.ConfigAPI.Main.Api;
using MarcoZechner.ConfigAPI.Shared;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Main
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ApiProvider : MySessionComponentBase
    {
        private ConfigApi _apiInstance;
        private const long MOD_ID = 1000; //TODO: find a better way
        
        public override void LoadData()
        {
            base.LoadData();
            _apiInstance = new ConfigApi();
            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_ID, ApiMessageHandler);
            MyAPIGateway.Utilities.SendModMessage(MOD_ID, _apiInstance); // Send Api to mods that might have loaded before us
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_ID, ApiMessageHandler);
        }

        private void ApiMessageHandler(object obj) 
            => MyAPIGateway.Utilities.SendModMessage(MOD_ID, _apiInstance);
    }
}