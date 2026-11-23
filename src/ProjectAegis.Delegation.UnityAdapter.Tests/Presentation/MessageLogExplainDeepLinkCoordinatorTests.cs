using NUnit.Framework;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MessageLogExplainDeepLinkCoordinatorTests
{
    [Test]
    public void Resolve_selects_visible_friendly_unit_and_reuses_EngageExplain_dto()
    {
        var detail = new DecisionLogDeepLinkDetail(
            Found: true,
            SequenceId: 42,
            SimTick: 12,
            SimTime: 12.5,
            EntryKind: OrderLogEntryKind.PolicyDenial,
            Category: "ENGAGE",
            SourceKind: "LiveOrderLogView",
            ActorId: "u1",
            AgentId: "agent-1",
            UnitId: "u1",
            Summary: "Refused under WeaponsTight",
            Rationale: "ROE_HOLD_FIRE",
            AutonomyLevel: "Assisted",
            AttentionLoad: 0.4,
            AttentionBudget: 1.0,
            RngDraw: 0.25,
            PolicySnapshotId: 7,
            ScoredAlternatives: Array.Empty<string>(),
            Fields: new[] { new KeyValuePair<string, string>("TargetId", "hostile-1") },
            UnavailableReason: null);

        var result = MessageLogExplainDeepLinkCoordinator.Resolve(
            detail,
            playerVisibleFriendlyIds: new[] { "u1", "u2" },
            playerVisibleContactIds: new[] { "hostile-1" });

        Assert.That(result.HasExplain, Is.True);
        Assert.That(result.SuppressedByFog, Is.False);
        Assert.That(result.SafeUnitIdToSelect, Is.EqualTo("u1"));
        Assert.That(result.SafeContactIdToSelect, Is.EqualTo("hostile-1"));
        Assert.That(result.Explain, Is.TypeOf<EngageExplain>());
        Assert.That(result.Explain.StatusLine, Does.Contain("SEQ #42"));
        Assert.That(result.Explain.ReasonPlain, Does.Contain("ROE_HOLD_FIRE"));
    }

    [Test]
    public void Resolve_suppresses_unobserved_hostile_unit_under_fog()
    {
        var detail = new DecisionLogDeepLinkDetail(
            Found: true,
            SequenceId: 99,
            SimTick: 30,
            SimTime: 30.0,
            EntryKind: OrderLogEntryKind.AgentDecision,
            Category: "CONTACT",
            SourceKind: "LiveOrderLogView",
            ActorId: "enemy-hidden-sub",
            AgentId: "enemy-agent",
            UnitId: "enemy-hidden-sub",
            Summary: "Hidden enemy motion",
            Rationale: null,
            AutonomyLevel: null,
            AttentionLoad: null,
            AttentionBudget: null,
            RngDraw: null,
            PolicySnapshotId: null,
            ScoredAlternatives: Array.Empty<string>(),
            Fields: Array.Empty<KeyValuePair<string, string>>(),
            UnavailableReason: null);

        var result = MessageLogExplainDeepLinkCoordinator.Resolve(
            detail,
            playerVisibleFriendlyIds: new[] { "u1" },
            playerVisibleContactIds: Array.Empty<string>());

        Assert.That(result.SuppressedByFog, Is.True);
        Assert.That(result.SafeUnitIdToSelect, Is.Null);
        Assert.That(result.SafeContactIdToSelect, Is.Null);
        Assert.That(result.Explain.StatusLine, Does.Contain("FOG"));
    }
}
