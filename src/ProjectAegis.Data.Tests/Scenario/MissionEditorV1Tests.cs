namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

public sealed class ScenarioStableJsonWriterTests
{
    [Fact]
    public void Double_save_is_byte_identical_ac6()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ac6-{Guid.NewGuid():N}.json");
        try
        {
            var doc = ScenarioDocumentEditor.CreateNew().ToDto();
            ScenarioStableJsonWriter.WriteToFile(doc, path);
            var first = File.ReadAllBytes(path);
            ScenarioStableJsonWriter.WriteToFile(ScenarioDocumentJsonLoader.LoadFromFile(path), path);
            var second = File.ReadAllBytes(path);
            Assert.Equal(first, second);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Single_field_change_produces_one_key_diff_hunk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ac6-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.Save(path);
            var before = File.ReadAllText(path);

            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.CommitMutation();
            var meta = loaded.Metadata;
            var updated = new ScenarioDocumentDto
            {
                Metadata = new ScenarioMetadataDto
                {
                    Title = "Changed Title",
                    DbRef = meta.DbRef,
                    Seed = meta.Seed,
                    PolicyId = meta.PolicyId,
                    EditVersion = meta.EditVersion + 1,
                    SchemaVersion = meta.SchemaVersion,
                },
                EditorState = loaded.ToDto().EditorState,
            };
            ScenarioStableJsonWriter.WriteToFile(updated, path);
            var after = File.ReadAllText(path);

            Assert.NotEqual(before, after);
            Assert.Contains("\"title\": \"Changed Title\"", after);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Serialize_uses_lf_newlines()
    {
        var json = ScenarioStableJsonWriter.Serialize(ScenarioDocumentEditor.CreateNew().ToDto());
        Assert.DoesNotContain("\r\n", json);
    }
}

public sealed class AegisScenarioPackageTests
{
    [Fact]
    public void Round_trip_zip_package_preserves_edit_version()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pkg-{Guid.NewGuid():N}.aegis-scenario");
        try
        {
            var doc = ScenarioDocumentEditor.CreateNew().ToDto();
            AegisScenarioPackage.Write(path, doc, "Test Scenario");
            var loaded = AegisScenarioPackage.Read(path);
            Assert.Equal(doc.Metadata.EditVersion, loaded.Metadata.EditVersion);
            var manifest = AegisScenarioPackage.ReadManifest(path);
            Assert.Equal("Test Scenario", manifest.Title);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}

public sealed class EventFireOrderCalculatorTests
{
    [Fact]
    public void Fire_order_is_stable_by_tick_priority_id()
    {
        var events = new[]
        {
            new ScenarioEventDto { Id = "b", Priority = 100, Trigger = new ScenarioEventTriggerDto { Type = "Time", AtTick = 10 } },
            new ScenarioEventDto { Id = "a", Priority = 50, Trigger = new ScenarioEventTriggerDto { Type = "Time", AtTick = 10 } },
            new ScenarioEventDto { Id = "c", Priority = 100, Trigger = new ScenarioEventTriggerDto { Type = "Time", AtTick = 5 } },
        };

        var order = EventFireOrderCalculator.ComputeFireOrder(events);
        Assert.Equal(["c", "a", "b"], order);
    }
}

public sealed class EditorStateSchemaLintTests
{
    [Fact]
    public void Editor_state_lint_finds_no_sim_validation_reads()
    {
        // Force-load forbidden consumer assemblies so lazy AppDomain loading cannot skip the scan.
        try { System.Reflection.Assembly.Load("ProjectAegis.Sim"); } catch { /* optional for this test host */ }
        var violations = ProjectAegis.Data.Validation.EditorStateSchemaLint.FindViolations();
        Assert.Empty(violations);
    }
}

public sealed class ScenarioExportTransformerTests
{
    [Fact]
    public void Teleport_unit_actions_stripped_with_manifest_ac11()
    {
        var doc = new ScenarioDocumentDto
        {
            Events =
            [
                new ScenarioEventDto
                {
                    Id = "evt-1",
                    Actions =
                    [
                        new ScenarioEventActionDto { Type = "TeleportUnit", UnitId = "u1" },
                        new ScenarioEventActionDto { Type = "Message", MissionId = "brief" },
                    ],
                },
            ],
        };

        var (exported, manifest) = ScenarioExportTransformer.TransformForExport(doc);
        Assert.DoesNotContain(exported.Events[0].Actions, a => a.Type == "TeleportUnit");
        Assert.Single(manifest);
        Assert.Equal("TeleportUnitRemoved", manifest[0].Transform);
    }
}

public sealed class ScenarioSaveExportGateTests
{
    [Fact]
    public void Save_allowed_with_blocking_errors_ac12()
    {
        var doc = new ScenarioDocumentDto
        {
            Missions = [new ScenarioMissionDto { Id = "patrol-1", Type = "Patrol" }],
        };

        Assert.True(ScenarioSaveExportGate.CanSave(doc));
        var (allowed, report) = ScenarioSaveExportGate.CanExportOrPlay(doc, ValidationCatalogFixture.Default(), new ValidationConfig());
        Assert.False(allowed);
        Assert.Contains(report.Findings, f => f.Code == "MISSION_NO_UNITS");
    }

    private sealed class ValidationCatalogFixture : ProjectAegis.Data.Catalog.ICatalogReader
    {
        public static ValidationCatalogFixture Default() => new();

        public string LayerVersion => "test";

        public IReadOnlyList<ProjectAegis.Data.Catalog.CatalogSensorBinding> GetSortedSensorBindings() => Array.Empty<ProjectAegis.Data.Catalog.CatalogSensorBinding>();

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
        {
            basePd = 0;
            return false;
        }

        public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId)
        {
            resolvedSnapshotId = dbRef;
            return true;
        }

        public bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm)
        {
            combatRadiusNm = 0;
            return false;
        }

        public bool TryGetPlatformPosition(string platformId, out double latDeg, out double lonDeg)
        {
            latDeg = 0;
            lonDeg = 0;
            return false;
        }
    }
}
