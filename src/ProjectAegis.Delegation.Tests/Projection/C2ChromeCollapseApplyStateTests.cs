using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class C2ChromeCollapseApplyStateTests
{
    [Test]
    public void Apply_expanded_shows_collapse_affordances()
    {
        var applied = C2ChromeCollapseApplyState.Apply(C2ChromeCollapseState.Expanded);

        Assert.That(applied.MessageLogCollapsed, Is.False);
        Assert.That(applied.LeftDrawerCollapsed, Is.False);
        Assert.That(applied.MessageLogAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.CollapseMessageLogLabel));
        Assert.That(applied.LeftDrawerAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.CollapseLeftDrawerLabel));
    }

    [Test]
    public void Apply_collapsed_shows_expand_affordances()
    {
        var state = new C2ChromeCollapseState(
            MessageLogCollapsed: true,
            LeftDrawerCollapsed: true);
        var applied = C2ChromeCollapseApplyState.Apply(state);

        Assert.That(applied.MessageLogCollapsed, Is.True);
        Assert.That(applied.LeftDrawerCollapsed, Is.True);
        Assert.That(applied.MessageLogAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.ExpandMessageLogLabel));
        Assert.That(applied.LeftDrawerAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.ExpandLeftDrawerLabel));
    }

    [Test]
    public void Apply_null_returns_empty_expanded_defaults()
    {
        var applied = C2ChromeCollapseApplyState.Apply(null);
        Assert.That(applied.MessageLogCollapsed, Is.False);
        Assert.That(applied.LeftDrawerCollapsed, Is.False);
        Assert.That(applied.MessageLogAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.CollapseMessageLogLabel));
    }

    [Test]
    public void ProjectAndApply_round_trips_flags()
    {
        var applied = C2ChromeCollapseApplyState.ProjectAndApply(
            messageLogCollapsed: true,
            leftDrawerCollapsed: false);

        Assert.That(applied.MessageLogCollapsed, Is.True);
        Assert.That(applied.LeftDrawerCollapsed, Is.False);
        Assert.That(applied.MessageLogAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.ExpandMessageLogLabel));
        Assert.That(applied.LeftDrawerAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.CollapseLeftDrawerLabel));
    }

    [Test]
    public void State_toggle_helpers_flip_flags()
    {
        var state = C2ChromeCollapseState.Expanded
            .ToggleMessageLog()
            .ToggleLeftDrawer();

        Assert.That(state.MessageLogCollapsed, Is.True);
        Assert.That(state.LeftDrawerCollapsed, Is.True);

        state = state.SetMessageLogCollapsed(false).SetLeftDrawerCollapsed(false);
        Assert.That(state, Is.EqualTo(C2ChromeCollapseState.Expanded));
    }

    [Test]
    public void Projection_Project_matches_record()
    {
        var projected = C2ChromeCollapseProjection.Project(
            messageLogCollapsed: false,
            leftDrawerCollapsed: true);
        Assert.That(projected.MessageLogCollapsed, Is.False);
        Assert.That(projected.LeftDrawerCollapsed, Is.True);
    }
}
