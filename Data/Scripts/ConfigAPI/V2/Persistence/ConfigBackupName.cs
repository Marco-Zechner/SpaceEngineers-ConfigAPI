using System;
using System.Globalization;

namespace MarcoZechner.ConfigAPI.V2.Persistence
{
    public static class ConfigBackupName
    {
        public static string Create(
            string file,
            DateTime timestampUtc)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentException("Config file must not be empty.", nameof(file));

            var utc = timestampUtc.Kind == DateTimeKind.Utc
                ? timestampUtc
                : timestampUtc.ToUniversalTime();

            return file
                + "."
                + utc.ToString(
                    "yyyyMMdd'T'HHmmss.fffffff'Z'",
                    CultureInfo.InvariantCulture)
                + ".bak";
        }
    }
}
