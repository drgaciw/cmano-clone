namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

/// <summary>UAT D24: no selected unit must not leave Fire enabled on a stale/primary unit.</summary>
[TestFixture]
public sealed class UnitDetailSelectionGateTests
{
    private static UnitDetailEntry Entry() => new(
        "u1", true, "OPERATIONAL", "MAG", "EMCON", "DOC", "FUEL", "ENGAGE", "ATTACK",
        new[]
        {
            new EngageAttackOptions.AttackOption("fire-single", "Fire single", true),
            new EngageAttackOptions.AttackOption("hold-fire", "Hold fire", true),
        });

    [Test]
    public void Apply_with_no_selection_disables_every_attack_option_with_NO_SELECTION()
    {
        var gated = UnitDetailSelectionGate.Apply(Entry(), null);
        Assert.That(gated!.AttackMenu, Has.Count.EqualTo(2));
        Assert.That(gated.AttackMenu, Has.All.Matches<EngageAttackOptions.AttackOption>(
            o => !o.Enabled && o.DisabledReason == UnitDetailSelectionGate.NoSelectionReason));
        var preview = AttackOptionsPreviewBinder.Bind(gated.AttackMenu);
        Assert.That(preview.Rows, Has.All.Matches<AttackOptionMenuRow>(
            r => !r.Enabled && r.AbortReason == "NO_SELECTION"));
    }

    [Test]
    public void Apply_with_selection_is_passthrough()
    {
        var entry = Entry();
        Assert.That(UnitDetailSelectionGate.Apply(entry, "u1"), Is.SameAs(entry));
        Assert.That(UnitDetailSelectionGate.Apply(null, null), Is.Null);
    }
}
