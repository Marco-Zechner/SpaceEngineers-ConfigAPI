- check event unload, it doesn't work

- server config support

- worldlog writer sometimes not closing properly?

- mod crashed silently on unload? but the game didn't.
-> probably caused all these "duplicate entry"

- crash on world save
```
Exception: System.InvalidOperationException: Duplicate field-number detected; 1 on: Digi.NetworkLib.PacketBase
   at ProtoBuf.Serializers.TypeSerializer..ctor(Type forType, Int32[] fieldNumbers, IProtoSerializer[] serializers, MethodInfo[] baseCtorCallbacks, Boolean isRootType, Boolean useConstructor, CallbackSet callbacks, Type constructType, MethodInfo factory)
   at ProtoBuf.Meta.MetaType.BuildSerializer()
   at ProtoBuf.Meta.MetaType.get_Serializer()
   at ProtoBuf.Meta.RuntimeTypeModel.Serialize(ProtoWriter dest, State& state, Int32 key, Object value)
   at ProtoBuf.Meta.TypeModel.SerializeCore(ProtoWriter writer, State& state, Object value)
   at ProtoBuf.Meta.TypeModel.Serialize(Stream dest, Object value, SerializationContext context)
   at ProtoBuf.Meta.TypeModel.Serialize_Patch1(TypeModel this, Stream dest, Object value)
   at ProtoBuf.Serializer.Serialize[T](Stream destination, T instance)
   at Sandbox.ModAPI.MyAPIUtilities.VRage.Game.ModAPI.IMyUtilities.SerializeToBinary[T](T obj)
   at Digi.NetworkLib.Network.RelayToSenderOnly(PacketBase packet, UInt64 senderSteamId, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 161
   at Digi.NetworkLib.Network.HandlePacket(PacketBase packet, UInt64 senderSteamId, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 241
   at Digi.NetworkLib.Network.SendToServer(PacketBase packet, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 95
   at MarcoZechner.ConfigAPI.Main.NetworkCore.WorldConfigNetworkCore.ConsumerFacade.SendRequest(WorldNetRequest req) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\NetworkCore\WorldConfigNetworkCore.cs:line 324
   at MarcoZechner.ConfigAPI.Main.Core.WorldConfigClientService.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\Core\WorldConfigClientService.cs:line 221
   at MarcoZechner.ConfigAPI.Main.Api.ConfigServiceImpl.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\Api\ConfigServiceImpl.cs:line 98
   at MarcoZechner.ConfigAPI.Client.Api.ConfigService.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\Libraries\ConfigAPI\Client\Api\ConfigService.cs:line 142
   at MarcoZechner.ConfigAPI.Client.Core.CfgSync`1.Save() in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\Libraries\ConfigAPI\Client\Core\CfgSync.TConfigBase.cs:line 165
   at mz.ConfigAPIExample.ConfigApiWorldExample.HandleWorldCommands(UInt64 sender, String[] arguments) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ConfigApiWorldExample.cs:line 120
   at mz.ConfigAPIExample.ConfigApiExampleMain.HandleCommands(UInt64 sender, String[] arguments) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ConfigApiExampleMain.cs:line 94
   at mz.ConfigAPIExample.ModMeta.CheckForCommands(UInt64 sender, String command, Boolean& sendToOthers) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ModMeta.cs:line 37
   at Sandbox.ModAPI.MyAPIUtilities.EnterMessage(UInt64 sender, String messageText, Boolean& sendToOthers)
   at Sandbox.Game.Gui.MyGuiScreenChat.OnInputFieldActivated(MyGuiControlTextbox textBox)
   at System.DelegateExtensions.InvokeIfNotNull[T1](Action`1 handler, T1 arg1)
   at Sandbox.Graphics.GUI.MyGuiControlTextbox.HandleInput()
   at Sandbox.Graphics.GUI.MyGuiScreenBase.HandleControlsInput(Boolean receivedFocusInThisUpdate)
   at Sandbox.Graphics.GUI.MyGuiScreenBase.HandleInput(Boolean receivedFocusInThisUpdate)
   at Sandbox.Graphics.GUI.MyScreenManager.HandleInput()
   at Sandbox.Graphics.GUI.MyDX9Gui.HandleInput()
   at Sandbox.MySandboxGame.Update()
   at Sandbox.Engine.Platform.Game.UpdateInternal()
   at Sandbox.Engine.Platform.Game.RunSingleFrame()
   at Sandbox.Engine.Platform.FixedLoop.<>c__DisplayClass11_0.<Run>b__0()
   at Sandbox.Engine.Platform.GenericLoop.Run(VoidAction tickCallback)
   at Sandbox.Engine.Platform.Game.RunLoop()
   at Sandbox.MySandboxGame.Run(Boolean customRenderLoop, Action disposeSplashScreen)
   at SpaceEngineers.MyProgram.Main(String[] args)
2025-12-31 17:58:59.443 - Thread:   1 ->  Exception occurred: System.InvalidOperationException: Duplicate field-number detected; 1 on: Digi.NetworkLib.PacketBase
   at ProtoBuf.Serializers.TypeSerializer..ctor(Type forType, Int32[] fieldNumbers, IProtoSerializer[] serializers, MethodInfo[] baseCtorCallbacks, Boolean isRootType, Boolean useConstructor, CallbackSet callbacks, Type constructType, MethodInfo factory)
   at ProtoBuf.Meta.MetaType.BuildSerializer()
   at ProtoBuf.Meta.MetaType.get_Serializer()
   at ProtoBuf.Meta.RuntimeTypeModel.Serialize(ProtoWriter dest, State& state, Int32 key, Object value)
   at ProtoBuf.Meta.TypeModel.SerializeCore(ProtoWriter writer, State& state, Object value)
   at ProtoBuf.Meta.TypeModel.Serialize(Stream dest, Object value, SerializationContext context)
   at ProtoBuf.Meta.TypeModel.Serialize_Patch1(TypeModel this, Stream dest, Object value)
   at ProtoBuf.Serializer.Serialize[T](Stream destination, T instance)
   at Sandbox.ModAPI.MyAPIUtilities.VRage.Game.ModAPI.IMyUtilities.SerializeToBinary[T](T obj)
   at Digi.NetworkLib.Network.RelayToSenderOnly(PacketBase packet, UInt64 senderSteamId, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 161
   at Digi.NetworkLib.Network.HandlePacket(PacketBase packet, UInt64 senderSteamId, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 241
   at Digi.NetworkLib.Network.SendToServer(PacketBase packet, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 95
   at MarcoZechner.ConfigAPI.Main.NetworkCore.WorldConfigNetworkCore.ConsumerFacade.SendRequest(WorldNetRequest req) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\NetworkCore\WorldConfigNetworkCore.cs:line 324
   at MarcoZechner.ConfigAPI.Main.Core.WorldConfigClientService.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\Core\WorldConfigClientService.cs:line 221
   at MarcoZechner.ConfigAPI.Main.Api.ConfigServiceImpl.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\Api\ConfigServiceImpl.cs:line 98
   at MarcoZechner.ConfigAPI.Client.Api.ConfigService.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\Libraries\ConfigAPI\Client\Api\ConfigService.cs:line 142
   at MarcoZechner.ConfigAPI.Client.Core.CfgSync`1.Save() in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\Libraries\ConfigAPI\Client\Core\CfgSync.TConfigBase.cs:line 165
   at mz.ConfigAPIExample.ConfigApiWorldExample.HandleWorldCommands(UInt64 sender, String[] arguments) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ConfigApiWorldExample.cs:line 120
   at mz.ConfigAPIExample.ConfigApiExampleMain.HandleCommands(UInt64 sender, String[] arguments) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ConfigApiExampleMain.cs:line 94
   at mz.ConfigAPIExample.ModMeta.CheckForCommands(UInt64 sender, String command, Boolean& sendToOthers) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ModMeta.cs:line 37
   at Sandbox.ModAPI.MyAPIUtilities.EnterMessage(UInt64 sender, String messageText, Boolean& sendToOthers)
   at Sandbox.Game.Gui.MyGuiScreenChat.OnInputFieldActivated(MyGuiControlTextbox textBox)
   at System.DelegateExtensions.InvokeIfNotNull[T1](Action`1 handler, T1 arg1)
   at Sandbox.Graphics.GUI.MyGuiControlTextbox.HandleInput()
   at Sandbox.Graphics.GUI.MyGuiScreenBase.HandleControlsInput(Boolean receivedFocusInThisUpdate)
   at Sandbox.Graphics.GUI.MyGuiScreenBase.HandleInput(Boolean receivedFocusInThisUpdate)
   at Sandbox.Graphics.GUI.MyScreenManager.HandleInput()
   at Sandbox.Graphics.GUI.MyDX9Gui.HandleInput()
   at Sandbox.MySandboxGame.Update()
   at Sandbox.Engine.Platform.Game.UpdateInternal()
   at Sandbox.Engine.Platform.Game.RunSingleFrame()
   at Sandbox.Engine.Platform.FixedLoop.<>c__DisplayClass11_0.<Run>b__0()
   at Sandbox.Engine.Platform.GenericLoop.Run(VoidAction tickCallback)
   at Sandbox.Engine.Platform.Game.RunLoop()
   at Sandbox.MySandboxGame.Run(Boolean customRenderLoop, Action disposeSplashScreen)
   at SpaceEngineers.MyProgram.Main(String[] args)
   at Pulsar.Legacy.Program.SetupGame(String[] args)
   at Pulsar.Legacy.Program.Main(String[] args)
2025-12-31 17:58:59.445 - Thread:   1 ->  Showing message
2025-12-31 17:58:59.449 - Thread:   1 ->  MyInitializer.OnCrash
2025-12-31 17:58:59.449 - Thread:   1 ->  var exception = System.InvalidOperationException: Duplicate field-number detected; 1 on: Digi.NetworkLib.PacketBase
   at ProtoBuf.Serializers.TypeSerializer..ctor(Type forType, Int32[] fieldNumbers, IProtoSerializer[] serializers, MethodInfo[] baseCtorCallbacks, Boolean isRootType, Boolean useConstructor, CallbackSet callbacks, Type constructType, MethodInfo factory)
   at ProtoBuf.Meta.MetaType.BuildSerializer()
   at ProtoBuf.Meta.MetaType.get_Serializer()
   at ProtoBuf.Meta.RuntimeTypeModel.Serialize(ProtoWriter dest, State& state, Int32 key, Object value)
   at ProtoBuf.Meta.TypeModel.SerializeCore(ProtoWriter writer, State& state, Object value)
   at ProtoBuf.Meta.TypeModel.Serialize(Stream dest, Object value, SerializationContext context)
   at ProtoBuf.Meta.TypeModel.Serialize_Patch1(TypeModel this, Stream dest, Object value)
   at ProtoBuf.Serializer.Serialize[T](Stream destination, T instance)
   at Sandbox.ModAPI.MyAPIUtilities.VRage.Game.ModAPI.IMyUtilities.SerializeToBinary[T](T obj)
   at Digi.NetworkLib.Network.RelayToSenderOnly(PacketBase packet, UInt64 senderSteamId, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 161
   at Digi.NetworkLib.Network.HandlePacket(PacketBase packet, UInt64 senderSteamId, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 241
   at Digi.NetworkLib.Network.SendToServer(PacketBase packet, Byte[] serialized) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Libraries\NetworkLib\Network.cs:line 95
   at MarcoZechner.ConfigAPI.Main.NetworkCore.WorldConfigNetworkCore.ConsumerFacade.SendRequest(WorldNetRequest req) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\NetworkCore\WorldConfigNetworkCore.cs:line 324
   at MarcoZechner.ConfigAPI.Main.Core.WorldConfigClientService.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\Core\WorldConfigClientService.cs:line 221
   at MarcoZechner.ConfigAPI.Main.Api.ConfigServiceImpl.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPI\Data\Scripts\ConfigAPI\Main\Api\ConfigServiceImpl.cs:line 98
   at MarcoZechner.ConfigAPI.Client.Api.ConfigService.ServerConfigSave(String typeKey, UInt64 baseIteration) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\Libraries\ConfigAPI\Client\Api\ConfigService.cs:line 142
   at MarcoZechner.ConfigAPI.Client.Core.CfgSync`1.Save() in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\Libraries\ConfigAPI\Client\Core\CfgSync.TConfigBase.cs:line 165
   at mz.ConfigAPIExample.ConfigApiWorldExample.HandleWorldCommands(UInt64 sender, String[] arguments) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ConfigApiWorldExample.cs:line 120
   at mz.ConfigAPIExample.ConfigApiExampleMain.HandleCommands(UInt64 sender, String[] arguments) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ConfigApiExampleMain.cs:line 94
   at mz.ConfigAPIExample.ModMeta.CheckForCommands(UInt64 sender, String command, Boolean& sendToOthers) in C:\Users\marco\AppData\Roaming\SpaceEngineers\Mods\ConfigAPIExample\Data\Scripts\ConfigAPIExample\ModMeta.cs:line 37
   at Sandbox.ModAPI.MyAPIUtilities.EnterMessage(UInt64 sender, String messageText, Boolean& sendToOthers)
   at Sandbox.Game.Gui.MyGuiScreenChat.OnInputFieldActivated(MyGuiControlTextbox textBox)
   at System.DelegateExtensions.InvokeIfNotNull[T1](Action`1 handler, T1 arg1)
   at Sandbox.Graphics.GUI.MyGuiControlTextbox.HandleInput()
   at Sandbox.Graphics.GUI.MyGuiScreenBase.HandleControlsInput(Boolean receivedFocusInThisUpdate)
   at Sandbox.Graphics.GUI.MyGuiScreenBase.HandleInput(Boolean receivedFocusInThisUpdate)
   at Sandbox.Graphics.GUI.MyScreenManager.HandleInput()
   at Sandbox.Graphics.GUI.MyDX9Gui.HandleInput()
   at Sandbox.MySandboxGame.Update()
   at Sandbox.Engine.Platform.Game.UpdateInternal()
   at Sandbox.Engine.Platform.Game.RunSingleFrame()
   at Sandbox.Engine.Platform.FixedLoop.<>c__DisplayClass11_0.<Run>b__0()
   at Sandbox.Engine.Platform.GenericLoop.Run(VoidAction tickCallback)
   at Sandbox.Engine.Platform.Game.RunLoop()
   at Sandbox.MySandboxGame.Run(Boolean customRenderLoop, Action disposeSplashScreen)
   at SpaceEngineers.MyProgram.Main(String[] args)
   at Pulsar.Legacy.Program.SetupGame(String[] args)
   at Pulsar.Legacy.Program.Main(String[] args)

```