namespace ProjectAegis.Delegation.Tests.Input;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using NUnit.Framework;

[TestFixture]
public sealed class C2CommandIssuanceTests
{
    [TestCase("hold", OrderKind.Hold)]
    [TestCase("rtb", OrderKind.ReturnToBase)]
    [TestCase("move", OrderKind.Move)]
    [TestCase("plot_course", OrderKind.Move)]
    [TestCase("engage", OrderKind.Engage)]
    [TestCase("set_emcon", OrderKind.SetEmcon)]
    [TestCase("set_sensors", OrderKind.SetSensors)]
    public void TryResolve_maps_known_command_ids(string commandId, OrderKind expected)
    {
        Assert.That(C2CommandIssuance.TryResolve(commandId, out var kind, out var reason), Is.True);
        Assert.That(kind, Is.EqualTo(expected));
        Assert.That(reason, Is.Null);
    }

    [TestCase("HOLD", OrderKind.Hold)]
    [TestCase(" Plot_Course ", OrderKind.Move)]
    [TestCase("SET_EMCON", OrderKind.SetEmcon)]
    public void TryResolve_is_case_and_whitespace_insensitive(string commandId, OrderKind expected)
    {
        Assert.That(C2CommandIssuance.TryResolve(commandId, out var kind, out _), Is.True);
        Assert.That(kind, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("warp")]
    [TestCase("fire-single")]
    public void TryResolve_rejects_unknown_with_UNKNOWN_COMMAND(string? commandId)
    {
        Assert.That(C2CommandIssuance.TryResolve(commandId, out _, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo(C2CommandIssuance.ReasonUnknownCommand));
    }

    [Test]
    public void Validate_rejects_empty_selection_with_NO_SELECTION()
    {
        var result = C2CommandIssuance.Validate("hold", hasSelection: false);
        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(C2CommandIssuance.ReasonNoSelection));
        Assert.That(result.Kind, Is.Null);
    }

    [Test]
    public void Validate_rejects_unknown_even_with_selection()
    {
        var result = C2CommandIssuance.Validate("nope", hasSelection: true);
        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(C2CommandIssuance.ReasonUnknownCommand));
        Assert.That(result.Kind, Is.Null);
    }

    [Test]
    public void Validate_ok_returns_resolved_kind()
    {
        var result = C2CommandIssuance.Validate("set_sensors", hasSelection: true);
        Assert.That(result.Ok, Is.True);
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(result.Kind, Is.EqualTo(OrderKind.SetSensors));
    }

    [Test]
    public void OrderKind_append_preserves_existing_ordinals()
    {
        Assert.That((int)OrderKind.Move, Is.EqualTo(0));
        Assert.That((int)OrderKind.Hold, Is.EqualTo(1));
        Assert.That((int)OrderKind.Engage, Is.EqualTo(2));
        Assert.That((int)OrderKind.SetEwPosture, Is.EqualTo(3));
        Assert.That((int)OrderKind.ReturnToBase, Is.EqualTo(4));
        Assert.That((int)OrderKind.SetEmcon, Is.EqualTo(5));
        Assert.That((int)OrderKind.SetSensors, Is.EqualTo(6));
    }
}
