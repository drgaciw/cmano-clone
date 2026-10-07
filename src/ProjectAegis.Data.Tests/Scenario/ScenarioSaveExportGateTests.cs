using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// S125-04 / AUTH-04 (AME-6.5 / AC-12): Save persists a draft (allowed while invalid) and never
/// produces an export artifact; Export is the validated artifact and writes nothing while the gate
/// reports blocking findings.
/// </summary>
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

    [Fact]
    public void Export_artifact_path_is_distinct_from_draft_path()
    {
        var draft = Path.Combine(_dir, "baltic.json");

        var artifact = ScenarioSaveExportGate.ExportArtifactPathFor(draft);

        Assert.NotEqual(draft, artifact);
        Assert.EndsWith(ScenarioSaveExportGate.ExportArtifactSuffix, artifact, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveDraft_on_invalid_document_saves_and_writes_no_export_artifact()
    {
        var draftPath = WriteInvalidDraft();
        using var session = ScenarioAuthoringSession.Open(draftPath);
        var mutation = session.Bus.UpsertUnit(
            session.EditVersion,
            new ScenarioOrbatUnitDto { Id = "u1", SideId = "blue", PlatformId = "u1", Lat = 57, Lon = 20 },
            save: false);
        Assert.True(mutation.Ok, mutation.ErrorMessage);
        Assert.True(session.IsDirty);

        var outcome = ScenarioSaveExportGate.SaveDraft(session);

        Assert.True(outcome.Saved);
        Assert.Equal(draftPath, outcome.DraftPath);
        Assert.False(outcome.ExportArtifactWritten);
        Assert.True(outcome.BlockingFindingCount > 0);
        Assert.False(session.IsDirty);
        Assert.Contains("draft", outcome.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not exported", outcome.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(ScenarioSaveExportGate.ExportArtifactPathFor(draftPath)));
    }

    [Fact]
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

        Assert.False(outcome.Allowed);
        Assert.Null(outcome.ArtifactPath);
        Assert.Contains(outcome.BlockingFindings, f => f.Code == "STRIKE_NO_TARGETS" && f.Severity == ValidationSeverity.Error);
        Assert.Contains(outcome.Report.Findings, f => f.Code == "STRIKE_NO_TARGETS");
        Assert.True(outcome.BlockingFindings.All(outcome.Report.Findings.Contains), "blocking findings must come from the report");
        Assert.Contains("blocked", outcome.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(artifactPath));
    }

    [Fact]
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

        Assert.True(outcome.Allowed, string.Join("; ", outcome.BlockingFindings.Select(f => f.Code)));
        Assert.Equal(artifactPath, outcome.ArtifactPath);
        Assert.True(File.Exists(artifactPath));
        Assert.Empty(outcome.BlockingFindings);
        Assert.DoesNotContain(outcome.Report.Findings, f => f.Severity == ValidationSeverity.Error);
        Assert.Equal(0, outcome.TransformCount);
        Assert.Contains("0 logged transforms", outcome.StatusText, StringComparison.Ordinal);
        Assert.Equal(draftBytes, File.ReadAllBytes(draftPath));
    }

    [Fact]
    public void Export_refuses_to_overwrite_the_draft()
    {
        var draftPath = WriteInvalidDraft();

        Assert.Throws<ArgumentException>(() => ScenarioSaveExportGate.Export(
            ScenarioDocumentJsonLoader.LoadFromFile(draftPath),
            InMemoryCatalogReader.BalticPatrolFixture(),
            draftPath,
            draftPath));
    }

    [Fact]
    public void IsSameFilePath_treats_case_variants_as_the_same_file_under_ordinal_ignore_case()
    {
        var draft = Path.Combine(_dir, "Draft.json");
        var variant = Path.Combine(_dir, "draft.JSON");
        var viaParent = Path.Combine(_dir, "nested", "..", "Draft.json");

        Assert.NotEqual(draft, variant, StringComparer.Ordinal);
        Assert.True(ScenarioSaveExportGate.IsSameFilePath(draft, variant, StringComparison.OrdinalIgnoreCase));
        Assert.False(ScenarioSaveExportGate.IsSameFilePath(draft, variant, StringComparison.Ordinal));
        Assert.True(ScenarioSaveExportGate.IsSameFilePath(draft, viaParent));
        Assert.True(ScenarioSaveExportGate.IsSameFilePath(draft, Path.GetRelativePath(Directory.GetCurrentDirectory(), draft)));
    }

    [Fact]
    public void Export_path_comparison_is_case_insensitive_on_windows_and_macos()
    {
        var expected = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        Assert.Equal(expected, ScenarioSaveExportGate.ExportPathComparison);
    }

    [Fact]
    public void Export_refuses_case_variant_of_the_draft_when_the_filesystem_ignores_case()
    {
        var draftPath = Path.Combine(_dir, "Draft.json");
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.Save(draftPath);
        var before = File.ReadAllBytes(draftPath);
        var variant = Path.Combine(_dir, "draft.json");
        Assert.False(ScenarioSaveExportGate.IsSameFilePath(draftPath, variant, StringComparison.Ordinal));
        Assert.True(ScenarioSaveExportGate.IsSameFilePath(draftPath, variant, StringComparison.OrdinalIgnoreCase));

        var document = ScenarioDocumentJsonLoader.LoadFromFile(draftPath);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        if (ScenarioSaveExportGate.ExportPathComparison == StringComparison.OrdinalIgnoreCase)
        {
            var ex = Assert.Throws<ArgumentException>(() => ScenarioSaveExportGate.Export(document, catalog, variant, draftPath));
            Assert.Contains("differ", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(before, File.ReadAllBytes(draftPath));
            Assert.False(File.Exists(variant) && !ScenarioSaveExportGate.IsSameFilePath(draftPath, variant));
            return;
        }

        var outcome = ScenarioSaveExportGate.Export(document, catalog, variant, draftPath);
        Assert.True(outcome.Allowed, string.Join("; ", outcome.BlockingFindings.Select(f => f.Code)));
        Assert.Equal(variant, outcome.ArtifactPath);
        Assert.True(File.Exists(variant));
        Assert.Equal(before, File.ReadAllBytes(draftPath));
    }
}
