namespace ProjectAegis.Data.Scenario.Authoring;

#if NET10_0_OR_GREATER
using System.IO.Compression;
using System.Text.Json;
#endif

// DRG-695: .NET 10 async ZIP APIs. net10.0 only; the netstandard2.1 Unity build keeps the sync API.
public static partial class AegisScenarioPackage
{
#if NET10_0_OR_GREATER
    /// <summary>Writes a complete package before replacing the destination; cancellation or failure preserves the previous file.</summary>
    public static async Task WriteAsync(
        string packagePath, ScenarioDocumentDto document, string? title = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dir = Path.GetDirectoryName(packagePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var temporaryPath = packagePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var archive = await ZipFile.OpenAsync(temporaryPath, ZipArchiveMode.Create, cancellationToken).ConfigureAwait(false))
            {
                var manifest = new PackageManifestDto
                {
                    PackageVersion = 1,
                    Title = title ?? document.Metadata.Title ?? Path.GetFileNameWithoutExtension(packagePath),
                    SchemaVersion = document.Metadata.SchemaVersionNumber,
                };

                await WriteJsonEntryAsync(archive, ManifestEntryName, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken).ConfigureAwait(false);
                await WriteJsonEntryAsync(archive, ScenarioEntryName, ScenarioStableJsonWriter.Serialize(document), cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, packagePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    /// <summary>Reads the scenario document from a package asynchronously.</summary>
    public static async Task<ScenarioDocumentDto> ReadAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        await using var archive = await ZipFile.OpenReadAsync(packagePath, cancellationToken).ConfigureAwait(false);
        var entry = archive.GetEntry(ScenarioEntryName)
            ?? throw new InvalidDataException($"Missing {ScenarioEntryName} in {packagePath}");

        await using var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<ScenarioDocumentDto>(stream, LoadOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Invalid scenario document in {packagePath}");
    }

    /// <summary>Reads the manifest from a package asynchronously.</summary>
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
#endif
}
