using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// S125-04 / AUTH-04 (AME-6.5 / AC-12): Save persists a draft (allowed while invalid) and never
/// produces an export artifact; Export is the validated artifact and writes nothing while the gate
/// reports blocking findings.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class ScenarioSaveExportGateTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"aegis-s125-save-export-{Guid.NewGuid():N}");

    public ScenarioSaveExportGateTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private string WriteInvalidDraft()
    {
        var path = Path.Combine(_dir, "draft.json");
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("strike-1", new[] { "u1" }, Array.Empty<string>());
        editor.Save(path);
        return path;
    }

    [Test]
    public void Export_artifact_path_is_distinct_from_draft_path()
    {
        var draft = Path.Combine(_dir, "baltic.json");

        var artifact = ScenarioSaveExportGate.ExportArtifactPathFor(draft);

        Assert.That(artifact, Is.Not.EqualTo(draft));
        Assert.That(artifact, Does.EndWith(ScenarioSaveExportGate.ExportArtifactSuffix));
    }

    [Test]
    public void SaveDraft_on_invalid_document_saves_and_writes_no_export_artifact()
    {
        var draftPath = WriteInvalidDraft();
        using var session = ScenarioAuthoringSession.Open(draftPath);
        var mutation = session.Bus.UpsertUnit(
            session.EditVersion,
            new ScenarioOrbatUnitDto { Id = "u1", SideId = "blue", PlatformId = "u1", Lat = 57, Lon = 20 },
            save: false);
        Assert.That(mutation.Ok, Is.True, mutation.ErrorMessage);
        Assert.That(session.IsDirty, Is.True);

        var outcome = ScenarioSaveExportGate.SaveDraft(session);

        Assert.That(outcome.Saved, Is.True);
        Assert.That(outcome.DraftPath, Is.EqualTo(draftPath));
        Assert.That(outcome.ExportArtifactWritten, Is.False);
        Assert.That(outcome.BlockingFindingCount > 0, Is.True);
        Assert.That(session.IsDirty, Is.False);
        Assert.That(outcome.StatusText, Does.Contain("draft").IgnoreCase);
        Assert.That(outcome.StatusText, Does.Contain("not exported").IgnoreCase);
        Assert.That(File.Exists(ScenarioSaveExportGate.ExportArtifactPathFor(draftPath)), Is.False);
    }

    [Test]
    public void Export_of_saved_invalid_draft_is_blocked_with_findings_and_writes_nothing()
    {
        var draftPath = WriteInvalidDraft();
        var artifactPath = ScenarioSaveExportGate.ExportArtifactPathFor(draftPath);
        var document = ScenarioDocumentJsonLoader.LoadFromFile(draftPath);

        var outcome = ScenarioSaveExportGate.Export(
            document,
            InMemoryCatalogReader.BalticPatrolFixture(),
            artifactPath,
            draftPath);

        Assert.That(outcome.Allowed, Is.False);
        Assert.That(outcome.ArtifactPath, Is.Null);
        Assert.That(outcome.BlockingFindings.Any(f => f.Code == "STRIKE_NO_TARGETS" && f.Severity == ValidationSeverity.Error), Is.True);
        Assert.That(outcome.Report.Findings.Any(f => f.Code == "STRIKE_NO_TARGETS"), Is.True);
        Assert.That(outcome.BlockingFindings.All(outcome.Report.Findings.Contains), Is.True, "blocking findings must come from the report");
        Assert.That(outcome.StatusText, Does.Contain("blocked").IgnoreCase);
        Assert.That(File.Exists(artifactPath), Is.False);
    }

    [Test]
    public void Export_allowed_writes_artifact_and_leaves_draft_untouched()
    {
        var draftPath = Path.Combine(_dir, "ok.json");
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.Save(draftPath);
        var draftBytes = File.ReadAllBytes(draftPath);
        var artifactPath = ScenarioSaveExportGate.ExportArtifactPathFor(draftPath);

        var outcome = ScenarioSaveExportGate.Export(
            ScenarioDocumentJsonLoader.LoadFromFile(draftPath),
            InMemoryCatalogReader.BalticPatrolFixture(),
            artifactPath,
            draftPath);

        Assert.That(outcome.Allowed, Is.True, string.Join("; ", outcome.BlockingFindings.Select(f => f.Code)));
        Assert.That(outcome.ArtifactPath, Is.EqualTo(artifactPath));
        Assert.That(File.Exists(artifactPath), Is.True);
        Assert.That(outcome.BlockingFindings, Is.Empty);
        Assert.That(outcome.Report.Findings.Any(f => f.Severity == ValidationSeverity.Error), Is.False);
        Assert.That(outcome.TransformCount, Is.EqualTo(0));
        Assert.That(outcome.StatusText, Does.Contain("0 logged transforms"));
        Assert.That(File.ReadAllBytes(draftPath), Is.EqualTo(draftBytes));
    }

    [Test]
    public void Export_refuses_to_overwrite_the_draft()
    {
        var draftPath = WriteInvalidDraft();

        Assert.Throws<ArgumentException>(() => ScenarioSaveExportGate.Export(
            ScenarioDocumentJsonLoader.LoadFromFile(draftPath),
            InMemoryCatalogReader.BalticPatrolFixture(),
            draftPath,
            draftPath));
    }

    [Test]
    public void IsSameFilePath_treats_case_variants_as_the_same_file_under_ordinal_ignore_case()
    {
        var draft = Path.Combine(_dir, "Draft.json");
        var variant = Path.Combine(_dir, "draft.JSON");
        var viaParent = Path.Combine(_dir, "nested", "..", "Draft.json");

        Assert.That(variant, Is.Not.EqualTo(draft).Using(StringComparer.Ordinal));
        Assert.That(ScenarioSaveExportGate.IsSameFilePath(draft, variant, StringComparison.OrdinalIgnoreCase), Is.True);
        Assert.That(ScenarioSaveExportGate.IsSameFilePath(draft, variant, StringComparison.Ordinal), Is.False);
        Assert.That(ScenarioSaveExportGate.IsSameFilePath(draft, viaParent), Is.True);
        Assert.That(ScenarioSaveExportGate.IsSameFilePath(draft, Path.GetRelativePath(Directory.GetCurrentDirectory(), draft)), Is.True);
    }

    [Test]
    public void Export_path_comparison_is_case_insensitive_on_windows_and_macos()
    {
        var expected = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        Assert.That(ScenarioSaveExportGate.ExportPathComparison, Is.EqualTo(expected));
    }

    [Test]
    public void Export_refuses_case_variant_of_the_draft_when_the_filesystem_ignores_case()
    {
        var draftPath = Path.Combine(_dir, "Draft.json");
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.Save(draftPath);
        var before = File.ReadAllBytes(draftPath);
        var variant = Path.Combine(_dir, "draft.json");
        Assert.That(ScenarioSaveExportGate.IsSameFilePath(draftPath, variant, StringComparison.Ordinal), Is.False);
        Assert.That(ScenarioSaveExportGate.IsSameFilePath(draftPath, variant, StringComparison.OrdinalIgnoreCase), Is.True);

        var document = ScenarioDocumentJsonLoader.LoadFromFile(draftPath);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        if (ScenarioSaveExportGate.ExportPathComparison == StringComparison.OrdinalIgnoreCase)
        {
            var ex = Assert.Throws<ArgumentException>(() => ScenarioSaveExportGate.Export(document, catalog, variant, draftPath));
            Assert.That(ex!.Message, Does.Contain("differ").IgnoreCase);
            Assert.That(File.ReadAllBytes(draftPath), Is.EqualTo(before));
            Assert.That(File.Exists(variant) && !ScenarioSaveExportGate.IsSameFilePath(draftPath, variant), Is.False);
            return;
        }

        var outcome = ScenarioSaveExportGate.Export(document, catalog, variant, draftPath);
        Assert.That(outcome.Allowed, Is.True, string.Join("; ", outcome.BlockingFindings.Select(f => f.Code)));
        Assert.That(outcome.ArtifactPath, Is.EqualTo(variant));
        Assert.That(File.Exists(variant), Is.True);
        Assert.That(File.ReadAllBytes(draftPath), Is.EqualTo(before));
    }
}
