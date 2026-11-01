using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario.Authoring;

/// <summary>DRG-695: async ZIP read/write parity with the sync AegisScenarioPackage API.</summary>
public sealed class AegisScenarioPackageAsyncTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aegis-pkg-async-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private static ScenarioDocumentDto Doc() => new() { Metadata = new ScenarioMetadataDto { Title = "Async Test" } };

    [Fact]
    public async Task WriteAsync_then_ReadAsync_round_trips()
    {
        var path = Path.Combine(_dir, "a.aegis-scenario");
        await AegisScenarioPackage.WriteAsync(path, Doc());
        var doc = await AegisScenarioPackage.ReadAsync(path);
        var manifest = await AegisScenarioPackage.ReadManifestAsync(path);
        Assert.Equal("Async Test", doc.Metadata.Title);
        Assert.Equal("Async Test", manifest.Title);
        Assert.Equal(1, manifest.PackageVersion);
    }

    [Fact]
    public async Task Async_and_sync_paths_are_interchangeable()
    {
        var asyncPath = Path.Combine(_dir, "async.aegis-scenario");
        var syncPath = Path.Combine(_dir, "sync.aegis-scenario");
        await AegisScenarioPackage.WriteAsync(asyncPath, Doc(), title: "T");
        AegisScenarioPackage.Write(syncPath, Doc(), title: "T");
        Assert.Equal("Async Test", AegisScenarioPackage.Read(asyncPath).Metadata.Title);
        Assert.Equal("T", (await AegisScenarioPackage.ReadManifestAsync(syncPath)).Title);
    }

    [Fact]
    public async Task WriteAsync_overwrites_existing_package()
    {
        var path = Path.Combine(_dir, "o.aegis-scenario");
        await AegisScenarioPackage.WriteAsync(path, Doc(), title: "first");
        await AegisScenarioPackage.WriteAsync(path, Doc(), title: "second");
        Assert.Equal("second", (await AegisScenarioPackage.ReadManifestAsync(path)).Title);
    }

    [Fact]
    public async Task ReadAsync_missing_entry_throws_InvalidData()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "empty.aegis-scenario");
        using (System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create))
        {
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => AegisScenarioPackage.ReadAsync(path));
    }

    [Fact]
    public async Task ReadAsync_honors_cancellation()
    {
        var path = Path.Combine(_dir, "c.aegis-scenario");
        await AegisScenarioPackage.WriteAsync(path, Doc());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AegisScenarioPackage.ReadAsync(path, cts.Token));
    }

    [Fact]
    public async Task WriteAsync_cancelled_before_start_preserves_existing_package()
    {
        var path = Path.Combine(_dir, "cancelled.aegis-scenario");
        await AegisScenarioPackage.WriteAsync(path, Doc(), title: "original");
        var original = await File.ReadAllBytesAsync(path);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AegisScenarioPackage.WriteAsync(path, Doc(), cancellationToken: new CancellationToken(canceled: true)));

        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        Assert.Single(Directory.GetFiles(_dir));
    }

    [Fact]
    public async Task WriteAsync_serialization_failure_preserves_existing_package_and_removes_temporary_file()
    {
        var path = Path.Combine(_dir, "invalid.aegis-scenario");
        await AegisScenarioPackage.WriteAsync(path, Doc(), title: "original");
        var original = await File.ReadAllBytesAsync(path);
        var invalidDocument = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { Title = "replacement" },
            ReferencePoints = new List<ScenarioReferencePointDto>
            {
                new() { Id = "invalid", RadiusNm = double.NaN },
            },
        };

        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() => AegisScenarioPackage.WriteAsync(path, invalidDocument));

        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        Assert.Single(Directory.GetFiles(_dir));
    }
}
