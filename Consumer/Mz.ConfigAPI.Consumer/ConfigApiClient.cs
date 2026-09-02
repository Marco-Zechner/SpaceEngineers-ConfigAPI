using System;
using Mz.ApiProtocol;
using Mz.ApiProtocol.SpaceEngineers;
using Mz.SemanticVersioning;

namespace Mz.ConfigApi
{
    public sealed class ConfigApiClient : IDisposable
    {
        public const string ProviderApiId = "MarcoZechner.ConfigAPI";
        public const string RegisterConsumerEndpoint = "RegisterConsumer";

        private readonly string _consumerId;
        private readonly Func<int, string, string> _read;
        private readonly Action<int, string, string> _write;
        private readonly ApiDiscoveryConsumer _consumer;

        private Action _providerUnregister;
        private bool _isDisposed;
        private Exception _lastError;

        public event Action Connected;
        public event Action Disconnected;

        public bool IsStarted =>
            _consumer.IsStarted;

        public bool IsConnected =>
            _providerUnregister != null;

        public SemanticVersion ProviderModVersion { get; private set; }
        public SemanticVersion ProviderApiVersion { get; private set; }

        public Exception LastError =>
            _lastError ?? _consumer.LastError;

        public ConfigApiClient(
            IModMessageBus messageBus,
            string consumerId,
            string consumerDisplayName,
            SemanticVersion consumerModVersion,
            bool isRequired,
            string featureDescription,
            Func<int, string, string> read,
            Action<int, string, string> write)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            if (string.IsNullOrWhiteSpace(consumerId))
                throw new ArgumentException("A stable consumer mod identifier is required.", nameof(consumerId));

            if (string.IsNullOrWhiteSpace(consumerDisplayName))
                throw new ArgumentException("A consumer display name is required.", nameof(consumerDisplayName));

            if (consumerModVersion == null)
                throw new ArgumentNullException(nameof(consumerModVersion));

            if (read == null)
                throw new ArgumentNullException(nameof(read));

            if (write == null)
                throw new ArgumentNullException(nameof(write));

            _consumerId = consumerId.Trim();
            _read = read;
            _write = write;

            var dependency =
                new ApiDependencyDescriptor(
                    new ApiModIdentity(
                        _consumerId,
                        consumerDisplayName.Trim(),
                        consumerModVersion),
                    new ApiRequirement(
                        ProviderApiId,
                        new ApiVersionRange(
                            ApiVersionFile.MinimumProviderApiVersion,
                            null)),
                    isRequired
                        ? ApiDependencyKind.Required
                        : ApiDependencyKind.Optional,
                    featureDescription);

            _consumer =
                new ApiDiscoveryConsumer(
                    messageBus,
                    dependency);

            _consumer.Connected += OnConnected;
            _consumer.Disconnected += OnDisconnected;
        }

        public void Start()
        {
            ThrowIfDisposed();

            if (IsStarted)
                return;

            _lastError = null;
            _consumer.Start();

            if (!_consumer.IsConnected)
                _consumer.RequestDiscovery();
        }

        public Guid RequestDiscovery()
        {
            ThrowIfDisposed();
            _lastError = null;

            return _consumer.RequestDiscovery();
        }

        public Guid Rediscover()
        {
            ThrowIfDisposed();
            _lastError = null;

            return _consumer.Rediscover();
        }

        public void Stop()
        {
            ThrowIfDisposed();

            ReleaseProviderRegistration();
            ClearConnection();
            _consumer.Stop();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            ReleaseProviderRegistration();

            _consumer.Connected -= OnConnected;
            _consumer.Disconnected -= OnDisconnected;
            _consumer.Dispose();

            ClearConnection();
        }

        private void OnConnected(ApiConnectedEventArgs eventArgs)
        {
            try
            {
                Func<
                    string,
                    Guid,
                    Func<int, string, string>,
                    Action<int, string, string>,
                    Action> registerConsumer;

                if (!eventArgs.Connection.TryGetEndpoint(
                    RegisterConsumerEndpoint,
                    out registerConsumer))
                {
                    _lastError =
                        new InvalidOperationException(
                            "The ConfigAPI provider is missing the exact RegisterConsumer endpoint.");

                    _consumer.Disconnect();
                    return;
                }

                var registrationId = Guid.NewGuid();

                Action unregister =
                    registerConsumer(
                        _consumerId,
                        registrationId,
                        _read,
                        _write);

                if (unregister == null)
                    throw new InvalidOperationException("The ConfigAPI provider returned no unregister action.");

                _providerUnregister = unregister;
                ProviderModVersion = eventArgs.Connection.Provider.Version;
                ProviderApiVersion = eventArgs.Connection.Descriptor.Version;
                _lastError = null;

                RaiseConnected();
            }
            catch (Exception exception)
            {
                _lastError = exception;
                ReleaseProviderRegistration();
                ClearConnection();
                _consumer.Disconnect();
            }
        }

        private void OnDisconnected(ApiDisconnectedEventArgs eventArgs)
        {
            ReleaseProviderRegistration();
            ClearConnection();
            RaiseDisconnected();
        }

        private void ReleaseProviderRegistration()
        {
            Action unregister = _providerUnregister;
            _providerUnregister = null;

            if (unregister == null)
                return;

            try
            {
                unregister();
            }
            catch (Exception exception)
            {
                _lastError = exception;
            }
        }

        private void ClearConnection()
        {
            _providerUnregister = null;
            ProviderModVersion = null;
            ProviderApiVersion = null;
        }

        private void RaiseConnected()
        {
            Action handler = Connected;

            if (handler == null)
                return;

            foreach (Action subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber();
                }
                catch (Exception exception)
                {
                    if (_lastError == null)
                        _lastError = exception;
                }
            }
        }

        private void RaiseDisconnected()
        {
            Action handler = Disconnected;

            if (handler == null)
                return;

            foreach (Action subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber();
                }
                catch (Exception exception)
                {
                    if (_lastError == null)
                        _lastError = exception;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("The ConfigAPI client has been disposed.");
        }
    }
}
