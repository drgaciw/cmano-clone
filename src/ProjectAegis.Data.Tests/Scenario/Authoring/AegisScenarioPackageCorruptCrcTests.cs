using System.IO.Compression;
using System.Text;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario.Authoring;

/// <summary>
/// DRG-726: corrupt-CRC scenario package fixture, exercised on the sync and async open paths.
/// <para>
/// The fixture stores entries uncompressed and flips one payload byte so the JSON stays well-formed while the
/// ZIP CRC32 no longer matches. .NET 10 does not verify entry CRC32 on read (the corrupted title is accepted);
/// .NET 11 does and throws <see cref="InvalidDataException"/>. The contract pinned here is that both paths
/// agree with each other and that any failure is a clean <see cref="InvalidDataException"/> (never a crash or
/// a different exception type). On runtime 11+ the failure is mandatory.
/// </para>
/// </summary>
public sealed class AegisScenarioPackageCorruptCrcTests : IDisposable
{
    private const string OriginalTitle = "CrcFixtureTitle";
    private const string CorruptedTitle = "XrcFixtureTitle";
    private const string ManifestTitle = "CrcManifestTitle";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aegis-pkg-crc-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private static bool RuntimeVerifiesZipCrc => Environment.Version.Major >= 11;

    /// <summary>Builds a valid package, repacks it with stored (uncompressed) entries, optionally flips one scenario.json payload byte.</summary>
    private string BuildPackage(string fileName, bool corruptScenarioPayload)
    {
        Directory.CreateDirectory(_dir);
        var seedPath = Path.Combine(_dir, "seed-" + fileName);
        AegisScenarioPackage.Write(
            seedPath,
            new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { Title = OriginalTitle } },
            title: ManifestTitle);

        var path = Path.Combine(_dir, fileName);
        using (var source = ZipFile.OpenRead(seedPath))
        using (var target = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach (var entry in source.Entries)
            {
                using var input = entry.Open();
                using var buffer = new MemoryStream();
                input.CopyTo(buffer);
                var created = target.CreateEntry(entry.FullName, CompressionLevel.NoCompression);
                using var output = created.Open();
                output.Write(buffer.ToArray());
            }
        }

        if (corruptScenarioPayload)
        {
            var bytes = File.ReadAllBytes(path);
            var needle = Encoding.UTF8.GetBytes(OriginalTitle);
            var index = bytes.AsSpan().IndexOf(needle);
            Assert.True(index >= 0, "stored scenario.json payload must contain the title bytes");
            // Same-length swap keeps the JSON valid and the ZIP structure intact; only the CRC32 is now wrong.
            var replacement = Encoding.UTF8.GetBytes(CorruptedTitle);
            replacement.CopyTo(bytes, index);
            File.WriteAllBytes(path, bytes);
        }

        return path;
    }

    private static (string? Title, Type? Failure) CaptureSync(Func<ScenarioDocumentDto> read)
    {
        try
        {
            return (read().Metadata.Title, null);
        }
        catch (Exception ex)
        {
            return (null, ex.GetType());
        }
    }

    private static async Task<(string? Title, Type? Failure)> CaptureAsync(Func<Task<ScenarioDocumentDto>> read)
    {
        try
        {
            return ((await read()).Metadata.Title, null);
        }
        catch (Exception ex)
        {
            return (null, ex.GetType());
        }
    }

    [Fact]
    public async Task Control_uncorrupted_stored_package_reads_on_sync_and_async_paths()
    {
        var path = BuildPackage("control.aegis-scenario", corruptScenarioPayload: false);

        Assert.Equal(OriginalTitle, AegisScenarioPackage.Read(path).Metadata.Title);
        Assert.Equal(OriginalTitle, (await AegisScenarioPackage.ReadAsync(path)).Metadata.Title);
        Assert.Equal(ManifestTitle, AegisScenarioPackage.ReadManifest(path).Title);
    }

    [Fact]
    public void Fixture_has_stale_crc_with_still_valid_zip_structure()
    {
        var path = BuildPackage("fixture.aegis-scenario", corruptScenarioPayload: true);

        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry(AegisScenarioPackage.ScenarioEntryName);
        Assert.NotNull(entry);
        Assert.Equal(entry.Length, entry.CompressedLength);

        // Raw payload bytes (bypassing entry.Open) must carry the corrupted title while the header CRC is unchanged.
        var raw = File.ReadAllBytes(path);
        Assert.True(raw.AsSpan().IndexOf(Encoding.UTF8.GetBytes(CorruptedTitle)) >= 0);
        Assert.True(raw.AsSpan().IndexOf(Encoding.UTF8.GetBytes(OriginalTitle)) < 0);
        Assert.NotEqual(0u, entry.Crc32);
    }

    [Fact]
    public async Task Corrupt_crc_scenario_entry_is_reported_cleanly_and_identically_on_sync_and_async_read()
    {
        var path = BuildPackage("corrupt-scenario.aegis-scenario", corruptScenarioPayload: true);

        var sync = CaptureSync(() => AegisScenarioPackage.Read(path));
        var async = await CaptureAsync(() => AegisScenarioPackage.ReadAsync(path));

        Assert.Equal(sync, async);
        AssertCleanOutcome(sync);
    }

    [Fact]
    public async Task Corrupt_crc_in_scenario_entry_does_not_affect_manifest_read_on_either_path()
    {
        var path = BuildPackage("corrupt-manifest-isolated.aegis-scenario", corruptScenarioPayload: true);

        Assert.Equal(ManifestTitle, AegisScenarioPackage.ReadManifest(path).Title);
        Assert.Equal(ManifestTitle, (await AegisScenarioPackage.ReadManifestAsync(path)).Title);
    }

    private static void AssertCleanOutcome((string? Title, Type? Failure) outcome)
    {
        if (outcome.Failure is not null)
        {
            Assert.Equal(typeof(InvalidDataException), outcome.Failure);
            return;
        }

        Assert.False(
            RuntimeVerifiesZipCrc,
            "runtime 11+ verifies ZIP CRC32 and must reject the corrupted entry with InvalidDataException");
        Assert.Equal(CorruptedTitle, outcome.Title);
    }
}
