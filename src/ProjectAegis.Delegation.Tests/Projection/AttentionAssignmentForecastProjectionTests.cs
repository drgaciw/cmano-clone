using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Sim;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AttentionAssignmentForecastProjectionTests
{
    private static ObservedState State(int contacts = 10, int engagements = 2) =>
        new(
            SimTime: 100,
            ContactCount: contacts,
            ActiveEngagementCount: engagements,
            MemberAlive: new Dictionary<TargetId, bool>());

    [Test]
    public void Forecast_unavailable_without_agent()
    {
        var f = AttentionAssignmentForecastProjection.Forecast("", 20, 2, 1, State());
        Assert.That(f.IsAvailable, Is.False);
        Assert.That(f.FailureReason, Does.Contain("agent").IgnoreCase);
    }

    [Test]
    public void Forecast_unavailable_without_state()
    {
        var f = AttentionAssignmentForecastProjection.Forecast("a1", 20, 2, 1, null);
        Assert.That(f.IsAvailable, Is.False);
        Assert.That(f.StatusLine, Does.Contain("advisory").IgnoreCase);
    }

    [Test]
    public void Forecast_nominal_assignment_stays_nominal()
    {
        // light load: contacts*0.5 + eng*1 + members*0.25
        var f = AttentionAssignmentForecastProjection.Forecast("a1", 50, 2, 1, State(contacts: 2, engagements: 0));
        Assert.That(f.IsAvailable, Is.True);
        Assert.That(f.IsAdvisory, Is.True);
        Assert.That(f.Projected!.Tier, Is.EqualTo(AttentionTierName.Nominal));
        Assert.That(f.StatusLine, Does.Contain("FORECAST"));
        Assert.That(f.AccessibleLabel, Does.Contain("Not committed"));
    }

    [Test]
    public void Forecast_large_assignment_can_cross_tier()
    {
        // Heavy theater + many members should push over budget
        var f = AttentionAssignmentForecastProjection.Forecast(
            "a1",
            attentionBudget: 5,
            currentMemberCount: 2,
            additionalMembers: 20,
            state: State(contacts: 20, engagements: 5));

        Assert.That(f.IsAvailable, Is.True);
        Assert.That(f.Projected!.IsOverloaded, Is.True);
        Assert.That(f.Projected.Tier, Is.Not.EqualTo(AttentionTierName.Nominal));
        Assert.That(f.TierCrosses || f.Current!.Tier != AttentionTierName.Nominal, Is.True);
    }

    [Test]
    public void Forecast_rejects_negative_delta()
    {
        var f = AttentionAssignmentForecastProjection.Forecast("a1", 20, 2, -1, State());
        Assert.That(f.IsAvailable, Is.False);
    }
}
