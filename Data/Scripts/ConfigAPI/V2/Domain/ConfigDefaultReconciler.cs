using System;
using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.V2.Domain
{
    public static class ConfigDefaultReconciler
    {
        public static ConfigDefaultReconciliationResult Reconcile(
            ConfigDocument baselineDefaults,
            ConfigDocument playerValues,
            ConfigDocument currentDefaults)
        {
            if (baselineDefaults == null)
                throw new ArgumentNullException(nameof(baselineDefaults));

            if (playerValues == null)
                throw new ArgumentNullException(nameof(playerValues));

            if (currentDefaults == null)
                throw new ArgumentNullException(nameof(currentDefaults));

            var changes = new List<ConfigDefaultChange>();
            var path = new List<string>();

            var reconciled = ReconcileObject(
                baselineDefaults.Root,
                playerValues.Root,
                currentDefaults.Root,
                path,
                changes);

            return new ConfigDefaultReconciliationResult(
                new ConfigDocument(reconciled.Baseline),
                new ConfigDocument(reconciled.Player),
                changes);
        }

        private static ObjectReconciliation ReconcileObject(
            ConfigObjectNode baseline,
            ConfigObjectNode player,
            ConfigObjectNode currentDefaults,
            List<string> path,
            IList<ConfigDefaultChange> changes)
        {
            var reconciledBaseline = baseline;
            var reconciledPlayer = player;

            foreach (var entry in currentDefaults.Entries)
            {
                ConfigNode baselineValue;
                ConfigNode playerValue;

                var hasBaseline = baseline.TryGet(entry.Name, out baselineValue);
                var hasPlayer = player.TryGet(entry.Name, out playerValue);

                path.Add(entry.Name);

                if (!hasBaseline && !hasPlayer)
                {
                    reconciledBaseline = ReplaceOrAppend(reconciledBaseline, entry.Name, entry.Value);
                    reconciledPlayer = ReplaceOrAppend(reconciledPlayer, entry.Name, entry.Value);
                    AddNewDefaultChanges(entry.Value, path, changes);
                    path.RemoveAt(path.Count - 1);
                    continue;
                }

                if (hasBaseline != hasPlayer)
                {
                    throw new InvalidOperationException(
                        "Baseline defaults and player values have inconsistent structure at " +
                        FormatPath(path) + ".");
                }

                var valueResult = ReconcileValue(
                    baselineValue,
                    playerValue,
                    entry.Value,
                    path,
                    changes);

                reconciledBaseline = ReplaceOrAppend(
                    reconciledBaseline,
                    entry.Name,
                    valueResult.Baseline);

                reconciledPlayer = ReplaceOrAppend(
                    reconciledPlayer,
                    entry.Name,
                    valueResult.Player);

                path.RemoveAt(path.Count - 1);
            }

            return new ObjectReconciliation(reconciledBaseline, reconciledPlayer);
        }

        private static ValueReconciliation ReconcileValue(
            ConfigNode baseline,
            ConfigNode player,
            ConfigNode currentDefault,
            List<string> path,
            IList<ConfigDefaultChange> changes)
        {
            var baselineObject = baseline as ConfigObjectNode;
            var playerObject = player as ConfigObjectNode;
            var currentObject = currentDefault as ConfigObjectNode;

            if (baselineObject != null && playerObject != null && currentObject != null)
            {
                var reconciledObject = ReconcileObject(
                    baselineObject,
                    playerObject,
                    currentObject,
                    path,
                    changes);

                return new ValueReconciliation(
                    reconciledObject.Baseline,
                    reconciledObject.Player);
            }

            if (baseline.Equals(currentDefault))
                return new ValueReconciliation(baseline, player);

            var changePath = CreatePath(path);

            if (player.Equals(baseline))
            {
                changes.Add(
                    new ConfigDefaultChange(
                        ConfigDefaultChangeKind.AppliedChangedDefault,
                        changePath,
                        baseline,
                        player,
                        currentDefault));

                return new ValueReconciliation(currentDefault, currentDefault);
            }

            changes.Add(
                new ConfigDefaultChange(
                    ConfigDefaultChangeKind.PendingChangedDefault,
                    changePath,
                    baseline,
                    player,
                    currentDefault));

            return new ValueReconciliation(baseline, player);
        }

        private static void AddNewDefaultChanges(
            ConfigNode currentDefault,
            List<string> path,
            IList<ConfigDefaultChange> changes)
        {
            var obj = currentDefault as ConfigObjectNode;
            if (obj == null)
            {
                changes.Add(
                    new ConfigDefaultChange(
                        ConfigDefaultChangeKind.AddedDefault,
                        CreatePath(path),
                        null,
                        null,
                        currentDefault));

                return;
            }

            foreach (var entry in obj.Entries)
            {
                path.Add(entry.Name);
                AddNewDefaultChanges(entry.Value, path, changes);
                path.RemoveAt(path.Count - 1);
            }
        }

        private static ConfigObjectNode ReplaceOrAppend(
            ConfigObjectNode source,
            string name,
            ConfigNode value)
        {
            var entries = new ConfigObjectEntry[source.Entries.Count + 1];
            var found = false;
            var writeIndex = 0;

            for (var i = 0; i < source.Entries.Count; i++)
            {
                var entry = source.Entries[i];

                if (string.Equals(entry.Name, name, StringComparison.Ordinal))
                {
                    entries[writeIndex] = new ConfigObjectEntry(entry.Name, value);
                    found = true;
                }
                else
                {
                    entries[writeIndex] = entry;
                }

                writeIndex++;
            }

            if (!found)
            {
                entries[writeIndex] = new ConfigObjectEntry(name, value);
                writeIndex++;
            }

            if (writeIndex == entries.Length)
                return new ConfigObjectNode(entries);

            var exactEntries = new ConfigObjectEntry[writeIndex];
            Array.Copy(entries, exactEntries, writeIndex);
            return new ConfigObjectNode(exactEntries);
        }

        private static ConfigValuePath CreatePath(List<string> path)
        {
            return new ConfigValuePath(path.ToArray());
        }

        private static string FormatPath(List<string> path)
        {
            return string.Join(".", path.ToArray());
        }

        private struct ObjectReconciliation
        {
            public readonly ConfigObjectNode Baseline;
            public readonly ConfigObjectNode Player;

            public ObjectReconciliation(ConfigObjectNode baseline, ConfigObjectNode player)
            {
                Baseline = baseline;
                Player = player;
            }
        }

        private struct ValueReconciliation
        {
            public readonly ConfigNode Baseline;
            public readonly ConfigNode Player;

            public ValueReconciliation(ConfigNode baseline, ConfigNode player)
            {
                Baseline = baseline;
                Player = player;
            }
        }
    }
}
