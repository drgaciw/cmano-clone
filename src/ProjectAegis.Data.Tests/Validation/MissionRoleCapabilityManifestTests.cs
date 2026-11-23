namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

/// <summary>
/// DRG-345 / proposed AME-6.11: the mission role capability manifest discloses whether the
/// selected execution backend runs each authored mission role, and unsupported roles produce
/// stable Error findings that block Export/Play while Save stays available.
/// </summary>
public sealed class MissionRoleCapabilityManifestTests
{
    private static readonly ValidationConfig Config = new();

    public static IEnumerable<TestCaseData> RoleTable()
    {
        yield return new TestCaseData("Patrol", null, MissionRoleExecution.Executed, null);
        yield return new TestCaseData("Strike", null, MissionRoleExecution.Executed, null);
        yield return new TestCaseData("Ferry", null, MissionRoleExecution.NotExecuted, MissionRoleCapabilityManifest.CodeRoleNotExecuted);
        yield return new TestCaseData("Support", "Tanker", MissionRoleExecution.NotExecuted, MissionRoleCapabilityManifest.CodeRoleNotExecuted);
        yield return new TestCaseData("Support", "AEW", MissionRoleExecution.NotExecuted, MissionRoleCapabilityManifest.CodeRoleNotExecuted);
        yield return new TestCaseData("Support", "EW", MissionRoleExecution.NotExecuted, MissionRoleCapabilityManifest.CodeRoleNotExecuted);
        yield return new TestCaseData("Support", null, MissionRoleExecution.Unknown, MissionRoleCapabilityManifest.CodeRoleUnknown);
        yield return new TestCaseData("Support", "Jammer", MissionRoleExecution.Unknown, MissionRoleCapabilityManifest.CodeRoleUnknown);
        yield return new TestCaseData("Escort", null, MissionRoleExecution.Unknown, MissionRoleCapabilityManifest.CodeRoleUnknown);
        yield return new TestCaseData("", null, MissionRoleExecution.Unknown, MissionRoleCapabilityManifest.CodeRoleUnknown);
    }
    [TestCaseSource(nameof(RoleTable))]
    public void Assess_classifies_authored_role_against_default_backend(
        string type,
        string? supportRole,
        MissionRoleExecution expected,
        string? expectedCode)
    {
        var mission = new ScenarioMissionDto { Id = "m1", Type = type, SupportRole = supportRole, AssignedUnitIds = ["u1"] };

        var assessment = MissionRoleCapabilityManifest.Assess(mission);

        Assert.That(assessment.Execution, Is.EqualTo(expected));
        Assert.That(assessment.FindingCode, Is.EqualTo(expectedCode));
        Assert.That(assessment.BackendId, Is.EqualTo(MissionRoleCapabilityManifest.DefaultBackendId));
        Assert.That(assessment.MissionId, Is.EqualTo("m1"));
        Assert.That(string.IsNullOrWhiteSpace(assessment.Message), Is.False);
    }
    [TestCase("support", "tanker")]
    [TestCase("SUPPORT", "Tanker")]
    [TestCase(" Support ", " AEW ")]
    public void Assess_role_matching_is_case_and_whitespace_insensitive(string type, string role)
    {
        var assessment = MissionRoleCapabilityManifest.Assess(
            new ScenarioMissionDto { Id = "m1", Type = type, SupportRole = role });

        Assert.That(assessment.Execution, Is.EqualTo(MissionRoleExecution.NotExecuted));
    }

    [Test]
    public void Unknown_backend_marks_every_mission_unknown_backend()
    {
        var mission = new ScenarioMissionDto { Id = "p1", Type = "Patrol" };

        var assessment = MissionRoleCapabilityManifest.Assess(mission, "no-such-backend");

        Assert.That(assessment.Execution, Is.EqualTo(MissionRoleExecution.Unknown));
        Assert.That(assessment.FindingCode, Is.EqualTo(MissionRoleCapabilityManifest.CodeBackendUnknown));
        Assert.That(assessment.BackendId, Is.EqualTo("no-such-backend"));
    }

    [Test]
    public void Findings_are_error_severity_with_stable_codes_and_actionable_messages()
    {
        var doc = LoadFixture("role-capability-unsupported.json");

        var findings = MissionRoleCapabilityManifest.EvaluateFindings(doc);

        Assert.That(            findings.Select(f => (f.MissionId!, f.Code)).ToArray(), Is.EqualTo(            new[]
            {
                ("escort-1", MissionRoleCapabilityManifest.CodeRoleUnknown),
                ("ferry-1", MissionRoleCapabilityManifest.CodeRoleNotExecuted),
                ("support-aew", MissionRoleCapabilityManifest.CodeRoleNotExecuted),
                ("support-ew", MissionRoleCapabilityManifest.CodeRoleNotExecuted),
                ("support-tanker", MissionRoleCapabilityManifest.CodeRoleNotExecuted),
            }));
        foreach (var f in findings)
{
Assert.That(f.Severity, Is.EqualTo(ValidationSeverity.Error));
}
        foreach (var f in findings)
{
Assert.That(f.Message, Does.Contain("Save remains available"));
}
        foreach (var f in findings)
{
Assert.That(f.Data!["backend"], Is.EqualTo(MissionRoleCapabilityManifest.DefaultBackendId));
}

        var tanker = AssertEx.Single(findings, f => f.MissionId == "support-tanker");
        Assert.That(            tanker.Message, Is.EqualTo(            "Support mission 'support-tanker' role 'Tanker' is not executed by backend 'baltic-replay-harness'. "
            + "Export and Play are blocked; Save remains available. Remove the mission or change it to a supported role (Patrol, Strike)."));
        Assert.That(tanker.Data!["missionType"], Is.EqualTo("Support"));
        Assert.That(tanker.Data!["supportRole"], Is.EqualTo("Tanker"));
        Assert.That(tanker.Data!["execution"], Is.EqualTo("NotExecuted"));
    }

    [Test]
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
        Assert.That(Key(b), Is.EqualTo(Key(a)));
        Assert.That(            MissionRoleCapabilityManifest.AssessDocument(reversed).Select(x => x.MissionId), Is.EqualTo(
            MissionRoleCapabilityManifest.AssessDocument(doc).Select(x => x.MissionId)));
    }

    [Test]
    public void AssessDocument_discloses_supported_and_unsupported_missions_in_mission_id_order()
    {
        var doc = LoadFixture("role-capability-unsupported.json");

        var rows = MissionRoleCapabilityManifest.AssessDocument(doc);

        Assert.That(            rows.Select(r => r.MissionId).ToArray(), Is.EqualTo(            new[] { "escort-1", "ferry-1", "patrol-1", "support-aew", "support-ew", "support-tanker" }));
        var patrol = AssertEx.Single(rows, r => r.MissionId == "patrol-1");
        Assert.That(patrol.Execution, Is.EqualTo(MissionRoleExecution.Executed));
        Assert.That(patrol.FindingCode, Is.Null);
    }

    [Test]
    public void Supported_fixture_has_no_capability_findings_and_exports()
    {
        var doc = LoadFixture("role-capability-supported.json");

        Assert.That(MissionRoleCapabilityManifest.EvaluateFindings(doc), Is.Empty);
        var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(
            doc, InMemoryCatalogReader.BalticPatrolFixture(), Config);
        Assert.That(allowed, Is.True);
        Assert.That(report.Findings.Any(f => MissionRoleCapabilityManifest.IsCapabilityCode(f.Code)), Is.False);
    }

    [Test]
    public void Validation_engine_export_gate_and_prepare_all_block_unsupported_roles()
    {
        var doc = LoadFixture("role-capability-unsupported.json");
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var expected = MissionRoleCapabilityManifest.EvaluateFindings(doc);

        var report = new ScenarioValidationEngine().Validate(doc, catalog, Config);
        var (allowed, gateReport) = ScenarioValidationExportGate.EvaluateExport(doc, catalog, Config);
        var package = ScenarioExportCommand.Prepare(doc, catalog, Config);

        Assert.That(allowed, Is.False);
        Assert.That(package.Allowed, Is.False);
        foreach (var r in new[] { report, gateReport, package.ValidationReport })
        {
            var capability = r.Findings.Where(f => MissionRoleCapabilityManifest.IsCapabilityCode(f.Code)).ToArray();
            Assert.That(                capability.Select(f => (f.Code, f.Severity, f.MissionId, f.Message)).OrderBy(x => x.MissionId, StringComparer.Ordinal), Is.EqualTo(                expected.Select(f => (f.Code, f.Severity, f.MissionId, f.Message)).OrderBy(x => x.MissionId, StringComparer.Ordinal)));
        }
    }

    [Test]
    public void Draft_with_unsupported_role_saves_and_reloads_while_export_stays_blocked()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddMissionFromTemplate("tpl-support-tanker", "support-1");
        var path = Path.Combine(Path.GetTempPath(), $"aegis-drg345-save-{Guid.NewGuid():N}.json");
        try
        {
            editor.Save(path);

            var reloaded = ScenarioDocumentEditor.Load(path);
            var mission = AssertEx.Single(reloaded.Missions);
            Assert.That(mission.SupportRole, Is.EqualTo("Tanker"));
            var live = reloaded.LiveValidate();
            Assert.That(live.Findings.Any(f => f.Code == MissionRoleCapabilityManifest.CodeRoleNotExecuted && f.MissionId == "support-1"), Is.True);
            Assert.That(ScenarioExportCommand.Prepare(reloaded.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), Config).Allowed, Is.False);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Every_built_in_template_is_disclosed_by_the_manifest()
    {
        foreach (var template in MissionTemplateCatalog.All)
        {
            var mission = MissionTemplateCatalog.Materialize(template.TemplateId, "m-" + template.TemplateId);
            var assessment = MissionRoleCapabilityManifest.Assess(mission);
            Assert.That(assessment.Execution, Is.Not.EqualTo(MissionRoleExecution.Unknown));
        }

        Assert.That(            MissionRoleCapabilityManifest.Assess(MissionTemplateCatalog.Materialize("tpl-support-tanker", "t")).Execution, Is.EqualTo(            MissionRoleExecution.NotExecuted));
    }

    [Test]
    public void Manifest_entries_are_stable_and_ordered()
    {
        var entries = MissionRoleCapabilityManifest.Entries;

        Assert.That(            entries.Select(e => e.SupportRole == null ? e.MissionType : e.MissionType + "/" + e.SupportRole).ToArray(), Is.EqualTo(            new[] { "Ferry", "Patrol", "Strike", "Support/AEW", "Support/EW", "Support/Tanker" }));
        foreach (var e in entries)
{
Assert.That(e.BackendId, Is.EqualTo(MissionRoleCapabilityManifest.DefaultBackendId));
}
        foreach (var e in entries)
{
Assert.That(e.RequiredOrderKinds, Is.Not.Empty);
}
        foreach (var e in entries)
{
Assert.That(string.IsNullOrWhiteSpace(e.RuntimeEvidence), Is.False);
}
    }

    [TestCase("Patrol")]
    [TestCase("Strike")]
    public void Cli_simulate_backend_classifies_patrol_and_strike_as_generic_only_warning(string type)
    {
        var mission = new ScenarioMissionDto { Id = "m1", Type = type, AssignedUnitIds = ["u1"] };

        var assessment = MissionRoleCapabilityManifest.Assess(mission, MissionRoleCapabilityManifest.CliSimulateBackendId);

        Assert.That(assessment.Execution, Is.EqualTo(MissionRoleExecution.GenericOnly));
        Assert.That(assessment.FindingCode, Is.EqualTo(MissionRoleCapabilityManifest.CodeRoleGenericOnly));
        Assert.That(assessment.BackendId, Is.EqualTo(MissionRoleCapabilityManifest.CliSimulateBackendId));
        Assert.That(assessment.Message, Does.Contain("generic").IgnoreCase);
        Assert.That(assessment.Message, Does.Contain(MissionRoleCapabilityManifest.CliSimulateBackendId));
        Assert.That(MissionRoleCapabilityManifest.IsCapabilityCode(MissionRoleCapabilityManifest.CodeRoleGenericOnly), Is.True);
        Assert.That(MissionRoleCapabilityManifest.IsBlockingCapabilityCode(MissionRoleCapabilityManifest.CodeRoleGenericOnly), Is.False);
    }

    [Test]
    public void Generic_only_fixture_emits_warning_on_cli_backend_and_allows_export_while_default_is_clean()
    {
        var doc = LoadFixture("role-capability-generic-only.json");
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var defaultFindings = MissionRoleCapabilityManifest.EvaluateFindings(doc, MissionRoleCapabilityManifest.DefaultBackendId);
        Assert.That(defaultFindings, Is.Empty);

        var cliFindings = MissionRoleCapabilityManifest.EvaluateFindings(doc, MissionRoleCapabilityManifest.CliSimulateBackendId);
        Assert.That(cliFindings.Select(f => (f.MissionId!, f.Code, f.Severity)).ToArray(), Is.EqualTo(new[]
        {
            ("patrol-1", MissionRoleCapabilityManifest.CodeRoleGenericOnly, ValidationSeverity.Warning),
            ("strike-1", MissionRoleCapabilityManifest.CodeRoleGenericOnly, ValidationSeverity.Warning),
        }));
        foreach (var f in cliFindings)
        {
            Assert.That(f.Message, Does.Contain("generic").IgnoreCase);
            Assert.That(f.Data!["execution"], Is.EqualTo("GenericOnly"));
        }

        var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(
            doc,
            catalog,
            Config,
            MissionRoleCapabilityManifest.CliSimulateBackendId);
        Assert.That(allowed, Is.True);
        Assert.That(report.Passed, Is.True);
        Assert.That(report.Findings.Count(f => f.Code == MissionRoleCapabilityManifest.CodeRoleGenericOnly), Is.EqualTo(2));
    }

    [Test]
    public void Mixed_tiers_fixture_keeps_tanker_aew_ew_as_blocking_errors_on_both_backends_while_save_stays_allowed()
    {
        var doc = LoadFixture("role-capability-mixed-tiers.json");
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var (allowedDefault, reportDefault) = ScenarioValidationExportGate.EvaluateExport(
            doc, catalog, Config, MissionRoleCapabilityManifest.DefaultBackendId);
        var (allowedCli, reportCli) = ScenarioValidationExportGate.EvaluateExport(
            doc, catalog, Config, MissionRoleCapabilityManifest.CliSimulateBackendId);

        Assert.That(allowedDefault, Is.False);
        Assert.That(allowedCli, Is.False);
        Assert.That(
            reportDefault.Findings.Where(f => f.Code == MissionRoleCapabilityManifest.CodeRoleNotExecuted).Select(f => f.MissionId).ToArray(),
            Is.EqualTo(new[] { "support-aew", "support-ew", "support-tanker" }));
        Assert.That(
            reportCli.Findings.Where(f => f.Code == MissionRoleCapabilityManifest.CodeRoleNotExecuted).Select(f => f.MissionId).ToArray(),
            Is.EqualTo(new[] { "support-aew", "support-ew", "support-tanker" }));
        Assert.That(
            reportCli.Findings.Where(f => f.Code == MissionRoleCapabilityManifest.CodeRoleGenericOnly).Select(f => f.MissionId).ToArray(),
            Is.EqualTo(new[] { "patrol-1", "strike-1" }));

        var draftPath = Path.Combine(Path.GetTempPath(), $"aegis-drg371-draft-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentJsonWriter.WriteToFile(doc, draftPath);
            using var session = ScenarioAuthoringSession.Open(draftPath);
            var saveOutcome = ScenarioSaveExportGate.SaveDraft(session, catalog, Config);
            Assert.That(saveOutcome.Saved, Is.True);
            Assert.That(saveOutcome.BlockingFindingCount, Is.EqualTo(3));
        }
        finally
        {
            if (File.Exists(draftPath))
            {
                File.Delete(draftPath);
            }
        }
    }

    [TestCase(" cli-simulate-sample ")]
    [TestCase("CLI-SIMULATE-SAMPLE")]
    public void EntriesForBackend_Find_and_Assess_trim_and_ignore_case_on_backendId(string rawBackendId)
    {
        var entries = MissionRoleCapabilityManifest.EntriesForBackend(rawBackendId);
        Assert.That(entries, Has.Count.EqualTo(MissionRoleCapabilityManifest.CliSimulateEntries.Count));
        Assert.That(entries[0].BackendId, Is.EqualTo(MissionRoleCapabilityManifest.CliSimulateBackendId));

        var found = MissionRoleCapabilityManifest.Find("Patrol", null, rawBackendId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Execution, Is.EqualTo(MissionRoleExecution.GenericOnly));
        Assert.That(found.BackendId, Is.EqualTo(MissionRoleCapabilityManifest.CliSimulateBackendId));

        var assessment = MissionRoleCapabilityManifest.Assess(
            new ScenarioMissionDto { Id = "p1", Type = "Patrol", AssignedUnitIds = ["u1"] },
            rawBackendId);
        Assert.That(assessment.Execution, Is.EqualTo(MissionRoleExecution.GenericOnly));
        Assert.That(assessment.FindingCode, Is.EqualTo(MissionRoleCapabilityManifest.CodeRoleGenericOnly));
        Assert.That(assessment.BackendId, Is.EqualTo(MissionRoleCapabilityManifest.CliSimulateBackendId));
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
