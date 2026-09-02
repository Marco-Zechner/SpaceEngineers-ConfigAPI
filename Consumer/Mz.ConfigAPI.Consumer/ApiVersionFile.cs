using Mz.SemanticVersioning;

namespace Mz.ConfigApi
{
    public static class ApiVersionFile
    {
        public const int Major = 2;
        public const int Minor = 0;
        public const int Patch = 0;

        public static SemanticVersion MinimumProviderApiVersion { get; } =
            new SemanticVersion(2, 0, 0);

        public static string VersionString =>
            $"{Major}.{Minor}.{Patch}";

        public static Changelog Changelog { get; } =
            new Changelog(
                VersionString,
                new[]
                {
                    new ChangelogEntry(
                        "2.0.0",
                        new[]
                        {
                            "Introduced the typed ConfigAPI consumer facade.",
                            "Added consumer-owned semantic config documents and values without provider-domain dependencies.",
                            "Added provider-backed Open and Save operations with exact endpoint validation.",
                            "Added automatic provider discovery and consumer-owned storage callback registration with reconnect-safe registration identifiers.",
                            "Accepted newer provider API versions without a hardcoded upper version ceiling."
                        })
                });
    }
}
