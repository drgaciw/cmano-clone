namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using System;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// Headless dogfood for message-log presentation bridge (UCA-A4b / DRG-141).
/// Proves ProjectFrom / ProjectCombatMessages are projection-only over DecisionLog.
/// </summary>
[TestFixture]
public sealed class MessageLogBridgeTests
{
    [Test]
    public void Harness_messages_include_kill_and_magazine_categories()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        Assert.That(result.Messages.Any(m => m.Category == "KILL_CONFIRMED"), Is.True);
        Assert.That(result.Messages.Any(m => m.Category == "MAGAZINE"), Is.True);
    }

    [Test]
    public void Classify_scenario_messages_include_contact_category()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-classify", ticks: 6);
        Assert.That(result.Messages.Any(m => m.Category == "CONTACT"), Is.True);
    }

    [Test]
    public void ProjectFrom_empty_log_returns_empty_readonly_list()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var lines = MessageLogBridge.ProjectFrom(bridge.Orchestrator.DecisionLog);
        Assert.That(lines, Is.InstanceOf<IReadOnlyList<MessageLogLine>>());
        Assert.That(lines, Is.Empty);
    }

    [Test]
    public void ProjectFrom_matches_harness_message_projection()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var viaBridge = MessageLogBridge.ProjectFrom(result.DecisionLog);
        Assert.That(viaBridge.Count, Is.EqualTo(result.Messages.Count));
        Assert.That(
            viaBridge.Select(m => (m.Category, m.Text)).ToArray(),
            Is.EqualTo(result.Messages.Select(m => (m.Category, m.Text)).ToArray()));
    }

    [Test]
    public void ProjectCombatMessages_is_strict_subset_of_full_log()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var combat = MessageLogBridge.ProjectCombatMessages(result.DecisionLog);
        var full = MessageLogBridge.ProjectFrom(result.DecisionLog);
        Assert.That(combat.Count, Is.LessThanOrEqualTo(full.Count));
        Assert.That(combat.All(m =>
            m.Category is "KILL_CONFIRMED" or "INTERCEPT_SUCCESS" or "HIT" or "MISS" or "MAGAZINE"), Is.True);
        Assert.That(combat.Any(m => m.Category is "KILL_CONFIRMED" or "MAGAZINE"), Is.True);
    }

    [Test]
    public void ProjectFrom_null_log_throws()
    {
        Assert.Throws<ArgumentNullException>((Action)(() =>
            MessageLogBridge.ProjectFrom(null!)));
    }

    [Test]
    public void ProjectCombatMessages_null_log_throws()
    {
        Assert.Throws<ArgumentNullException>((Action)(() =>
            MessageLogBridge.ProjectCombatMessages(null!)));
    }
}
