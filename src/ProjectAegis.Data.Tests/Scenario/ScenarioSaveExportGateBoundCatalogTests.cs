using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// DRG-372: SaveDraft / LiveValidate must use the draft's bound catalog — never the silent
/// Baltic fixture fallback. Unbound yields a stable actionable finding; Export stays gated;
/// incomplete draft save remains allowed.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class ScenarioSaveExportGateBoundCatalogTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"aegis-drg372-bound-{Guid.NewGuid():N}");

    public ScenarioSaveExportGateBoundCatalogTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Test]
    public void LiveValidate_with_null_bound_catalog_yields_stable_catalog_unbound_finding()
    {
        var document = ScenarioDocumentEditor.CreateNew(dbRef: "custom-theater-v1").ToDto();

        var report = ScenarioSaveExportGate.LiveValidate(document, boundCatalog: null);

        Assert.That(            report.Findings.Any(            f => f.Code == ScenarioSaveExportGate.CatalogUnboundFindingCode
                 && f.Severity == ValidationSeverity.Error), Is.True);
        Assert.That(report.Findings.Any(f => f.Code == "DB_MISMATCH"), Is.False);
        Assert.That(report.Findings.Any(f => f.Code == "STRIKE_NO_TARGETS"), Is.False);
        Assert.That(            report.Findings.Any(            f => f.Code == ScenarioSaveExportGate.CatalogUnboundFindingCode
                 && f.Message.Contains("bound", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void SaveDraft_without_catalog_saves_with_catalog_unbound_and_never_uses_silent_Baltic()
    {
        var draftPath = Path.Combine(_dir, "unbound.json");
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "custom-theater-v1");
        editor.AddStrikeMission("strike-1", new[] { "u1" }, Array.Empty<string>());
        editor.Save(draftPath);

        using var session = ScenarioAuthoringSession.Open(draftPath);
        var outcome = ScenarioSaveExportGate.SaveDraft(session);

        Assert.That(outcome.Saved, Is.True);
        Assert.That(outcome.ExportArtifactWritten, Is.False);
        Assert.That(outcome.BlockingFindingCount >= 1, Is.True);
        Assert.That(outcome.StatusText, Does.Contain("not exported").IgnoreCase);
        Assert.That(File.Exists(ScenarioSaveExportGate.ExportArtifactPathFor(draftPath)), Is.False);

        // Re-run the gate LiveValidate path the save uses: unbound, not Baltic fixture findings.
        var unbound = ScenarioSaveExportGate.LiveValidate(session.Editor.ToDto(), boundCatalog: null);
        Assert.That(unbound.Findings.Any(f => f.Code == ScenarioSaveExportGate.CatalogUnboundFindingCode), Is.True);

        var againstBaltic = new ScenarioValidationEngine().Validate(
            session.Editor.ToDto(),
            InMemoryCatalogReader.BalticPatrolFixture(),
            new ValidationConfig());
        Assert.That(againstBaltic.Findings.Any(f => f.Code == "STRIKE_NO_TARGETS"), Is.True);
        Assert.That(            unbound.Findings.Any(            f => f.Code == "STRIKE_NO_TARGETS"), Is.False);
    }

    [Test]
    public void SaveDraft_with_non_Baltic_bound_catalog_validates_against_that_catalog()
    {
        var draftPath = Path.Combine(_dir, "custom-bound.json");
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "custom-theater-v1");
        editor.Save(draftPath);
        var custom = new CustomTheaterCatalogReader();

        using var session = ScenarioAuthoringSession.Open(draftPath);
        var outcome = ScenarioSaveExportGate.SaveDraft(session, custom);

        Assert.That(outcome.Saved, Is.True);
        Assert.That(outcome.ExportArtifactWritten, Is.False);

        var boundReport = ScenarioSaveExportGate.LiveValidate(session.Editor.ToDto(), custom);
        Assert.That(boundReport.Findings.Any(f => f.Code == "DB_MISMATCH"), Is.False);
        Assert.That(            boundReport.Findings.Any(            f => f.Code == ScenarioSaveExportGate.CatalogUnboundFindingCode), Is.False);

        var balticReport = ScenarioSaveExportGate.LiveValidate(
            session.Editor.ToDto(),
            InMemoryCatalogReader.BalticPatrolFixture());
        Assert.That(balticReport.Findings.Any(f => f.Code == "DB_MISMATCH"), Is.True);
        Assert.That(            outcome.BlockingFindingCount == boundReport.Findings.Count(f => f.Severity >= ValidationSeverity.Error), Is.True,             "SaveDraft blocking count must come from the bound (non-Baltic) catalog, not Baltic.");
    }

    [Test]
    public void Baltic_fixture_only_used_when_explicitly_passed_as_bound_catalog()
    {
        var draftPath = Path.Combine(_dir, "baltic-explicit.json");
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("strike-1", new[] { "u1" }, Array.Empty<string>());
        editor.Save(draftPath);

        using var session = ScenarioAuthoringSession.Open(draftPath);
        var baltic = InMemoryCatalogReader.BalticPatrolFixture();
        var outcome = ScenarioSaveExportGate.SaveDraft(session, baltic);

        Assert.That(outcome.Saved, Is.True);
        Assert.That(outcome.ExportArtifactWritten, Is.False);
        Assert.That(outcome.BlockingFindingCount >= 1, Is.True);

        var report = ScenarioSaveExportGate.LiveValidate(session.Editor.ToDto(), baltic);
        Assert.That(report.Findings.Any(f => f.Code == "STRIKE_NO_TARGETS"), Is.True);
        Assert.That(            report.Findings.Any(            f => f.Code == ScenarioSaveExportGate.CatalogUnboundFindingCode), Is.False);
    }

    [Test]
    public void Export_still_requires_catalog_and_stays_gated_when_draft_is_incomplete()
    {
        var draftPath = Path.Combine(_dir, "export-gate.json");
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "custom-theater-v1");
        editor.AddStrikeMission("strike-1", new[] { "u1" }, Array.Empty<string>());
        editor.Save(draftPath);
        var artifact = ScenarioSaveExportGate.ExportArtifactPathFor(draftPath);
        var custom = new CustomTheaterCatalogReader();

        using var session = ScenarioAuthoringSession.Open(draftPath);
        var saved = ScenarioSaveExportGate.SaveDraft(session, custom);
        Assert.That(saved.Saved, Is.True);

        var exported = ScenarioSaveExportGate.Export(
            session.Editor.ToDto(),
            custom,
            artifact,
            draftPath);

        Assert.That(exported.Allowed, Is.False);
        Assert.That(exported.ArtifactPath, Is.Null);
        Assert.That(File.Exists(artifact), Is.False);
        Assert.That(exported.BlockingFindings.Any(f => f.Code == "STRIKE_NO_TARGETS"), Is.True);
    }

    /// <summary>Minimal non-Baltic catalog that resolves only <c>custom-theater-v1</c>.</summary>
    private sealed class CustomTheaterCatalogReader : ICatalogReader
    {
        public string LayerVersion => "test-custom-theater-v1";

        public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() =>
            Array.Empty<CatalogSensorBinding>();

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
        {
            basePd = 0;
            return false;
        }

        public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId)
        {
            if (string.Equals(dbRef, "custom-theater-v1", StringComparison.OrdinalIgnoreCase))
            {
                resolvedSnapshotId = "custom-theater-snapshot";
                return true;
            }

            resolvedSnapshotId = "";
            return false;
        }
    }
}
