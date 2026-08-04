using ProjectAegis.Data.Scenario;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class ScenarioLibraryApplyStateTests
{
    [Test]
    public void Apply_formats_available_and_unavailable_rows()
    {
        var entries = new[]
        {
            new ScenarioLibraryEntry(
                ScenarioId: "alpha",
                Title: "Alpha",
                PolicyId: "p1",
                TlBranch: "TL-0",
                Seed: 1,
                Location: "—",
                Year: "—",
                ProvenanceLabel: "authored",
                Available: true,
                UnavailableReason: null,
                Difficulty: "—",
                Complexity: "—",
                SourcePath: "/tmp/alpha.scenario.json"),
            new ScenarioLibraryEntry(
                ScenarioId: "beta",
                Title: "Beta",
                PolicyId: "p2",
                TlBranch: "TL-2",
                Seed: 2,
                Location: "—",
                Year: "—",
                ProvenanceLabel: "authored",
                Available: false,
                UnavailableReason: ScenarioLibraryReasons.DbMismatch,
                Difficulty: "—",
                Complexity: "—",
                SourcePath: "/tmp/beta.scenario.json"),
        };

        var presentation = ScenarioLibraryApplyState.Apply(entries);

        Assert.That(presentation.Count, Is.EqualTo(2));
        Assert.That(presentation.Lines[0], Is.EqualTo("Alpha  [available]"));
        Assert.That(presentation.Lines[1], Is.EqualTo("Beta  [unavailable: DB_MISMATCH]"));
        Assert.That(presentation.Rows[1].UnavailableReason, Is.EqualTo(ScenarioLibraryReasons.DbMismatch));
    }

    [Test]
    public void ApplyPreview_null_is_zero_state_instruction()
    {
        var preview = ScenarioLibraryApplyState.ApplyPreview(null);

        Assert.That(preview.IsZeroState, Is.True);
        Assert.That(preview.InstructionLine, Is.EqualTo(ScenarioLibraryApplyState.ZeroStateInstruction));
        Assert.That(preview.TitleLine, Is.Empty);
    }

    [Test]
    public void ApplyPreview_selected_fills_metadata_fields()
    {
        var entry = new ScenarioLibraryEntry(
            ScenarioId: "alpha",
            Title: "Alpha Patrol",
            PolicyId: "baltic-patrol",
            TlBranch: "TL-2",
            Seed: 42,
            Location: "—",
            Year: "—",
            ProvenanceLabel: "authored",
            Available: true,
            UnavailableReason: null,
            Difficulty: "—",
            Complexity: "—",
            SourcePath: "/data/scenarios/alpha.scenario.json");

        var preview = ScenarioLibraryApplyState.ApplyPreview(entry);

        Assert.That(preview.IsZeroState, Is.False);
        Assert.That(preview.TitleLine, Is.EqualTo("Alpha Patrol"));
        Assert.That(preview.ScenarioIdLine, Is.EqualTo("ID: alpha"));
        Assert.That(preview.AvailabilityLine, Is.EqualTo("Available"));
        Assert.That(preview.PolicyLine, Is.EqualTo("Policy: baltic-patrol"));
        Assert.That(preview.TlBranchLine, Is.EqualTo("TL: TL-2"));
        Assert.That(preview.SeedLine, Is.EqualTo("Seed: 42"));
        Assert.That(preview.ProvenanceLine, Is.EqualTo("Provenance: authored"));
        Assert.That(preview.SourcePathLine, Does.Contain("alpha.scenario.json"));
    }
}
