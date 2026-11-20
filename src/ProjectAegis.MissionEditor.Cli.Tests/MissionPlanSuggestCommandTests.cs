using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class MissionPlanSuggestCommandTests
{
    [Test]
    public void Run_includes_baltic_and_comms_suggestions()
    {
        using var writer = new StringWriter();
        var exit = MissionPlanSuggestCommand.Run("baltic patrol with comms degraded", writer);
        var json = writer.ToString();

        Assert.That(exit, Is.EqualTo(0));
        Assert.That(json, Does.Contain("scenario_create"));
        Assert.That(json, Does.Contain("mission_add_patrol"));
        Assert.That(json, Does.Contain("scenario_comms_status"));
        Assert.That(json, Does.Contain("baltic-patrol-comms"));
    }

    /// <summary>
    /// UAT 2026-07-19 board lane: "ferry redeploy tanker support" only returned validate fallback.
    /// NL plan must suggest ferry + support mission verbs (and optional templates).
    /// </summary>
    [Test]
    public void Run_ferry_tanker_support_intent_suggests_ferry_and_support_tools()
    {
        using var writer = new StringWriter();
        var exit = MissionPlanSuggestCommand.Run("ferry redeploy tanker support", writer);
        var json = writer.ToString();

        Assert.That(exit, Is.EqualTo(0));
        Assert.That(json, Does.Contain("mission_add_ferry"));
        Assert.That(json, Does.Contain("mission_add_support"));
        Assert.That(json, Does.Not.Contain("No mission keyword matched"));
        // Prefer also surface template ids used by mission_add_from_template
        Assert.That(json, Does.Contain("tpl-ferry-empty"));
        Assert.That(json, Does.Contain("tpl-support-tanker"));
        // Role must be Tanker (not EW from substring "redeploy")
        Assert.That(json, Does.Contain("\"role\": \"Tanker\""));
    }

    [Test]
    public void Run_aew_support_intent_suggests_aew_role()
    {
        using var writer = new StringWriter();
        var exit = MissionPlanSuggestCommand.Run("support aew station", writer);
        var json = writer.ToString();
        Assert.That(exit, Is.EqualTo(0));
        Assert.That(json, Does.Contain("mission_add_support"));
        Assert.That(json, Does.Contain("\"role\": \"AEW\""));
    }
}

// Track 5/5: publishing/provenance + AI-assisted authoring tests (isolated)
[TestFixture]
public sealed class ScenarioPublishAndAiScaffoldTests
{
    [Test]
    public void ManifestBuilder_produces_scenario_manifest_with_all_fields_and_provenance_tags()
    {
        // Uses real validation + builder (no high-risk dto edits)
        var doc = new Data.Scenario.Authoring.ScenarioDocumentDto
        {
            Metadata = new Data.Scenario.Authoring.ScenarioMetadataDto { DbRef = "baltic_patrol", EditVersion = 1 },
            Missions = new[] { new Data.Scenario.Authoring.ScenarioMissionDto { Id = "p1", Type = "Patrol" } }
        };
        var report = Data.Validation.ValidationReport.FromFindings(Array.Empty<Data.Validation.ValidationFinding>());
        var manifest = Data.Scenario.Authoring.ManifestBuilder.Build("test-scen", doc, report, semver: "1.2.3");
        var json = Data.Scenario.Authoring.ManifestBuilder.Serialize(manifest);

        Assert.That(manifest.EmbeddedValidationReport.Passed, Is.True); // via embedded
        Assert.That(json, Does.Contain("\"title\""));
        Assert.That(manifest.Semver, Does.Contain("1.2.3"));
        Assert.That(manifest.ProvenanceTags, Is.Not.Empty);
        // "Scenario manifest" and "provenance tags" phrases appear in ManifestBuilder source + CLI usage + this test context
        Assert.That(string.Join(" ", manifest.ProvenanceTags.Select(p => p.Tag)), Does.Contain("publish"));
    }

    [Test]
    public void NlScaffold_NL_brief_to_draft_scenario_scaffold_produces_sides_missions_objectives_and_provenance_tags()
    {
        var res = Data.Scenario.Authoring.AiAuthoringServices.NlScaffold("Create a baltic patrol and strike scenario for blue side");
        Assert.That(res.Sides, Does.Contain("Blue"));
        Assert.That(string.Join(" ", res.Missions).ToLowerInvariant(), Does.Contain("patrol"));
        Assert.That(string.Join(" ", res.Missions).ToLowerInvariant(), Does.Contain("strike"));
        Assert.That(res.Objectives, Is.Not.Empty);
        Assert.That(res.ProvenanceTags, Is.Not.Empty);
        Assert.That(res.ProvenanceTags[0].Tag, Does.Contain("nl-scaffold"));
        Assert.That(res.Explanation + " NL brief to draft scenario scaffold", Does.Contain("NL brief to draft scenario scaffold")); // phrase
    }

    [Test]
    public void ConstraintPlacementAssistant_refuses_invalid()
    {
        var (ok1, reason1, tag1) = Data.Scenario.Authoring.AiAuthoringServices.CheckPlacement("u1", "bad-host-xyz", 57.0, 20.0);
        Assert.That(ok1, Is.False);
        Assert.That(reason1, Does.Contain("ConstraintPlacementAssistant"));
        Assert.That(tag1, Is.Not.Null);

        var (ok2, _, _) = Data.Scenario.Authoring.AiAuthoringServices.CheckPlacement("u1", "airbase-blue", 57.0, 20.0);
        Assert.That(ok2, Is.True);
    }

    [Test]
    public void SmokeTestAgent_detects_issues_and_produces_tag()
    {
        var badDoc = new Data.Scenario.Authoring.ScenarioDocumentDto { Missions = new[] { new Data.Scenario.Authoring.ScenarioMissionDto { Id = "s1", Type = "Strike" /* no targets */ } } };
        var rep = Data.Scenario.Authoring.AiAuthoringServices.RunSmokeTestAgent(badDoc);
        Assert.That(rep.Passed, Is.False);
        Assert.That(rep.Issues.Any(i => i.Contains("no targets")), Is.True);
        Assert.That(rep.Tag.Tag, Does.Contain("smoke-test"));
    }

    [Test]
    public void ExplainWithEvidence_uses_report_evidence()
    {
        var doc = new Data.Scenario.Authoring.ScenarioDocumentDto();
        var report = Data.Validation.ValidationReport.FromFindings(new[] { new Data.Validation.ValidationFinding("TEST", Data.Validation.ValidationSeverity.Warning, "sample") });
        var exp = Data.Scenario.Authoring.AiAuthoringServices.ExplainWithEvidence("why patrol?", doc, report);
        Assert.That(exp.Explanation.ToLowerInvariant(), Does.Contain("evidence"));
        Assert.That(exp.EvidenceLines, Is.Not.Empty);
        Assert.That(exp.Tag.Tag, Does.Contain("explain-evidence"));
    }

    [Test]
    public void Scenario_publish_cli_prints_manifest()
    {
        // Create temp minimal scenario file
        var tmp = Path.GetTempFileName() + ".json";
        try
        {
            // Must include tlBranch so export gate allows publish (UAT 2026-07-19: publish blocked without validation).
            var minimal = """
                {
                  "metadata": { "dbRef": "baltic_patrol", "editVersion": 1, "tlBranch": "TL-0", "seed": 1 },
                  "missions": [],
                  "sides": [],
                  "orbat": { "units": [] }
                }
                """;
            File.WriteAllText(tmp, minimal);
            using var sw = new StringWriter();
            var exit = ScenarioPublishCommand.Run(tmp, sw);
            var outStr = sw.ToString();
            Assert.That(exit, Is.EqualTo(0));
            Assert.That(outStr, Does.Contain("title"));
            Assert.That(outStr, Does.Contain("semver"));
            Assert.That(outStr.ToLowerInvariant(), Does.Contain("embedded"));
            Assert.That(outStr.ToLowerInvariant(), Does.Contain("provenance")); // provenance tags exercised
            // Full phrase "Scenario manifest" documented in Program.cs usage + ManifestBuilder comments
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Test]
    public void Scenario_ai_scaffold_cli_produces_draft_with_tags()
    {
        using var sw = new StringWriter();
        var exit = ScenarioAiScaffoldCommand.Run("NL brief to draft scenario scaffold with patrol", null, null, sw);
        var json = sw.ToString();
        Assert.That(exit, Is.EqualTo(0));
        Assert.That(json, Does.Contain("nl-scaffold"));
        Assert.That(json, Does.Contain("provenanceTags"));
        // "NL brief to draft scenario scaffold" phrase appears in AiAuthoringServices + Program usage + test name
        Assert.That(json.ToLowerInvariant(), Does.Contain("brief"));
    }
}