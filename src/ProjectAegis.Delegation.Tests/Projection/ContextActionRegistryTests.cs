using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0 Phase 0: context-action registry contracts (T2 shell seam).</summary>
[TestFixture]
public sealed class ContextActionRegistryTests
{
    [Test]
    public void Register_and_Eligible_returns_matching_actions_in_order()
    {
        var registry = new ContextActionRegistry();
        registry.Register(new StubAction("a", eligible: true));
        registry.Register(new StubAction("b", eligible: false));
        registry.Register(new StubAction("c", eligible: true));

        var eligible = registry.Eligible(new ContextActionEvaluationContext(["u1"], isMapSurface: false, isReplay: false));

        Assert.That(eligible.Select(p => p.ActionId).ToArray(), Is.EqualTo(new[] { "a", "c" }));
    }

    [Test]
    public void Eligible_empty_when_replay()
    {
        var registry = new ContextActionRegistry();
        registry.Register(new StubAction("a", eligible: true));

        var eligible = registry.Eligible(new ContextActionEvaluationContext(["u1"], isMapSurface: true, isReplay: true));

        Assert.That(eligible, Is.Empty);
    }

    [Test]
    public void Register_duplicate_ActionId_throws()
    {
        var registry = new ContextActionRegistry();
        registry.Register(new StubAction("dup", eligible: true));
        TestDelegate act = () => registry.Register(new StubAction("dup", eligible: true));
        Assert.Throws<InvalidOperationException>(act);
    }

    [Test]
    public void CreateIntent_returns_payload_for_selection()
    {
        var registry = new ContextActionRegistry();
        registry.Register(new StubAction("plot_course", eligible: true));
        var ctx = new ContextActionEvaluationContext(["u1", "u2"], isMapSurface: true, isReplay: false, primaryUnitId: "u1");
        Assert.That(registry.Eligible(ctx), Has.Count.EqualTo(1));
        var action = registry.Eligible(ctx)[0];
        var intent = action.CreateIntent(ctx);
        Assert.That(intent, Is.Not.Null);
        Assert.That(intent!.ActionId, Is.EqualTo("plot_course"));
        Assert.That(intent.TargetUnitIds.ToArray(), Is.EqualTo(new[] { "u1", "u2" }));
    }

    private sealed class StubAction : IContextActionProvider
    {
        private readonly bool _eligible;

        public StubAction(string id, bool eligible)
        {
            ActionId = id;
            _eligible = eligible;
        }

        public string ActionId { get; }
        public string Label => ActionId;
        public bool IsEligible(ContextActionEvaluationContext context) => _eligible;
        public ContextActionIntent? CreateIntent(ContextActionEvaluationContext context) =>
            new(ActionId, IntentKind: "stub", TargetUnitIds: context.SelectedUnitIds);
    }
}

