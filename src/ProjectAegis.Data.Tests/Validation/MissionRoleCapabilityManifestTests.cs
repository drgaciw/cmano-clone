namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

/// <summary>
/// DRG-345 / proposed AME-6.11: the mission role capability manifest discloses whether the
/// selected execution backend runs each authored mission role, and unsupported roles produce
/// stable Error findings that block Export/Play while Save stays available.
/// </summary>
public sealed class MissionRoleCapabilityManifestTests
{
    private static readonly ValidationConfig Config = new();

    public static TheoryData<string, string?, MissionRoleExecution, string?> RoleTable => new()
    {
        { "Patrol", null, MissionRoleExecution.Executed, null },
        { "Strike", null, MissionRoleExecution.Executed, null },
        { "Ferry", null, MissionRoleExecution.NotExecuted, MissionRoleCapabilityManifest.CodeRoleNotExecuted },
        { "Support", "Tanker", MissionRoleExecution.NotExecuted, MissionRoleCapabilityManifest.CodeRoleNotExecuted },
        { "Support", "AEW", MissionRoleExecution.NotExecuted, MissionRoleCapabilityManifest.CodeRoleNotExecuted },
        { "Support", "EW", MissionRoleExecution.NotExecuted, MissionRoleCapabilityManifest.CodeRoleNotExecuted },
        { "Support", null, MissionRoleExecution.Unknown, MissionRoleCapabilityManifest.CodeRoleUnknown },
        { "Support", "Jammer", MissionRoleExecution.Unknown, MissionRoleCapabilityManifest.CodeRoleUnknown },
        { "Escort", null, MissionRoleExecution.Unknown, MissionRoleCapabilityManifest.CodeRoleUnknown },
        { "", null, MissionRoleExecution.Unknown, MissionRoleCapabilityManifest.CodeRoleUnknown },
    };

    [Theory]
    [MemberData(nameof(RoleTable))]
    public void Assess_classifies_authored_role_against_default_backend(
        string type,
        string? supportRole,
        MissionRoleExecution expected,
        string? expectedCode)
    {
        var mission = new ScenarioMissionDto { Id = "m1", Type = type, SupportRole = supportRole, AssignedUnitIds = ["u1"] };

        var assessment = MissionRoleCapabilityManifest.Assess(mission);

        Assert.Equal(expected, assessment.Execution);
        Assert.Equal(expectedCode, assessment.FindingCode);
        Assert.Equal(MissionRoleCapabilityManifest.DefaultBackendId, assessment.BackendId);
        Assert.Equal("m1", assessment.MissionId);
        Assert.False(string.IsNullOrWhiteSpace(assessment.Message));
    }

    [Theory]
    [InlineData("support", "tanker")]
    [InlineData("SUPPORT", "Tanker")]
    [InlineData(" Support ", " AEW ")]
    public void Assess_role_matching_is_case_and_whitespace_insensitive(string type, string role)
    {
        var assessment = MissionRoleCapabilityManifest.Assess(
            new ScenarioMissionDto { Id = "m1", Type = type, SupportRole = role });

        Assert.Equal(MissionRoleExecution.NotExecuted, assessment.Execution);
    }

    [Fact]
    public void Unknown_backend_marks_every_mission_unknown_backend()
    {
        var mission = new ScenarioMissionDto { Id = "p1", Type = "Patrol" };

        var assessment = MissionRoleCapabilityManifest.Assess(mission, "no-such-backend");

        Assert.Equal(MissionRoleExecution.Unknown, assessment.Execution);
        Assert.Equal(MissionRoleCapabilityManifest.CodeBackendUnknown, assessment.FindingCode);
        Assert.Equal("no-such-backend", assessment.BackendId);
    }

    [Fact]
    public void Findings_are_error_severity_with_stable_codes_and_actionable_messages()
    {
        var doc = LoadFixture("role-capability-unsupported.json");

        var findings = MissionRoleCapabilityManifest.EvaluateFindings(doc);

        Assert.Equal(
            new[]
            {
                ("escort-1", MissionRoleCapabilityManifest.CodeRoleUnknown),
                ("ferry-1", MissionRoleCapabilityManifest.CodeRoleNotExecuted),
                ("support-aew", MissionRoleCapabilityManifest.CodeRoleNotExecuted),
                ("support-ew", MissionRoleCapabilityManifest.CodeRoleNotExecuted),
                ("support-tanker", MissionRoleCapabilityManifest.CodeRoleNotExecuted),
            },
            findings.Select(f => (f.MissionId!, f.Code)).ToArray());
        Assert.All(findings, f => Assert.Equal(ValidationSeverity.Error, f.Severity));
        Assert.All(findings, f => Assert.Contains("Save remains available", f.Message, StringComparison.Ordinal));
        Assert.All(findings, f => Assert.Equal(MissionRoleCapabilityManifest.DefaultBackendId, f.Data!["backend"]));

        var tanker = Assert.Single(findings, f => f.MissionId == "support-tanker");
        Assert.Equal(
            "Support mission 'support-tanker' role 'Tanker' is not executed by backend 'baltic-replay-harness'. "
            + "Export and Play are blocked; Save remains available. Remove the mission or change it to a supported role (Patrol, Strike).",
            tanker.Message);
        Assert.Equal("Support", tanker.Data!["missionType"]);
        Assert.Equal("Tanker", tanker.Data!["supportRole"]);
        Assert.Equal("NotExecuted", tanker.Data!["execution"]);
    }

    [Fact]
    public void Findings_are_deterministic_regardless_of_authored_mission_order()
    {
        var doc = LoadFixture("role-capability-unsupported.json");
        var reversed = new ScenarioDocumentDto
        {
            Metadata = doc.Metadata,
            Missions = doc.Missions.Reverse().ToArray(),
        };

        var a = MissionRoleCapabilityManifest.EvaluateFindings(doc);
        var b = MissionRoleCapabilityManifest.EvaluateFindings(reversed);

        static string[] Key(IEnumerable<ValidationFinding> fs) => fs
            .Select(f => $"{f.Severity}|{f.Code}|{f.MissionId}|{f.Message}|{string.Join(",", f.Data!.Select(kv => kv.Key + "=" + kv.Value))}")
            .ToArray();
        Assert.Equal(Key(a), Key(b));
        Assert.Equal(

            MissionRoleCapabilityManifest.AssessDocument(doc).Select(x => x.MissionId),
            MissionRoleCapabilityManifest.AssessDocument(reversed).Select(x => x.MissionId));
    }

    [Fact]
    public void AssessDocument_discloses_supported_and_unsupported_missions_in_mission_id_order()
    {
        var doc = LoadFixture("role-capability-unsupported.json");

        var rows = MissionRoleCapabilityManifest.AssessDocument(doc);

        Assert.Equal(
            new[] { "escort-1", "ferry-1", "patrol-1", "support-aew", "support-ew", "support-tanker" },
            rows.Select(r => r.MissionId).ToArray());
        var patrol = Assert.Single(rows, r => r.MissionId == "patrol-1");
        Assert.Equal(MissionRoleExecution.Executed, patrol.Execution);
        Assert.Null(patrol.FindingCode);
    }

    [Fact]
    public void Supported_fixture_has_no_capability_findings_and_exports()
    {
        var doc = LoadFixture("role-capability-supported.json");

        Assert.Empty(MissionRoleCapabilityManifest.EvaluateFindings(doc));
        var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(
            doc, InMemoryCatalogReader.BalticPatrolFixture(), Config);
        Assert.True(allowed);
        Assert.DoesNotContain(report.Findings, f => MissionRoleCapabilityManifest.IsCapabilityCode(f.Code));
    }

    [Fact]
    public void Validation_engine_export_gate_and_prepare_all_block_unsupported_roles()
    {
        var doc = LoadFixture("role-capability-unsupported.json");
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var expected = MissionRoleCapabilityManifest.EvaluateFindings(doc);

        var report = new ScenarioValidationEngine().Validate(doc, catalog, Config);
        var (allowed, gateReport) = ScenarioValidationExportGate.EvaluateExport(doc, catalog, Config);
        var package = ScenarioExportCommand.Prepare(doc, catalog, Config);

        Assert.False(allowed);
        Assert.False(package.Allowed);
        foreach (var r in new[] { report, gateReport, package.ValidationReport })
        {
            var capability = r.Findings.Where(f => MissionRoleCapabilityManifest.IsCapabilityCode(f.Code)).ToArray();
            Assert.Equal(
                expected.Select(f => (f.Code, f.Severity, f.MissionId, f.Message)).OrderBy(x => x.MissionId, StringComparer.Ordinal),
                capability.Select(f => (f.Code, f.Severity, f.MissionId, f.Message)).OrderBy(x => x.MissionId, StringComparer.Ordinal));
        }
    }

    [Fact]
    public void Draft_with_unsupported_role_saves_and_reloads_while_export_stays_blocked()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddMissionFromTemplate("tpl-support-tanker", "support-1");
        var path = Path.Combine(Path.GetTempPath(), $"aegis-drg345-save-{Guid.NewGuid():N}.json");
        try
        {
            editor.Save(path);

            var reloaded = ScenarioDocumentEditor.Load(path);
            var mission = Assert.Single(reloaded.Missions);
            Assert.Equal("Tanker", mission.SupportRole);
            var live = reloaded.LiveValidate();
            Assert.Contains(live.Findings, f => f.Code == MissionRoleCapabilityManifest.CodeRoleNotExecuted && f.MissionId == "support-1");
            Assert.False(ScenarioExportCommand.Prepare(reloaded.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), Config).Allowed);
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
    public void Every_built_in_template_is_disclosed_by_the_manifest()
    {
        foreach (var template in MissionTemplateCatalog.All)
        {
            var mission = MissionTemplateCatalog.Materialize(template.TemplateId, "m-" + template.TemplateId);
            var assessment = MissionRoleCapabilityManifest.Assess(mission);
            Assert.NotEqual(MissionRoleExecution.Unknown, assessment.Execution);
        }

        Assert.Equal(
            MissionRoleExecution.NotExecuted,
            MissionRoleCapabilityManifest.Assess(MissionTemplateCatalog.Materialize("tpl-support-tanker", "t")).Execution);
    }

    [Fact]
    public void Manifest_entries_are_stable_and_ordered()
    {
        var entries = MissionRoleCapabilityManifest.Entries;

        Assert.Equal(
            new[] { "Ferry", "Patrol", "Strike", "Support/AEW", "Support/EW", "Support/Tanker" },
            entries.Select(e => e.SupportRole == null ? e.MissionType : e.MissionType + "/" + e.SupportRole).ToArray());
        Assert.All(entries, e => Assert.Equal(MissionRoleCapabilityManifest.DefaultBackendId, e.BackendId));
        Assert.All(entries, e => Assert.NotEmpty(e.RequiredOrderKinds));
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.RuntimeEvidence)));
    }

    private static ScenarioDocumentDto LoadFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "data", "scenarios", "validation", fileName);
            if (File.Exists(candidate))
            {
                return ScenarioDocumentJsonLoader.LoadFromFile(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}
