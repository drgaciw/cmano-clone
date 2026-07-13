using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless Addressables catalog resolver for APP-6 map atlas metadata (S27-07).
/// Reads <c>App6AtlasAddressablesManifest.json</c> from the repo so tests and hosts can
/// resolve <see cref="App6AtlasSpriteSheet.AddressableKey"/> without Unity Addressables runtime.
/// </summary>
public static class App6AddressablesCatalog
{
    public const string DefaultManifestRelativePath =
        "unity/ProjectAegis/Assets/Addressables/Map/App6AtlasAddressablesManifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Resolved Addressables entry plus sprite-sheet slice metadata.</summary>
    public sealed record App6AddressablesAtlasResolution(
        string AddressableKey,
        string TextureAssetPath,
        string AddressableGroup,
        IReadOnlyDictionary<string, App6AtlasSpriteSlice> FrameSlices);

    public static bool TryResolveFromRepo(
        string repoRoot,
        out App6AtlasCatalog catalog,
        out App6AddressablesAtlasResolution resolution)
    {
        var manifestPath = Path.Combine(repoRoot, DefaultManifestRelativePath);
        var assetsRoot = Path.Combine(repoRoot, "unity", "ProjectAegis", "Assets");
        return TryResolveFromManifest(manifestPath, assetsRoot, out catalog, out resolution);
    }

    public static bool TryResolveFromManifest(
        string manifestJsonPath,
        string? projectAssetsRoot,
        out App6AtlasCatalog catalog,
        out App6AddressablesAtlasResolution resolution)
    {
        catalog = App6AtlasCatalog.Unavailable;
        resolution = new App6AddressablesAtlasResolution(
            App6AtlasSpriteSheet.AddressableKey,
            App6AtlasSpriteSheet.TextureAssetPath,
            App6AtlasSpriteSheet.AddressableGroup,
            App6AtlasSpriteSheet.FrameSlices);

        if (string.IsNullOrWhiteSpace(manifestJsonPath) || !File.Exists(manifestJsonPath))
        {
            return false;
        }

        ManifestDocument? manifest;
        try
        {
            var json = File.ReadAllText(manifestJsonPath);
            manifest = JsonSerializer.Deserialize<ManifestDocument>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (manifest?.Entries == null || manifest.Entries.Count == 0)
        {
            return false;
        }

        ManifestEntry? entry = null;
        foreach (var candidate in manifest.Entries)
        {
            if (string.Equals(candidate.Address, App6AtlasSpriteSheet.AddressableKey, StringComparison.Ordinal))
            {
                entry = candidate;
                break;
            }
        }

        if (entry == null
            || !string.Equals(entry.AssetPath, App6AtlasSpriteSheet.TextureAssetPath, StringComparison.Ordinal))
        {
            return false;
        }

        if (projectAssetsRoot != null)
        {
            var textureRelative = entry.AssetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? entry.AssetPath["Assets/".Length..]
                : entry.AssetPath;
            var texturePath = Path.Combine(projectAssetsRoot, textureRelative);
            if (!File.Exists(texturePath))
            {
                return false;
            }
        }

        var groupName = string.IsNullOrWhiteSpace(manifest.Group)
            ? App6AtlasSpriteSheet.AddressableGroup
            : manifest.Group;

        resolution = new App6AddressablesAtlasResolution(
            entry.Address,
            entry.AssetPath,
            groupName,
            App6AtlasSpriteSheet.FrameSlices);
        catalog = App6AtlasCatalog.Default;
        return true;
    }

    private sealed class ManifestDocument
    {
        [JsonPropertyName("group")]
        public string Group { get; init; } = string.Empty;

        [JsonPropertyName("entries")]
        public List<ManifestEntry> Entries { get; init; } = new();
    }

    private sealed class ManifestEntry
    {
        [JsonPropertyName("address")]
        public string Address { get; init; } = string.Empty;

        [JsonPropertyName("assetPath")]
        public string AssetPath { get; init; } = string.Empty;

        [JsonPropertyName("labels")]
        public string[]? Labels { get; init; }
    }
}