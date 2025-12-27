using MarcoZechner.ConfigAPI.Main.Core;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.NetworkCore
{
    public interface IWorldConfigNetworkCore
    {
        IWorldConfigNetwork CreateConsumerFacade(ulong consumerModId);

        void RegisterConsumer(ulong consumerModId, IWorldConfigClientSink sink, InternalConfigService configService, IConfigUserHooks userHooks); //TODO change into interface
        void UnregisterConsumer(ulong consumerModId);
    }
}