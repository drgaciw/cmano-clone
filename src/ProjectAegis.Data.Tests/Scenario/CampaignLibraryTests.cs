namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Campaign;
using Xunit;

public sealed class CampaignLibraryTests
{
    [Fact]
    public void Load_fixture_preserves_sequence_order()
    {
        var path = ResolveFixturePath();
        if (path is null)
        {
            return;
        }

        var doc = CampaignDocumentJsonLoader.LoadFromFile(path);

        Assert.Equal("baltic-patrol-campaign", doc.CampaignId);
        Assert.Equal("Baltic Patrol Campaign", doc.Title);
        Assert.False(string.IsNullOrWhiteSpace(doc.Synopsis));
        Assert.Equal(3, doc.Members.Count);
        Assert.Equal(1, doc.Members[0].Sequence);
        Assert.Equal(2, doc.Members[1].Sequence);
        Assert.Equal(3, doc.Members[2].Sequence);
        Assert.Equal("baltic-patrol", doc.Members[0].ScenarioId);
        Assert.Equal("ferry-redeploy", doc.Members[1].ScenarioId);
        Assert.Equal("strike-package", doc.Members[2].ScenarioId);
        Assert.All(doc.Members, m => Assert.False(m.Completed));
    }

    [Fact]
    public void JsonWriter_round_trips_with_stable_property_order()
    {
        var doc = new CampaignDocument(
            CampaignId: "test-campaign",
            Title: "Test Campaign",
            Synopsis: "Synopsis text",
            Members: new[]
            {
                new CampaignScenarioMember("zeta", Sequence: 2, Completed: false, DisplayTitle: "Zeta"),
                new CampaignScenarioMember("alpha", Sequence: 1, Completed: true),
            });

        var json = CampaignDocumentJsonWriter.Serialize(doc);
        Assert.Contains("\"campaignId\":", json, StringComparison.Ordinal);
        Assert.Contains("\"title\":", json, StringComparison.Ordinal);
        Assert.Contains("\"synopsis\":", json, StringComparison.Ordinal);
        Assert.Contains("\"members\":", json, StringComparison.Ordinal);

        // Stable order: campaignId before title before synopsis before members.
        var idAt = json.IndexOf("\"campaignId\"", StringComparison.Ordinal);
        var titleAt = json.IndexOf("\"title\"", StringComparison.Ordinal);
        var synopsisAt = json.IndexOf("\"synopsis\"", StringComparison.Ordinal);
        var membersAt = json.IndexOf("\"members\"", StringComparison.Ordinal);
        Assert.True(idAt >= 0 && idAt < titleAt && titleAt < synopsisAt && synopsisAt < membersAt);

        // Members written in sequence order (alpha/1 before zeta/2).
        var alphaAt = json.IndexOf("alpha", StringComparison.Ordinal);
        var zetaAt = json.IndexOf("zeta", StringComparison.Ordinal);
        Assert.True(alphaAt >= 0 && alphaAt < zetaAt);

        var reloaded = CampaignDocumentJsonLoader.LoadFromJson(json);
        Assert.NotNull(reloaded);
        Assert.Equal(doc.CampaignId, reloaded!.CampaignId);
        Assert.Equal(2, reloaded.Members.Count);
        Assert.Equal("alpha", reloaded.Members[0].ScenarioId);
        Assert.True(reloaded.Members[0].Completed);
        Assert.Equal("zeta", reloaded.Members[1].ScenarioId);
        Assert.Equal("Zeta", reloaded.Members[1].DisplayTitle);
    }

    [Fact]
    public void MarkComplete_sets_member_and_IsCampaignComplete()
    {
        var doc = new CampaignDocument(
            CampaignId: "c1",
            Title: "C1",
            Synopsis: "s",
            Members: new[]
            {
                new CampaignScenarioMember("a", 1, Completed: false),
                new CampaignScenarioMember("b", 2, Completed: false),
            });

        Assert.False(CampaignProgress.IsCampaignComplete(doc));
        Assert.Equal("a", CampaignProgress.NextScenarioId(doc));

        var afterA = CampaignProgress.MarkComplete(doc, "a");
        Assert.True(afterA.Members[0].Completed);
        Assert.False(afterA.Members[1].Completed);
        Assert.False(CampaignProgress.IsCampaignComplete(afterA));
        Assert.Equal("b", CampaignProgress.NextScenarioId(afterA));
        // Original unchanged (pure).
        Assert.False(doc.Members[0].Completed);

        var afterBoth = CampaignProgress.MarkComplete(afterA, "b");
        Assert.True(CampaignProgress.IsCampaignComplete(afterBoth));
        Assert.Null(CampaignProgress.NextScenarioId(afterBoth));
    }

    [Fact]
    public void MarkComplete_unknown_id_is_noop()
    {
        var doc = new CampaignDocument(
            "c1",
            "C1",
            "s",
            new[] { new CampaignScenarioMember("a", 1, false) });

        var next = CampaignProgress.MarkComplete(doc, "missing");
        Assert.Same(doc, next);
    }

    [Fact]
    public void Lister_finds_fixture_campaign_with_members_available()
    {
        var campaignsDir = ScenarioDataPaths.TryResolveCampaignsDirectory();
        var scenariosDir = ScenarioDataPaths.TryResolveScenariosDirectory();
        if (campaignsDir is null || scenariosDir is null)
        {
            return;
        }

        var entries = CampaignLibraryLister.ListFromDirectory(campaignsDir, scenariosDir);
        Assert.NotEmpty(entries);

        var baltic = entries.FirstOrDefault(e => e.CampaignId == "baltic-patrol-campaign");
        Assert.NotNull(baltic);
        Assert.Equal("Baltic Patrol Campaign", baltic!.Title);
        Assert.Equal(3, baltic.MemberCount);
        Assert.Equal(0, baltic.CompletedCount);
        Assert.Equal("baltic-patrol", baltic.NextScenarioId);
        Assert.True(baltic.Available);
        Assert.Null(baltic.UnavailableReason);
        Assert.Contains("baltic-patrol-campaign.campaign.json", baltic.SourcePath, StringComparison.Ordinal);
    }

    [Fact]
    public void Lister_missing_member_is_unavailable_with_MEMBER_MISSING()
    {
        var tempCampaigns = CreateTempDir("aegis-campaigns-");
        var tempScenarios = CreateTempDir("aegis-scenarios-");
        try
        {
            // Only one of two members present.
            File.WriteAllText(
                Path.Combine(tempScenarios, "alpha.scenario.json"),
                """{"metadata":{"title":"Alpha","policyId":"p","seed":1},"missions":[]}""");

            var campaign = new CampaignDocument(
                CampaignId: "broken-campaign",
                Title: "Broken",
                Synopsis: "missing member",
                Members: new[]
                {
                    new CampaignScenarioMember("alpha", 1, false),
                    new CampaignScenarioMember("missing-scenario", 2, false),
                });
            var campaignPath = Path.Combine(tempCampaigns, "broken-campaign.campaign.json");
            CampaignDocumentJsonWriter.WriteToFile(campaign, campaignPath);

            var entries = CampaignLibraryLister.ListFromDirectory(tempCampaigns, tempScenarios);
            Assert.Single(entries);
            Assert.False(entries[0].Available);
            Assert.Equal(CampaignLibraryReasons.MemberMissing, entries[0].UnavailableReason);
            Assert.Equal(2, entries[0].MemberCount);
        }
        finally
        {
            TryDeleteDir(tempCampaigns);
            TryDeleteDir(tempScenarios);
        }
    }

    [Fact]
    public void Lister_all_members_present_is_available()
    {
        var tempCampaigns = CreateTempDir("aegis-campaigns-");
        var tempScenarios = CreateTempDir("aegis-scenarios-");
        try
        {
            File.WriteAllText(
                Path.Combine(tempScenarios, "alpha.scenario.json"),
                """{"metadata":{"title":"Alpha","policyId":"p","seed":1},"missions":[]}""");
            File.WriteAllText(
                Path.Combine(tempScenarios, "beta.scenario.json"),
                """{"metadata":{"title":"Beta","policyId":"p","seed":2},"missions":[]}""");

            var campaign = new CampaignDocument(
                CampaignId: "ok-campaign",
                Title: "OK",
                Synopsis: "all good",
                Members: new[]
                {
                    new CampaignScenarioMember("alpha", 1, Completed: true, DisplayTitle: "Alpha Leg"),
                    new CampaignScenarioMember("beta", 2, Completed: false),
                });
            CampaignDocumentJsonWriter.WriteToFile(
                campaign,
                Path.Combine(tempCampaigns, "ok-campaign.campaign.json"));

            var entries = CampaignLibraryLister.ListFromDirectory(tempCampaigns, tempScenarios);
            Assert.Single(entries);
            Assert.True(entries[0].Available);
            Assert.Null(entries[0].UnavailableReason);
            Assert.Equal(1, entries[0].CompletedCount);
            Assert.Equal(2, entries[0].MemberCount);
            Assert.Equal("beta", entries[0].NextScenarioId);
        }
        finally
        {
            TryDeleteDir(tempCampaigns);
            TryDeleteDir(tempScenarios);
        }
    }

    [Fact]
    public void ProjectFromPath_schema_error_marks_unavailable()
    {
        var temp = CreateTempDir("aegis-campaign-bad-");
        try
        {
            var badPath = Path.Combine(temp, "broken.campaign.json");
            File.WriteAllText(badPath, "{ not-valid-json ");

            var entry = CampaignLibraryProjection.ProjectFromPath(badPath);
            Assert.False(entry.Available);
            Assert.Equal(CampaignLibraryReasons.SchemaError, entry.UnavailableReason);
            Assert.Equal("broken", entry.CampaignId);
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Fact]
    public void CampaignIdFromPath_strips_campaign_json_without_encoding_sequence()
    {
        Assert.Equal(
            "baltic-patrol-campaign",
            CampaignLibraryProjection.CampaignIdFromPath("/data/campaigns/baltic-patrol-campaign.campaign.json"));
        Assert.Equal("unknown", CampaignLibraryProjection.CampaignIdFromPath(null));
    }

    private static string? ResolveFixturePath()
    {
        var campaigns = ScenarioDataPaths.TryResolveCampaignsDirectory();
        if (campaigns is null)
        {
            // Fallback: walk from cwd for worktree checkout.
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (var i = 0; i < 8 && dir is not null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "data", "campaigns", "baltic-patrol-campaign.campaign.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            return null;
        }

        var path = Path.Combine(campaigns, "baltic-patrol-campaign.campaign.json");
        return File.Exists(path) ? path : null;
    }

    private static string CreateTempDir(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDir(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
