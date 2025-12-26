namespace MarcoZechner.ConfigAPI.Main.NetworkCore
{
    public interface IWorldConfigNetworkCore
    {
        IWorldConfigNetwork CreateConsumerFacade(ulong consumerModId);

        void RegisterConsumer(ulong consumerModId, IWorldConfigClientSink sink);
        void UnregisterConsumer(ulong consumerModId);
    }
}