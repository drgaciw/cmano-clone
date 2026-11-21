using NUnit.Framework;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>UAT D26: unwired Scenario Editor actions are disabled with a NOT_AVAILABLE reason.</summary>
public sealed class ScenarioEditorActionAvailabilityTests
{
    [TestCase("Load")]
    [TestCase("Save")]
    [TestCase("Undo")]
    [TestCase("Redo")]
    [TestCase("Play")]
    [TestCase("Sample")]
    public void No_subscriber_disables_with_not_available_tooltip_even_when_state_enabled(string action)
    {
        var result = ScenarioEditorActionAvailability.Resolve(action, hasSubscriber: false, stateEnabled: true);

        Assert.That(result.Enabled, Is.False);
        Assert.That(result.Tooltip, Is.EqualTo($"NOT_AVAILABLE: {action} is not wired in this build"));
    }

    [Test]
    public void No_subscriber_overrides_existing_state_reason()
    {
        var result = ScenarioEditorActionAvailability.Resolve(
            "Play", hasSubscriber: false, stateEnabled: false, stateDisabledReason: "Fix errors");

        Assert.That(result.Enabled, Is.False);
        Assert.That(result.Tooltip, Does.StartWith("NOT_AVAILABLE:"));
    }

    [Test]
    public void Subscriber_and_state_enabled_is_enabled_without_tooltip()
    {
        var result = ScenarioEditorActionAvailability.Resolve("Load", hasSubscriber: true, stateEnabled: true);

        Assert.That(result.Enabled, Is.True);
        Assert.That(result.Tooltip, Is.Empty);
    }

    [Test]
    public void Subscriber_and_state_disabled_keeps_existing_reason()
    {
        var result = ScenarioEditorActionAvailability.Resolve(
            "Play", hasSubscriber: true, stateEnabled: false, stateDisabledReason: "Fix errors");

        Assert.That(result.Enabled, Is.False);
        Assert.That(result.Tooltip, Is.EqualTo("Fix errors"));
    }

    [Test]
    public void Subscriber_and_state_disabled_without_reason_has_empty_tooltip()
    {
        var result = ScenarioEditorActionAvailability.Resolve("Undo", hasSubscriber: true, stateEnabled: false);

        Assert.That(result.Enabled, Is.False);
        Assert.That(result.Tooltip, Is.Empty);
    }
}
