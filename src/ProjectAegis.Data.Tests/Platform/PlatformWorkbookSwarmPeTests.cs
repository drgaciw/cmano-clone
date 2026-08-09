using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>SWARM-21 PE chrome: Swarms sheet export + write-gate round-trip (DRG-110).</summary>
public sealed class PlatformWorkbookSwarmPeTests
{
    private const string SnapshotId = "baltic_patrol";

    private static readonly string[] ExpectedSwarmsHeader =
    [
        "PlatformId", "IsSwarm", "MaxDrones", "ArmorClass", "DefaultSensorId", "DefaultWeaponId",
        "DefaultMode", "RequiresHost", "AllowedHostClasses", "CecCapable",
        "ReviewState", "TrlLevel", "ValueTier", "CitationRef",
    ];

    private static PlatformCatalogExportData SampleWithSwarm() => new(
        Platforms: new[]
        {
            new CatalogPlatformEntry(CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 57.05, 20.05, 120.0),
            new CatalogPlatformEntry(CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId, 57.15, 20.15, 150.0),
        },
        Sensors: [],
        Mounts: [],
        Loadouts: [],
        Magazines: [],
        Comms: [],
        Swarms: new[]
        {
            CatalogValidationDefaults.GenericSwarmPlatform(),
            CatalogValidationDefaults.UsnCecSwarmPlatform(),
        });

    private static PlatformWorkbook Export(PlatformCatalogExportData data) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(utcTicks: 0));

    [Fact]
    public void Export_includes_Swarms_sheet_with_headers_and_cec_rows()
    {
        var workbook = Export(SampleWithSwarm());
        var swarms = workbook.FindSheet("Swarms");
        Assert.NotNull(swarms);
        Assert.Equal(ExpectedSwarmsHeader, swarms!.Header);
        Assert.Equal(2, swarms.Rows.Count);

        var usn = swarms.Rows.Single(r => r[0] == CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId);
        Assert.Equal("1", usn[ExpectedSwarmsHeader.ToList().IndexOf("CecCapable")]);
        Assert.Equal(
            CatalogValidationDefaults.UsnCecSwarmPlatform().DefaultMode,
            usn[ExpectedSwarmsHeader.ToList().IndexOf("DefaultMode")]);

        var generic = swarms.Rows.Single(r => r[0] == CatalogSwarmPlatformDefaults.GenericSwarmPlatformId);
        Assert.Equal("0", generic[ExpectedSwarmsHeader.ToList().IndexOf("CecCapable")]);
    }

    [Fact]
    public void Validator_accepts_clean_Swarms_export()
    {
        var findings = PlatformWorkbookValidator.Validate(Export(SampleWithSwarm()));
        Assert.DoesNotContain(findings, f => f.Code.StartsWith("PLE-SWARM", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_flags_invalid_mode_and_orphan()
    {
        var source = Export(SampleWithSwarm());
        var swarms = source.FindSheet("Swarms")!;
        var badRows = swarms.Rows.Select(r => r.ToArray()).ToList();
        // corrupt mode on first row
        var modeIdx = ExpectedSwarmsHeader.ToList().IndexOf("DefaultMode");
        badRows[0][modeIdx] = "NotAMode";
        // orphan platform
        var orphan = badRows[0].ToArray();
        orphan[0] = "missing-platform";
        badRows.Add(orphan);

        var edited = new PlatformWorkbook(
            source.Sheets.Select(s =>
                string.Equals(s.Name, "Swarms", StringComparison.Ordinal)
                    ? new PlatformWorkbookSheet("Swarms", swarms.Header, badRows.Select(r => (IReadOnlyList<string>)r).ToArray())
                    : s).ToArray());

        var findings = PlatformWorkbookValidator.Validate(edited);
        Assert.Contains(findings, f => f.Code == PlatformWorkbookValidator.SwarmInvalidMode);
        Assert.Contains(findings, f => f.Code == PlatformWorkbookValidator.SwarmOrphanPlatform);
    }

    [Fact]
    public void Importer_stages_and_approves_swarm_cec_edit()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"swarm-pe-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            var clock = new FixedCatalogClock(utcTicks: 42_000);
            var service = new PlatformWorkbookWriteService();
            var exported = service.ExportFromDatabase(dbPath, SnapshotId, clock);

            var swarms = exported.FindSheet("Swarms");
            Assert.NotNull(swarms);
            Assert.NotEmpty(swarms!.Rows);

            var header = swarms.Header.ToList();
            var maxIdx = header.IndexOf("MaxDrones");
            var cecIdx = header.IndexOf("CecCapable");
            var rows = swarms.Rows.Select(r => r.ToArray()).ToList();
            var genericIdx = rows.FindIndex(r => r[0] == CatalogSwarmPlatformDefaults.GenericSwarmPlatformId);
            Assert.True(genericIdx >= 0);
            rows[genericIdx][maxIdx] = "48";
            // keep generic non-CEC
            rows[genericIdx][cecIdx] = "0";

            var edited = new PlatformWorkbook(
                exported.Sheets.Select(s =>
                    string.Equals(s.Name, "Swarms", StringComparison.Ordinal)
                        ? new PlatformWorkbookSheet("Swarms", swarms.Header, rows.Select(r => (IReadOnlyList<string>)r).ToArray())
                        : s).ToArray());

            var importer = new PlatformWorkbookImporter(
                snapshotId =>
                {
                    PlatformCatalogExportResolver.TryResolve(dbPath, snapshotId, out var data);
                    return data;
                },
                clock);

            using var gate = new CatalogWriteGate(dbPath, clock);
            var result = importer.Stage(edited, gate, "human", "swarm-pe-qa");
            Assert.True(result.Staged);
            Assert.NotNull(result.SwarmBatchId);
            Assert.True(gate.ApproveBatch(result.SwarmBatchId!, "human", "swarm-pe-qa").Committed);

            using var reader = new SqliteCatalogReader(dbPath, "swarm-pe-verify");
            Assert.True(reader.TryGetSwarmPlatform(CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, out var swarm));
            Assert.Equal(48, swarm.MaxDrones);
            Assert.False(swarm.CecCapable);
        }
        finally
        {
            try { File.Delete(dbPath); } catch { /* best effort */ }
        }
    }
}
