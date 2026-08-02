using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Input;

[TestFixture]
public sealed class AgentDirectiveIssuanceTests
{
    [TestCase("take_control", AgentDirectiveAction.TakeControl, true, null)]
    [TestCase("return_to_agent", AgentDirectiveAction.ReturnToAgent, true, null)]
    [TestCase("hold", AgentDirectiveAction.Hold, false, OrderKind.Hold)]
    [TestCase("rtb", AgentDirectiveAction.Rtb, false, OrderKind.ReturnToBase)]
    public void TryResolve_maps_known_directive_ids(
        string directiveId,
        AgentDirectiveAction expectedAction,
        bool expectedModeChange,
        OrderKind? expectedOrder)
    {
        Assert.That(AgentDirectiveIssuance.TryResolve(directiveId, out var request, out var reason), Is.True);
        Assert.That(reason, Is.Null);
        Assert.That(request.DirectiveId, Is.EqualTo(directiveId));
        Assert.That(request.Action, Is.EqualTo(expectedAction));
        Assert.That(request.IsModeChange, Is.EqualTo(expectedModeChange));
        Assert.That(request.OrderKind, Is.EqualTo(expectedOrder));
    }

    [TestCase("TAKE_CONTROL", "take_control")]
    [TestCase(" Return_To_Agent ", "return_to_agent")]
    [TestCase("HOLD", "hold")]
    [TestCase("RTB", "rtb")]
    public void TryResolve_is_case_and_whitespace_insensitive(string input, string normalizedId)
    {
        Assert.That(AgentDirectiveIssuance.TryResolve(input, out var request, out _), Is.True);
        Assert.That(request.DirectiveId, Is.EqualTo(normalizedId));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("warp")]
    [TestCase("engage")]
    [TestCase("fire-single")]
    public void TryResolve_rejects_unknown_with_UNKNOWN_DIRECTIVE(string? directiveId)
    {
        Assert.That(AgentDirectiveIssuance.TryResolve(directiveId, out _, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo(AgentDirectiveIssuance.ReasonUnknownDirective));
    }

    [Test]
    public void Validate_rejects_empty_selection_with_NO_SELECTION()
    {
        var result = AgentDirectiveIssuance.Validate("take_control", hasSelection: false, hasAgent: true);
        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(AgentDirectiveIssuance.ReasonNoSelection));
        Assert.That(result.Request, Is.Null);
    }

    [Test]
    public void Validate_rejects_mode_change_without_agent_with_NO_AGENT()
    {
        var take = AgentDirectiveIssuance.Validate("take_control", hasSelection: true, hasAgent: false);
        Assert.That(take.Ok, Is.False);
        Assert.That(take.FailureReason, Is.EqualTo(AgentDirectiveIssuance.ReasonNoAgent));

        var ret = AgentDirectiveIssuance.Validate("return_to_agent", hasSelection: true, hasAgent: false);
        Assert.That(ret.Ok, Is.False);
        Assert.That(ret.FailureReason, Is.EqualTo(AgentDirectiveIssuance.ReasonNoAgent));
    }

    [Test]
    public void Validate_allows_hold_and_rtb_without_agent()
    {
        var hold = AgentDirectiveIssuance.Validate("hold", hasSelection: true, hasAgent: false);
        Assert.That(hold.Ok, Is.True);
        Assert.That(hold.Request!.OrderKind, Is.EqualTo(OrderKind.Hold));
        Assert.That(hold.Request.IsModeChange, Is.False);

        var rtb = AgentDirectiveIssuance.Validate("rtb", hasSelection: true, hasAgent: false);
        Assert.That(rtb.Ok, Is.True);
        Assert.That(rtb.Request!.OrderKind, Is.EqualTo(OrderKind.ReturnToBase));
    }

    [Test]
    public void Validate_rejects_unknown_even_with_selection_and_agent()
    {
        var result = AgentDirectiveIssuance.Validate("nope", hasSelection: true, hasAgent: true);
        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(AgentDirectiveIssuance.ReasonUnknownDirective));
        Assert.That(result.Request, Is.Null);
    }

    [Test]
    public void Validate_ok_returns_mode_change_request_for_take_control()
    {
        var result = AgentDirectiveIssuance.Validate("take_control", hasSelection: true, hasAgent: true);
        Assert.That(result.Ok, Is.True);
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(result.Request, Is.Not.Null);
        Assert.That(result.Request!.IsModeChange, Is.True);
        Assert.That(result.Request.Action, Is.EqualTo(AgentDirectiveAction.TakeControl));
        Assert.That(result.Request.OrderKind, Is.Null);
    }

    [Test]
    public void Validate_distinguishes_active_and_suspended_agent_mode_changes()
    {
        var take = AgentDirectiveIssuance.Validate(
            "take_control", hasSelection: true, hasActiveAgent: false, hasSuspendedAgent: true);
        Assert.That(take.Ok, Is.False);
        Assert.That(take.FailureReason, Is.EqualTo(AgentDirectiveIssuance.ReasonNoActiveAgent));

        var ret = AgentDirectiveIssuance.Validate(
            "return_to_agent", hasSelection: true, hasActiveAgent: true, hasSuspendedAgent: false);
        Assert.That(ret.Ok, Is.False);
        Assert.That(ret.FailureReason, Is.EqualTo(AgentDirectiveIssuance.ReasonNoSuspendedAgent));
    }

    [Test]
    public void hold_and_rtb_reuse_existing_OrderKinds_without_new_enums()
    {
        Assert.That(AgentDirectiveIssuance.TryResolve("hold", out var hold, out _), Is.True);
        Assert.That(hold.OrderKind, Is.EqualTo(OrderKind.Hold));
        Assert.That((int)OrderKind.Hold, Is.EqualTo(1));

        Assert.That(AgentDirectiveIssuance.TryResolve("rtb", out var rtb, out _), Is.True);
        Assert.That(rtb.OrderKind, Is.EqualTo(OrderKind.ReturnToBase));
        Assert.That((int)OrderKind.ReturnToBase, Is.EqualTo(4));
    }
}
