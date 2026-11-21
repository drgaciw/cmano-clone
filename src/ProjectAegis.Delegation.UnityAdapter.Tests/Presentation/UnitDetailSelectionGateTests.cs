namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using Projection;
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
        var menu = UnitDetailSelectionGate.Apply(Entry(), null)!.AttackMenu;
        Assert.That(menu, Has.Count.EqualTo(2));
        Assert.That(menu, Has.All.Matches<EngageAttackOptions.AttackOption>(
            o => o is { Enabled: false, DisabledReason: UnitDetailSelectionGate.NoSelectionReason }));
        var preview = AttackOptionsPreviewBinder.Bind(menu);
        Assert.That(preview.Rows, Has.All.Matches<AttackOptionMenuRow>(
            r => r is { Enabled: false, AbortReason: "NO_SELECTION" }));
    }

    [Test]
    public void Apply_with_selection_is_passthrough()
    {
        var entry = Entry();
        Assert.That(UnitDetailSelectionGate.Apply(entry, "u1"), Is.SameAs(entry));
        Assert.That(UnitDetailSelectionGate.Apply(null, null), Is.Null);
    }
}
