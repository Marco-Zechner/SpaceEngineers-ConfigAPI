using System;

namespace MarcoZechner.ConfigAPI.V2.Domain
{
    public enum ConfigDefaultChangeKind
    {
        AppliedChangedDefault = 0,
        PendingChangedDefault = 1,
        AddedDefault = 2
    }

    public sealed class ConfigDefaultChange
    {
        public ConfigDefaultChangeKind Kind { get; }
        public ConfigValuePath Path { get; }
        public ConfigNode BaselineDefault { get; }
        public ConfigNode PlayerValue { get; }
        public ConfigNode CurrentDefault { get; }

        public ConfigDefaultChange(
            ConfigDefaultChangeKind kind,
            ConfigValuePath path,
            ConfigNode baselineDefault,
            ConfigNode playerValue,
            ConfigNode currentDefault)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (currentDefault == null)
                throw new ArgumentNullException(nameof(currentDefault));

            Kind = kind;
            Path = path;
            BaselineDefault = baselineDefault;
            PlayerValue = playerValue;
            CurrentDefault = currentDefault;
        }
    }
}
