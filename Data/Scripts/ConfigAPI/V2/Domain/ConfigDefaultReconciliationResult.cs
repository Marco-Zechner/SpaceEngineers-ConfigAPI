using System;
using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.V2.Domain
{
    public sealed class ConfigDefaultReconciliationResult
    {
        private readonly ConfigDefaultChange[] _changes;
        private readonly IReadOnlyList<ConfigDefaultChange> _readOnlyChanges;

        public ConfigDocument BaselineDefaults { get; }
        public ConfigDocument PlayerValues { get; }
        public IReadOnlyList<ConfigDefaultChange> Changes => _readOnlyChanges;

        internal ConfigDefaultReconciliationResult(
            ConfigDocument baselineDefaults,
            ConfigDocument playerValues,
            IList<ConfigDefaultChange> changes)
        {
            if (baselineDefaults == null)
                throw new ArgumentNullException(nameof(baselineDefaults));

            if (playerValues == null)
                throw new ArgumentNullException(nameof(playerValues));

            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            BaselineDefaults = baselineDefaults;
            PlayerValues = playerValues;

            _changes = new ConfigDefaultChange[changes.Count];

            for (var i = 0; i < changes.Count; i++)
                _changes[i] = changes[i];

            _readOnlyChanges = Array.AsReadOnly(_changes);
        }
    }
}
