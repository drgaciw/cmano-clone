using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class WeaponsReleaseConfirmationGateTests
{
    [Test]
    public void Confirm_emits_when_positive_control_required()
    {
        Assert.That(
            WeaponsReleaseConfirmationGate.ShouldEmit(
                positiveControlRequired: true,
                WeaponsReleaseConfirmationGate.GateAction.Confirm),
            Is.True);
    }

    [Test]
    public void Cancel_emits_no_intent_when_positive_control_required()
    {
        Assert.That(
            WeaponsReleaseConfirmationGate.ShouldEmit(
                positiveControlRequired: true,
                WeaponsReleaseConfirmationGate.GateAction.Cancel),
            Is.False);
    }

    [Test]
    public void Confirm_emits_when_positive_control_not_required()
    {
        Assert.That(
            WeaponsReleaseConfirmationGate.ShouldEmit(
                positiveControlRequired: false,
                WeaponsReleaseConfirmationGate.GateAction.Confirm),
            Is.True);
    }

    [Test]
    public void Cancel_still_emits_when_gate_does_not_apply()
    {
        // The gate is a no-op when positive control isn't required — cancel does not block
        // an otherwise-ungated intent.
        Assert.That(
            WeaponsReleaseConfirmationGate.ShouldEmit(
                positiveControlRequired: false,
                WeaponsReleaseConfirmationGate.GateAction.Cancel),
            Is.True);
    }
}
