using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// DRG-345 / proposed AME-6.11: GUI projection of the headless mission role capability manifest.
/// The projection, export gate and play gate derive from <see cref="MissionRoleCapabilityManifest"/>;
/// they never decide runtime support themselves.
/// </summary>
public sealed class MissionRoleCapabilityProjectionTests
{
    [Test]
    public void Manifest_execution_claims_match_the_orders_the_replay_harness_policy_generates()
    {
        var generated = new HashSet<OrderKind>(PatrolCandidateEngagePolicy.Candidates.Select(c => c.Kind));

        foreach (var entry in MissionRoleCapabilityManifest.Entries)
        {
            var allGenerated = entry.RequiredOrderKinds.All(k =>
                Enum.TryParse<OrderKind>(k, ignoreCase: false, out var kind) && generated.Contains(kind));
            var label = entry.MissionType + "/" + (entry.SupportRole ?? "-");

            Assert.That(
                entry.Execution == MissionRoleExecution.Executed,
                Is.EqualTo(allGenerated),
                $"{label}: manifest says {entry.Execution} but harness candidates are [{string.Join(",", generated)}] and role needs [{string.Join(",", entry.RequiredOrderKinds)}]");
        }
    }

    [Test]
    public void Project_rows_mirror_manifest_assessments_one_to_one()
    {
        var doc = LoadFixture("role-capability-unsupported.json");

        var rows = MissionRoleCapabilityProjection.Project(doc);
        var assessments = MissionRoleCapabilityManifest.AssessDocument(doc);

        Assert.That(rows.Select(r => r.MissionId), Is.EqualTo(assessments.Select(a => a.MissionId)));
        for (var i = 0; i < rows.Count; i++)
        {
            Assert.That(rows[i].Execution, Is.EqualTo(assessments[i].Execution));
            Assert.That(rows[i].FindingCode, Is.EqualTo(assessments[i].FindingCode));
            Assert.That(rows[i].Message, Is.EqualTo(assessments[i].Message));
            Assert.That(rows[i].BackendId, Is.EqualTo(MissionRoleCapabilityManifest.DefaultBackendId));
            Assert.That(rows[i].BlocksExportAndPlay, Is.EqualTo(assessments[i].Execution != MissionRoleExecution.Executed));
        }

        var patrol = rows.Single(r => r.MissionId == "patrol-1");
        Assert.That(patrol.StatusLabel, Is.EqualTo("Executed by baltic-replay-harness"));
        var tanker = rows.Single(r => r.MissionId == "support-tanker");
        Assert.That(tanker.RoleLabel, Is.EqualTo("Support / Tanker"));
        Assert.That(tanker.StatusLabel, Is.EqualTo("Not executed by baltic-replay-harness — Export and Play blocked"));
        var escort = rows.Single(r => r.MissionId == "escort-1");
        Assert.That(escort.StatusLabel, Is.EqualTo("Unknown role for baltic-replay-harness — Export and Play blocked"));
    }

    [Test]
    public void Template_rows_disclose_that_tanker_template_is_not_executed()
    {
        var rows = MissionRoleCapabilityProjection.ProjectTemplates();

        Assert.That(rows.Select(r => r.TemplateId), Is.EqualTo(MissionTemplateCatalog.All.Select(t => t.TemplateId)));
        Assert.That(rows.Select(r => r.DisplayName), Has.All.Not.Null.And.Not.Empty);
        var tanker = rows.Single(r => r.TemplateId == "tpl-support-tanker");
        Assert.That(tanker.Execution, Is.EqualTo(MissionRoleExecution.NotExecuted));
        Assert.That(tanker.StatusLabel, Does.Contain("Not executed"));
        Assert.That(rows.Single(r => r.TemplateId == "tpl-patrol-empty").Execution, Is.EqualTo(MissionRoleExecution.Executed));
    }

    [Test]
    public void Save_succeeds_for_unsupported_role_while_export_gate_discloses_unsupported_execution()
    {
        var path = NewTempPath("save");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var result = session.Bus.AddFromTemplate(session.EditVersion, "tpl-support-tanker", "support-1", save: true);

            Assert.That(result.Ok, Is.True, result.ErrorMessage);
            Assert.That(ScenarioDocumentEditor.Load(path).Missions.Single().SupportRole, Is.EqualTo("Tanker"));

            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            findings.RefreshImmediate();

            Assert.That(findings.CanExport, Is.False);
            Assert.That(findings.Gate.HasUnsupportedExecution, Is.True);
            Assert.That(findings.Gate.UnsupportedExecutionFindings.Select(f => f.MissionId), Is.EqualTo(new[] { "support-1" }));
            Assert.That(findings.BlockingReason, Does.Contain("Save is still available"));
            Assert.That(findings.Gate.PlayBlockingReason, Does.Contain(MissionRoleCapabilityManifest.CodeRoleNotExecuted));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Gui_findings_match_cli_export_gate_findings_code_severity_and_message()
    {
        var path = NewTempPath("parity");
        try
        {
            File.Copy(FixturePath("role-capability-unsupported.json"), path);
            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            findings.RefreshImmediate();

            var doc = session.Editor.ToDto();
            var (_, cliReport) = ScenarioValidationExportGate.EvaluateExport(
                doc, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());
            var expected = MissionRoleCapabilityManifest.EvaluateFindings(doc);

            static string[] Key(IEnumerable<ValidationFinding> fs) => fs
                .Where(f => MissionRoleCapabilityManifest.IsCapabilityCode(f.Code))
                .Select(f => $"{f.Severity}|{f.Code}|{f.MissionId}|{f.Message}")
                .ToArray();

            Assert.That(Key(findings.LastFindings), Is.EqualTo(Key(cliReport.Findings)));
            Assert.That(Key(findings.Gate.UnsupportedExecutionFindings), Is.EqualTo(Key(cliReport.Findings)));
            Assert.That(Key(findings.LastFindings).OrderBy(x => x, StringComparer.Ordinal), Is.EqualTo(Key(expected).OrderBy(x => x, StringComparer.Ordinal)));
            Assert.That(Key(findings.LastFindings), Has.Length.EqualTo(5));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void TryEnterPlay_force_confirm_cannot_bypass_unsupported_role()
    {
        var path = NewTempPath("play");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AddFromTemplate(session.EditVersion, "tpl-support-tanker", "support-1", save: true);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var controller = new EditModeController(session, findings);
            var before = File.ReadAllText(path);

            Assert.That(controller.TryEnterPlay(forceConfirmInvalid: true), Is.False);
            Assert.That(controller.Mode, Is.EqualTo(ScenarioHostMode.Edit));
            Assert.That(controller.LastPlayBlockReason, Does.Contain(MissionRoleCapabilityManifest.CodeRoleNotExecuted));
            Assert.That(controller.LastPlayBlockReason, Does.Contain("support-1"));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void TryEnterPlay_force_confirm_still_allows_non_capability_errors()
    {
        var path = NewTempPath("force");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AddFromTemplate(session.EditVersion, "tpl-patrol-empty", "patrol-empty", save: true);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var controller = new EditModeController(session, findings);

            Assert.That(controller.TryEnterPlay(forceConfirmInvalid: false), Is.False);
            Assert.That(controller.LastPlayBlockReason, Is.Not.Null);
            Assert.That(findings.Gate.HasUnsupportedExecution, Is.False);
            Assert.That(controller.TryEnterPlay(forceConfirmInvalid: true), Is.True);
            Assert.That(controller.LastPlayBlockReason, Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string NewTempPath(string tag) =>
        Path.Combine(Path.GetTempPath(), $"aegis-drg345-{tag}-{Guid.NewGuid():N}.json");

    private static ScenarioDocumentDto LoadFixture(string fileName) =>
        ScenarioDocumentJsonLoader.LoadFromFile(FixturePath(fileName));

    private static string FixturePath(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "data", "scenarios", "validation", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}
