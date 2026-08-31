using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.V2.Domain;

namespace MarcoZechner.ConfigAPI.V2.Persistence
{
    public sealed class ConfigPersistedStateReconciliationResult
    {
        public ConfigPersistedState State { get; }
        public IReadOnlyList<ConfigDefaultChange> Changes { get; }
        public bool RequiresBackup { get; }

        internal ConfigPersistedStateReconciliationResult(
            ConfigPersistedState state,
            IReadOnlyList<ConfigDefaultChange> changes,
            bool requiresBackup)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            State = state;
            Changes = changes;
            RequiresBackup = requiresBackup;
        }
    }

    public static class ConfigPersistedStateReconciler
    {
        public static ConfigPersistedStateReconciliationResult Reconcile(
            ConfigPersistedState state,
            ConfigDocument currentDefaults)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (currentDefaults == null)
                throw new ArgumentNullException(nameof(currentDefaults));

            var reconciliation = ConfigDefaultReconciler.Reconcile(
                state.BaselineDefaults,
                state.PlayerValues,
                currentDefaults);

            var reconciledState = new ConfigPersistedState(
                state.Identity,
                reconciliation.PlayerValues,
                reconciliation.BaselineDefaults,
                state.CurrentFile);

            return new ConfigPersistedStateReconciliationResult(
                reconciledState,
                reconciliation.Changes,
                reconciliation.RequiresBackup);
        }
    }
}
