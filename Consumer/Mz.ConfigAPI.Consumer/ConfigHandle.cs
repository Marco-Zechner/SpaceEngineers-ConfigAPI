using System;

namespace Mz.ConfigApi
{
    public sealed class ConfigHandle<T>
        where T : class
    {
        private readonly ConfigApiClient _client;
        private readonly ConfigDefinition<T> _definition;

        public ConfigLocation Location { get; private set; }
        public string CurrentFile { get; private set; }
        public T Value { get; private set; }

        internal ConfigHandle(
            ConfigApiClient client,
            ConfigDefinition<T> definition,
            ConfigLocation location,
            string currentFile,
            T value)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(currentFile))
            {
                throw new ArgumentException(
                    "Current config file must not be empty.",
                    nameof(currentFile));
            }

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _client = client;
            _definition = definition;
            Location = location;
            CurrentFile = currentFile;
            Value = value;
        }

        public T Reload()
        {
            T value =
                _client.Open(
                    _definition.ConfigKey,
                    Location,
                    CurrentFile,
                    _definition.CreateDefaults());

            Value = value;
            return value;
        }
    }
}
