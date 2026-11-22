using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>DRG-747: opt-in --provider fake on mission_plan_suggest (FakeChatClient / fixtures only).</summary>
[TestFixture]
public sealed class MissionPlanSuggestProviderModeTests
{
    [Test]
    public void Provider_fake_valid_draft_fixture_yields_draft_and_diff()
    {
        using var writer = new StringWriter();
        var exit = MissionPlanSuggestCommand.RunProviderFake(
            intent: "Blue forces patrol the Baltic strait chokepoint with unit u1",
            fixtureCaseId: "valid-draft",
            scriptPath: null,
            expectedEditVersion: null,
            currentEditVersion: null,
            allowedUnitIds: null,
            allowedTargetIds: null,
            supportedMissionTypes: null,
            idempotencyKey: null,
            dbRef: null,
            output: writer);

        var json = writer.ToString();
        Assert.That(exit, Is.EqualTo(0));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("mode").GetString(), Is.EqualTo("provider"));
        Assert.That(root.GetProperty("provider").GetString(), Is.EqualTo("fake"));
        Assert.That(root.GetProperty("outcomeClass").GetString(), Is.EqualTo("ValidDraft"));
        Assert.That(root.GetProperty("apply").GetBoolean(), Is.False);
        Assert.That(root.GetProperty("transparencyLabel").GetString(), Is.EqualTo("agent-assisted"));

        var tags = root.GetProperty("provenanceTags").EnumerateArray().ToList();
        Assert.That(tags, Is.Not.Empty);
        Assert.That(tags[0].GetProperty("source").GetString(), Is.EqualTo("ai"));

        var diff = root.GetProperty("proposedOperationsDiff").EnumerateArray().ToList();
        Assert.That(diff, Is.Not.Empty);
        Assert.That(diff[0].GetProperty("op").GetString(), Is.EqualTo("AddPatrolMission"));
        Assert.That(diff[0].GetProperty("applyVerb").GetString(), Is.EqualTo("mission_add_patrol"));

        Assert.That(root.TryGetProperty("draftDocument", out var draft) && draft.ValueKind == JsonValueKind.Object, Is.True);
        Assert.That(root.GetProperty("missions").EnumerateArray().Select(e => e.GetString()), Does.Contain("patrol-blue-1"));
    }

    [Test]
    public void Provider_fake_stale_revision_refused_with_conflict()
    {
        using var writer = new StringWriter();
        var exit = MissionPlanSuggestCommand.RunProviderFake(
            intent: "Blue patrol with u1",
            fixtureCaseId: "stale-revision",
            scriptPath: null,
            expectedEditVersion: null,
            currentEditVersion: null,
            allowedUnitIds: null,
            allowedTargetIds: null,
            supportedMissionTypes: null,
            idempotencyKey: null,
            dbRef: null,
            output: writer);

        var json = writer.ToString();
        Assert.That(exit, Is.EqualTo(3));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("ok").GetBoolean(), Is.False);
        Assert.That(root.GetProperty("code").GetString(), Is.EqualTo("CONFLICT"));
        Assert.That(root.GetProperty("message").GetString()!, Does.Contain("editVersion").IgnoreCase);

        var details = root.GetProperty("details");
        Assert.That(details.GetProperty("outcomeClass").GetString(), Is.EqualTo("Conflict"));
        Assert.That(details.GetProperty("apply").GetBoolean(), Is.False);
    }

    [Test]
    public void Keyword_Run_unchanged_when_provider_not_used()
    {
        // Pin: existing keyword contract remains byte-stable for MissionPlanSuggestCommandTests.
        using var writer = new StringWriter();
        var exit = MissionPlanSuggestCommand.Run("baltic patrol with comms degraded", writer);
        var json = writer.ToString();
        Assert.That(exit, Is.EqualTo(0));
        Assert.That(json, Does.Contain("\"ok\": true"));
        Assert.That(json, Does.Contain("mission_add_patrol"));
        Assert.That(json, Does.Not.Contain("proposedOperationsDiff"));
        Assert.That(json, Does.Not.Contain("\"mode\":"));
    }
}
