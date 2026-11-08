using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>S124-03 W2-C2-04: refuse/drop bark + loggable reason.</summary>
[TestFixture]
public sealed class CommitRefusalBarkBinderTests
{
    [Test]
    public void Refused_commit_produces_visible_bark_and_log_line_with_same_stable_code()
    {
        var bark = CommitRefusalBarkBinder.Refused("u1", "fire-salvo", "NO_AMMO", simTime: 42.5);

        Assert.That(bark.Kind, Is.EqualTo(CommitBarkKind.Refused));
        Assert.That(bark.ReasonCode, Is.EqualTo("NO_AMMO"));
        Assert.That(bark.UnitId, Is.EqualTo("u1"));
        Assert.That(bark.OptionId, Is.EqualTo("fire-salvo"));
        Assert.That(bark.BarkText, Is.EqualTo("u1 REFUSED fire-salvo: NO_AMMO"));
        Assert.That(bark.CueClass, Is.EqualTo(CommitRefusalBarkCueClasses.Refused));
        Assert.That(bark.IsFireOrder, Is.False);

        Assert.That(bark.LogLine.Category, Is.EqualTo(CommitRefusalBarkBinder.RefusedCategory));
        Assert.That(bark.LogLine.SimTime, Is.EqualTo(42.5));
        Assert.That(bark.LogLine.UnitId, Is.EqualTo("u1"));
        Assert.That(bark.LogLine.Text, Is.EqualTo("Commit refused for u1: NO_AMMO (fire-salvo)"));
        Assert.That(bark.LogLine.SequenceId, Is.EqualTo(0UL), "Presentation-local line: never claims an order-log sequence.");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Refused_without_facade_reason_uses_enqueue_rejected_code(string? failure)
    {
        var bark = CommitRefusalBarkBinder.Refused("u1", "fire-single", failure, simTime: 1);

        Assert.That(bark.ReasonCode, Is.EqualTo(CommitRefusalBarkBinder.EnqueueRejectedCode));
        Assert.That(bark.LogLine.Text, Does.Contain(CommitRefusalBarkBinder.EnqueueRejectedCode));
    }

    [Test]
    public void Refused_without_unit_omits_unit_but_keeps_reason()
    {
        var bark = CommitRefusalBarkBinder.Refused(null, "fire-single", "UNKNOWN_UNIT", simTime: 0);

        Assert.That(bark.UnitId, Is.Null);
        Assert.That(bark.BarkText, Is.EqualTo("REFUSED fire-single: UNKNOWN_UNIT"));
        Assert.That(bark.LogLine.Text, Is.EqualTo("Commit refused: UNKNOWN_UNIT (fire-single)"));
    }

    [Test]
    public void Refused_from_strip_uses_strip_primary_reason()
    {
        var strip = CommitConstraintStripState.Empty with
        {
            OptionId = "fire-salvo",
            PrimaryReasonCode = AbortReasonCatalog.Engage.DLZ_OUT,
        };

        var bark = CommitRefusalBarkBinder.Refused("u1", strip, simTime: 3);

        Assert.That(bark.ReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.DLZ_OUT));
        Assert.That(bark.OptionId, Is.EqualTo("fire-salvo"));
    }

    [Test]
    public void Dropped_engagement_abort_bark_matches_projected_message_log_row()
    {
        var entry = OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(7, 12.0, 120, new TargetId("u1"), 99, Launched: false, AbortReasonCode: "DLZ_OUT"),
            sequenceId: 7);

        var ok = CommitRefusalBarkBinder.TryDropped(entry, out var bark);
        var logRow = MessageLogProjection.Project(new[] { entry }).Single();

        Assert.That(ok, Is.True);
        Assert.That(bark!.Kind, Is.EqualTo(CommitBarkKind.Dropped));
        Assert.That(bark.ReasonCode, Is.EqualTo("DLZ_OUT"));
        Assert.That(bark.UnitId, Is.EqualTo("u1"));
        Assert.That(bark.BarkText, Is.EqualTo("u1 DROPPED: DLZ_OUT"));
        Assert.That(bark.CueClass, Is.EqualTo(CommitRefusalBarkCueClasses.Dropped));
        Assert.That(bark.LogLine, Is.EqualTo(logRow));
        Assert.That(bark.LogLine.Text, Does.Contain(bark.ReasonCode));
    }

    [Test]
    public void Dropped_policy_denial_uses_fire_abort_reason_name_matching_log_text()
    {
        var entry = OrderLogEntryFactories.FromPolicyDenial(
            new PolicyDenialRecord(3, 4.0, 40, new AgentId("a1"), new TargetId("u2"), 1, FireAbortReason.WeaponsTight, OrderKind.Engage),
            sequenceId: 3);

        var ok = CommitRefusalBarkBinder.TryDropped(entry, out var bark);

        Assert.That(ok, Is.True);
        Assert.That(bark!.ReasonCode, Is.EqualTo(nameof(FireAbortReason.WeaponsTight)));
        Assert.That(bark.UnitId, Is.EqualTo("u2"));
        Assert.That(bark.LogLine, Is.EqualTo(MessageLogProjection.Project(new[] { entry }).Single()));
        Assert.That(bark.LogLine.Text, Does.Contain(bark.ReasonCode));
    }

    [Test]
    public void Dropped_engagement_without_code_uses_unspecified_code()
    {
        var entry = OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(1, 1.0, 10, new TargetId("u1"), 5, Launched: false),
            sequenceId: 1);

        Assert.That(CommitRefusalBarkBinder.TryDropped(entry, out var bark), Is.True);
        Assert.That(bark!.ReasonCode, Is.EqualTo(CommitRefusalBarkBinder.UnspecifiedCode));
    }

    [Test]
    public void Launched_engagement_is_not_a_drop()
    {
        var entry = OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(1, 1.0, 10, new TargetId("u1"), 5, Launched: true),
            sequenceId: 1);

        Assert.That(CommitRefusalBarkBinder.TryDropped(entry, out var bark), Is.False);
        Assert.That(bark, Is.Null);
    }

    [Test]
    public void Project_dropped_keeps_log_order_and_filters_by_unit()
    {
        var entries = new[]
        {
            OrderLogEntryFactories.FromEngagement(
                new EngagementRecord(1, 1.0, 10, new TargetId("u1"), 1, Launched: false, AbortReasonCode: "NO_AMMO"), 1),
            OrderLogEntryFactories.FromEngagement(
                new EngagementRecord(2, 2.0, 20, new TargetId("u2"), 2, Launched: false, AbortReasonCode: "DLZ_OUT"), 2),
            OrderLogEntryFactories.FromEngagement(
                new EngagementRecord(3, 3.0, 30, new TargetId("u1"), 3, Launched: true), 3),
            OrderLogEntryFactories.FromPolicyDenial(
                new PolicyDenialRecord(4, 4.0, 40, new AgentId("a1"), new TargetId("u1"), 1, FireAbortReason.WraSalvo, OrderKind.Engage), 4),
        };

        var all = CommitRefusalBarkBinder.ProjectDropped(entries);
        var u1 = CommitRefusalBarkBinder.ProjectDropped(entries, unitId: "u1");

        Assert.That(all.Select(b => b.ReasonCode), Is.EqualTo(new[] { "NO_AMMO", "DLZ_OUT", "WraSalvo" }));
        Assert.That(u1.Select(b => b.LogLine.SequenceId), Is.EqualTo(new[] { 1UL, 4UL }));
    }

    [Test]
    public void Bind_row_targets_bark_element_with_cue_class()
    {
        var bark = CommitRefusalBarkBinder.Refused("u1", "fire-salvo", "NO_AMMO", simTime: 0);

        var row = CommitRefusalBarkBinder.BindRow(bark);

        Assert.That(row.ElementName, Is.EqualTo(CommitRefusalBarkBinder.BarkElementName));
        Assert.That(row.Text, Is.EqualTo(bark.BarkText));
        Assert.That(row.CueClass, Is.EqualTo(CommitRefusalBarkCueClasses.Refused));
    }
}
