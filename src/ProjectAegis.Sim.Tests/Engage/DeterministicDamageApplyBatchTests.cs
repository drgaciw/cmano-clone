using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class DeterministicDamageApplyBatchTests
{
    [Fact]
    public void Empty_batch_returns_empty_sorted_list()
    {
        var sorted = DeterministicDamageApplyBatch.Sort(Array.Empty<EngagementDamageOutcome>());
        Assert.Empty(sorted);
    }

    [Fact]
    public void Single_outcome_returns_unchanged()
    {
        var input = new[] { new EngagementDamageOutcome(7, 3, EngagementOutcomeCodes.Kill) };
        var sorted = DeterministicDamageApplyBatch.Sort(input);
        Assert.Single(sorted);
        Assert.Equal(input[0], sorted[0]);
    }

    [Fact]
    public void Outcomes_sorted_by_engagement_id_ascending()
    {
        var input = new[]
        {
            new EngagementDamageOutcome(30, 1, EngagementOutcomeCodes.Miss),
            new EngagementDamageOutcome(10, 2, EngagementOutcomeCodes.Kill),
            new EngagementDamageOutcome(20, 3, EngagementOutcomeCodes.Hit),
        };

        var sorted = DeterministicDamageApplyBatch.Sort(input);
        Assert.Equal(new ulong[] { 10, 20, 30 }, sorted.Select(o => o.EngagementId).ToArray());
    }

    [Fact]
    public void Same_engagement_id_tie_breaks_on_sequence_id()
    {
        var input = new[]
        {
            new EngagementDamageOutcome(5, 9, EngagementOutcomeCodes.Miss),
            new EngagementDamageOutcome(5, 1, EngagementOutcomeCodes.Kill),
            new EngagementDamageOutcome(5, 4, EngagementOutcomeCodes.Hit),
        };

        var sorted = DeterministicDamageApplyBatch.Sort(input);
        Assert.Equal(new ulong[] { 1, 4, 9 }, sorted.Select(o => o.SequenceId).ToArray());
    }
}