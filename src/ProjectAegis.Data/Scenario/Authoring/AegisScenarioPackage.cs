namespace ProjectAegis.Data.Scenario.Authoring;

using System.IO.Compression;
using System.Text.Json;

/// <summary>ZIP package for *.aegis-scenario (GDD §3.2: manifest + scenario.json).</summary>
public static class AegisScenarioPackage
{
    public const string ScenarioEntryName = "scenario.json";
    public const string ManifestEntryName = "manifest.json";

    public static void Write(string packagePath, ScenarioDocumentDto document, string? title = null)
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

        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        var manifest = new PackageManifestDto
        {
            PackageVersion = 1,
            Title = title ?? document.Metadata.Title ?? Path.GetFileNameWithoutExtension(packagePath),
            SchemaVersion = document.Metadata.SchemaVersion,
        };

        WriteJsonEntry(archive, ManifestEntryName, JsonSerializer.Serialize(manifest, JsonOptions));
        WriteJsonEntry(archive, ScenarioEntryName, ScenarioStableJsonWriter.Serialize(document));
    }

    public static ScenarioDocumentDto Read(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var entry = archive.GetEntry(ScenarioEntryName)
            ?? throw new InvalidDataException($"Missing {ScenarioEntryName} in {packagePath}");

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<ScenarioDocumentDto>(json, LoadOptions)
            ?? throw new InvalidDataException($"Invalid scenario document in {packagePath}");
    }

    public static PackageManifestDto ReadManifest(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var entry = archive.GetEntry(ManifestEntryName)
            ?? throw new InvalidDataException($"Missing {ManifestEntryName} in {packagePath}");

        using var stream = entry.Open();
        return JsonSerializer.Deserialize<PackageManifestDto>(stream, LoadOptions)
            ?? throw new InvalidDataException($"Invalid manifest in {packagePath}");
    }

    private static void WriteJsonEntry(ZipArchive archive, string name, string json)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(json.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions LoadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

public sealed class PackageManifestDto
{
    public int PackageVersion { get; init; } = 1;

    public string Title { get; init; } = "";

    public int SchemaVersion { get; init; } = 1;
}
