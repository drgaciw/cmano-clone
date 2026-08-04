using ProjectAegis.Data.Scenario.Campaign;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class CampaignLibraryApplyStateTests
{
    [Test]
    public void Apply_formats_available_and_unavailable_rows()
    {
        var entries = new[]
        {
            new CampaignLibraryEntry(
                CampaignId: "alpha",
                Title: "Alpha Campaign",
                MemberCount: 3,
                CompletedCount: 1,
                NextScenarioId: "leg-2",
                Available: true,
                UnavailableReason: null,
                SourcePath: "/tmp/alpha.campaign.json"),
            new CampaignLibraryEntry(
                CampaignId: "beta",
                Title: "Beta Campaign",
                MemberCount: 2,
                CompletedCount: 0,
                NextScenarioId: "missing",
                Available: false,
                UnavailableReason: CampaignLibraryReasons.MemberMissing,
                SourcePath: "/tmp/beta.campaign.json"),
        };

        var presentation = CampaignLibraryApplyState.Apply(entries);

        Assert.That(presentation.Count, Is.EqualTo(2));
        Assert.That(presentation.Lines[0], Is.EqualTo("Alpha Campaign  [1/3]  [available]"));
        Assert.That(presentation.Lines[1], Is.EqualTo("Beta Campaign  [0/2]  [unavailable: MEMBER_MISSING]"));
        Assert.That(presentation.Rows[1].UnavailableReason, Is.EqualTo(CampaignLibraryReasons.MemberMissing));
        Assert.That(presentation.Rows[0].NextScenarioId, Is.EqualTo("leg-2"));
    }

    [Test]
    public void ApplyPreview_null_is_zero_state_select_a_campaign()
    {
        var preview = CampaignLibraryApplyState.ApplyPreview(null);

        Assert.That(preview.IsZeroState, Is.True);
        Assert.That(preview.InstructionLine, Is.EqualTo(CampaignLibraryApplyState.ZeroStateInstruction));
        Assert.That(preview.InstructionLine, Is.EqualTo("Select a campaign"));
        Assert.That(preview.TitleLine, Is.Empty);
    }

    [Test]
    public void ApplyPreview_selected_fills_campaign_fields()
    {
        var entry = new CampaignLibraryEntry(
            CampaignId: "baltic-patrol-campaign",
            Title: "Baltic Patrol Campaign",
            MemberCount: 3,
            CompletedCount: 0,
            NextScenarioId: "baltic-patrol",
            Available: true,
            UnavailableReason: null,
            SourcePath: "/data/campaigns/baltic-patrol-campaign.campaign.json");

        var preview = CampaignLibraryApplyState.ApplyPreview(entry);

        Assert.That(preview.IsZeroState, Is.False);
        Assert.That(preview.TitleLine, Is.EqualTo("Baltic Patrol Campaign"));
        Assert.That(preview.CampaignIdLine, Is.EqualTo("ID: baltic-patrol-campaign"));
        Assert.That(preview.AvailabilityLine, Is.EqualTo("Available"));
        Assert.That(preview.ProgressLine, Is.EqualTo("Progress: 0/3"));
        Assert.That(preview.NextScenarioLine, Is.EqualTo("Next: baltic-patrol"));
        Assert.That(preview.SourcePathLine, Does.Contain("baltic-patrol-campaign.campaign.json"));
    }

    [Test]
    public void Apply_empty_is_empty_presentation()
    {
        var presentation = CampaignLibraryApplyState.Apply(null);
        Assert.That(presentation.Count, Is.EqualTo(0));
        Assert.That(presentation.Lines, Is.Empty);
    }
}
