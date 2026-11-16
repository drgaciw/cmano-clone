using System.Text.Json;
using ProjectAegis.Ai.Host.Authoring;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>DRG-747: opt-in --provider fake on mission_plan_suggest (FakeChatClient / fixtures only).</summary>
public sealed class MissionPlanSuggestProviderModeTests
{
    [Fact]
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
        Assert.Equal(0, exit);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("ok").GetBoolean());
        Assert.Equal("provider", root.GetProperty("mode").GetString());
        Assert.Equal("fake", root.GetProperty("provider").GetString());
        Assert.Equal("ValidDraft", root.GetProperty("outcomeClass").GetString());
        Assert.False(root.GetProperty("apply").GetBoolean());
        Assert.Equal(AuthoringTransparencyLabel.AgentAssisted, root.GetProperty("transparencyLabel").GetString());

        var tags = root.GetProperty("provenanceTags").EnumerateArray().ToList();
        Assert.NotEmpty(tags);
        Assert.Equal("ai", tags[0].GetProperty("source").GetString());

        var diff = root.GetProperty("proposedOperationsDiff").EnumerateArray().ToList();
        Assert.NotEmpty(diff);
        Assert.Equal("AddPatrolMission", diff[0].GetProperty("op").GetString());
        Assert.Equal("mission_add_patrol", diff[0].GetProperty("applyVerb").GetString());

        Assert.True(root.TryGetProperty("draftDocument", out var draft) && draft.ValueKind == JsonValueKind.Object);
        Assert.Contains("patrol-blue-1", root.GetProperty("missions").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
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
        Assert.Equal(3, exit);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("ok").GetBoolean());
        Assert.Equal("CONFLICT", root.GetProperty("code").GetString());
        Assert.Contains("editVersion", root.GetProperty("message").GetString()!, StringComparison.OrdinalIgnoreCase);

        var details = root.GetProperty("details");
        Assert.Equal("Conflict", details.GetProperty("outcomeClass").GetString());
        Assert.False(details.GetProperty("apply").GetBoolean());
    }

    [Fact]
    public void Keyword_Run_unchanged_when_provider_not_used()
    {
        // Pin: existing keyword contract remains byte-stable for MissionPlanSuggestCommandTests.
        using var writer = new StringWriter();
        var exit = MissionPlanSuggestCommand.Run("baltic patrol with comms degraded", writer);
        var json = writer.ToString();
        Assert.Equal(0, exit);
        Assert.Contains("\"ok\": true", json, StringComparison.Ordinal);
        Assert.Contains("mission_add_patrol", json, StringComparison.Ordinal);
        Assert.DoesNotContain("proposedOperationsDiff", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"mode\":", json, StringComparison.Ordinal);
    }
}
