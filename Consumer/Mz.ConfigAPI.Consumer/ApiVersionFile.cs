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
                            "Added reflection-based CLR config mapping and typed Open<T> and Save<T> operations for public field or public read/write property models.",
                            "Supported enums, nullable values, nested objects, one-dimensional arrays, List<T>, and Dictionary<string, T> in typed configs.",
                            "Reserved World configs for the server-authoritative path; direct Open and Save operations now accept only Local and Global.",
                            "Added automatic provider discovery and consumer-owned storage callback registration with reconnect-safe registration identifiers.",
                            "Accepted newer provider API versions without a hardcoded upper version ceiling."
                        })
                });
    }
}
