using System;

namespace Mz.ConfigApi
{
    public sealed class ConfigDefinition<T>
        where T : class
    {
        private readonly Func<T> _createDefaults;

        public string ConfigKey { get; private set; }
        public string DefaultFile { get; private set; }

        public ConfigDefinition(
            string configKey,
            string defaultFile,
            Func<T> createDefaults)
        {
            if (string.IsNullOrWhiteSpace(configKey))
            {
                throw new ArgumentException(
                    "Config key must not be empty.",
                    nameof(configKey));
            }

            if (string.IsNullOrWhiteSpace(defaultFile))
            {
                throw new ArgumentException(
                    "Default config file must not be empty.",
                    nameof(defaultFile));
            }

            if (createDefaults == null)
                throw new ArgumentNullException(nameof(createDefaults));

            ConfigKey = configKey.Trim();
            DefaultFile = defaultFile;
            _createDefaults = createDefaults;
        }

        public T CreateDefaults()
        {
            T defaults = _createDefaults();

            if (defaults == null)
            {
                throw new InvalidOperationException(
                    "The config default factory returned null for " +
                    typeof(T).FullName +
                    ".");
            }

            return defaults;
        }
    }
}