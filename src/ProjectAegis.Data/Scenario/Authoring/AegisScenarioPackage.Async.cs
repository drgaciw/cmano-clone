#if NET10_0_OR_GREATER
namespace ProjectAegis.Data.Scenario.Authoring;

using System.IO.Compression;
using System.Text.Json;

// DRG-695: .NET 10 async ZIP APIs. net10.0 only; the netstandard2.1 Unity build keeps the sync API.
public static partial class AegisScenarioPackage
{
    public static async Task WriteAsync(
        string packagePath, ScenarioDocumentDto document, string? title = null, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(packagePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        await using var archive = await ZipFile.OpenAsync(packagePath, ZipArchiveMode.Create, cancellationToken).ConfigureAwait(false);
        var manifest = new PackageManifestDto
        {
            PackageVersion = 1,
            Title = title ?? document.Metadata.Title ?? Path.GetFileNameWithoutExtension(packagePath),
            SchemaVersion = document.Metadata.SchemaVersion,
        };

        await WriteJsonEntryAsync(archive, ManifestEntryName, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken).ConfigureAwait(false);
        await WriteJsonEntryAsync(archive, ScenarioEntryName, ScenarioStableJsonWriter.Serialize(document), cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ScenarioDocumentDto> ReadAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        await using var archive = await ZipFile.OpenReadAsync(packagePath, cancellationToken).ConfigureAwait(false);
        var entry = archive.GetEntry(ScenarioEntryName)
            ?? throw new InvalidDataException($"Missing {ScenarioEntryName} in {packagePath}");

        await using var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<ScenarioDocumentDto>(stream, LoadOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Invalid scenario document in {packagePath}");
    }

    public static async Task<PackageManifestDto> ReadManifestAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        await using var archive = await ZipFile.OpenReadAsync(packagePath, cancellationToken).ConfigureAwait(false);
        var entry = archive.GetEntry(ManifestEntryName)
            ?? throw new InvalidDataException($"Missing {ManifestEntryName} in {packagePath}");

        await using var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<PackageManifestDto>(stream, LoadOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Invalid manifest in {packagePath}");
    }

    private static async Task WriteJsonEntryAsync(ZipArchive archive, string name, string json, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        await using var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(json.Replace("\r\n", "\n", StringComparison.Ordinal).AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
#endif
