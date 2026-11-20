namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Campaign;
using NUnit.Framework;

public sealed class CampaignLibraryTests
{
    [Test]
    public void Load_fixture_preserves_sequence_order()
    {
        var path = ResolveFixturePath();
        if (path is null)
        {
            return;
        }

        var doc = CampaignDocumentJsonLoader.LoadFromFile(path);

        Assert.That(doc.CampaignId, Is.EqualTo("baltic-patrol-campaign"));
        Assert.That(doc.Title, Is.EqualTo("Baltic Patrol Campaign"));
        Assert.That(string.IsNullOrWhiteSpace(doc.Synopsis), Is.False);
        Assert.That(doc.Members.Count, Is.EqualTo(3));
        Assert.That(doc.Members[0].Sequence, Is.EqualTo(1));
        Assert.That(doc.Members[1].Sequence, Is.EqualTo(2));
        Assert.That(doc.Members[2].Sequence, Is.EqualTo(3));
        Assert.That(doc.Members[0].ScenarioId, Is.EqualTo("baltic-patrol"));
        Assert.That(doc.Members[1].ScenarioId, Is.EqualTo("ferry-redeploy"));
        Assert.That(doc.Members[2].ScenarioId, Is.EqualTo("strike-package"));
        foreach (var m in doc.Members)
{
Assert.That(m.Completed, Is.False);
}
    }

    [Test]
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
        Assert.That(json, Does.Contain("\"campaignId\":"));
        Assert.That(json, Does.Contain("\"title\":"));
        Assert.That(json, Does.Contain("\"synopsis\":"));
        Assert.That(json, Does.Contain("\"members\":"));

        // Stable order: campaignId before title before synopsis before members.
        var idAt = json.IndexOf("\"campaignId\"", StringComparison.Ordinal);
        var titleAt = json.IndexOf("\"title\"", StringComparison.Ordinal);
        var synopsisAt = json.IndexOf("\"synopsis\"", StringComparison.Ordinal);
        var membersAt = json.IndexOf("\"members\"", StringComparison.Ordinal);
        Assert.That(idAt >= 0 && idAt < titleAt && titleAt < synopsisAt && synopsisAt < membersAt, Is.True);

        // Members written in sequence order (alpha/1 before zeta/2).
        var alphaAt = json.IndexOf("alpha", StringComparison.Ordinal);
        var zetaAt = json.IndexOf("zeta", StringComparison.Ordinal);
        Assert.That(alphaAt >= 0 && alphaAt < zetaAt, Is.True);

        var reloaded = CampaignDocumentJsonLoader.LoadFromJson(json);
        Assert.That(reloaded, Is.Not.Null);
        Assert.That(reloaded!.CampaignId, Is.EqualTo(doc.CampaignId));
        Assert.That(reloaded.Members.Count, Is.EqualTo(2));
        Assert.That(reloaded.Members[0].ScenarioId, Is.EqualTo("alpha"));
        Assert.That(reloaded.Members[0].Completed, Is.True);
        Assert.That(reloaded.Members[1].ScenarioId, Is.EqualTo("zeta"));
        Assert.That(reloaded.Members[1].DisplayTitle, Is.EqualTo("Zeta"));
    }

    [Test]
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

        Assert.That(CampaignProgress.IsCampaignComplete(doc), Is.False);
        Assert.That(CampaignProgress.NextScenarioId(doc), Is.EqualTo("a"));

        var afterA = CampaignProgress.MarkComplete(doc, "a");
        Assert.That(afterA.Members[0].Completed, Is.True);
        Assert.That(afterA.Members[1].Completed, Is.False);
        Assert.That(CampaignProgress.IsCampaignComplete(afterA), Is.False);
        Assert.That(CampaignProgress.NextScenarioId(afterA), Is.EqualTo("b"));
        // Original unchanged (pure).
        Assert.That(doc.Members[0].Completed, Is.False);

        var afterBoth = CampaignProgress.MarkComplete(afterA, "b");
        Assert.That(CampaignProgress.IsCampaignComplete(afterBoth), Is.True);
        Assert.That(CampaignProgress.NextScenarioId(afterBoth), Is.Null);
    }

    [Test]
    public void MarkComplete_unknown_id_is_noop()
    {
        var doc = new CampaignDocument(
            "c1",
            "C1",
            "s",
            new[] { new CampaignScenarioMember("a", 1, false) });

        var next = CampaignProgress.MarkComplete(doc, "missing");
        Assert.That(next, Is.SameAs(doc));
    }

    [Test]
    public void Lister_finds_fixture_campaign_with_members_available()
    {
        var campaignsDir = ScenarioDataPaths.TryResolveCampaignsDirectory();
        var scenariosDir = ScenarioDataPaths.TryResolveScenariosDirectory();
        if (campaignsDir is null || scenariosDir is null)
        {
            return;
        }

        var entries = CampaignLibraryLister.ListFromDirectory(campaignsDir, scenariosDir);
        Assert.That(entries, Is.Not.Empty);

        var baltic = entries.FirstOrDefault(e => e.CampaignId == "baltic-patrol-campaign");
        Assert.That(baltic, Is.Not.Null);
        Assert.That(baltic!.Title, Is.EqualTo("Baltic Patrol Campaign"));
        Assert.That(baltic.MemberCount, Is.EqualTo(3));
        Assert.That(baltic.CompletedCount, Is.EqualTo(0));
        Assert.That(baltic.NextScenarioId, Is.EqualTo("baltic-patrol"));
        Assert.That(baltic.Available, Is.True);
        Assert.That(baltic.UnavailableReason, Is.Null);
        Assert.That(baltic.SourcePath, Does.Contain("baltic-patrol-campaign.campaign.json"));
    }

    [Test]
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
            Assert.That(entries, Has.Exactly(1).Items);
            Assert.That(entries[0].Available, Is.False);
            Assert.That(entries[0].UnavailableReason, Is.EqualTo(CampaignLibraryReasons.MemberMissing));
            Assert.That(entries[0].MemberCount, Is.EqualTo(2));
        }
        finally
        {
            TryDeleteDir(tempCampaigns);
            TryDeleteDir(tempScenarios);
        }
    }

    [Test]
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
            Assert.That(entries, Has.Exactly(1).Items);
            Assert.That(entries[0].Available, Is.True);
            Assert.That(entries[0].UnavailableReason, Is.Null);
            Assert.That(entries[0].CompletedCount, Is.EqualTo(1));
            Assert.That(entries[0].MemberCount, Is.EqualTo(2));
            Assert.That(entries[0].NextScenarioId, Is.EqualTo("beta"));
        }
        finally
        {
            TryDeleteDir(tempCampaigns);
            TryDeleteDir(tempScenarios);
        }
    }

    [Test]
    public void ProjectFromPath_schema_error_marks_unavailable()
    {
        var temp = CreateTempDir("aegis-campaign-bad-");
        try
        {
            var badPath = Path.Combine(temp, "broken.campaign.json");
            File.WriteAllText(badPath, "{ not-valid-json ");

            var entry = CampaignLibraryProjection.ProjectFromPath(badPath);
            Assert.That(entry.Available, Is.False);
            Assert.That(entry.UnavailableReason, Is.EqualTo(CampaignLibraryReasons.SchemaError));
            Assert.That(entry.CampaignId, Is.EqualTo("broken"));
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Test]
    public void CampaignIdFromPath_strips_campaign_json_without_encoding_sequence()
    {
        Assert.That(            CampaignLibraryProjection.CampaignIdFromPath("/data/campaigns/baltic-patrol-campaign.campaign.json"), Is.EqualTo(            "baltic-patrol-campaign"));
        Assert.That(CampaignLibraryProjection.CampaignIdFromPath(null), Is.EqualTo("unknown"));
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
