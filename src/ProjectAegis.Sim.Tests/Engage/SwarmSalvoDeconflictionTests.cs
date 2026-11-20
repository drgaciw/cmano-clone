using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class SwarmSalvoDeconflictionTests
{
    [Test]
    public void Allocate_keeps_one_shooter_per_target_deterministically()
    {
        var requests = new[]
        {
            new SwarmSalvoDeconfliction.Slot(2, 100, 1),
            new SwarmSalvoDeconfliction.Slot(1, 100, 1),
            new SwarmSalvoDeconfliction.Slot(3, 200, 1),
        };

        var allocated = SwarmSalvoDeconfliction.Allocate(requests);

        Assert.That(allocated.Count, Is.EqualTo(2));
        Assert.That((allocated[0].ShooterUnitId, allocated[0].TargetId), Is.EqualTo((1ul, 100ul)));
        Assert.That((allocated[1].ShooterUnitId, allocated[1].TargetId), Is.EqualTo((3ul, 200ul)));
    }

    /// <summary>Wave 2 adversarial: multi-way contention + reverse-input determinism (doc 14 ENG-06).</summary>
    [Test]
    public void Allocate_three_way_same_target_is_deterministic_under_reverse_input()
    {
        var requests = new[]
        {
            new SwarmSalvoDeconfliction.Slot(ShooterUnitId: 30, TargetId: 500, WeaponId: 9),
            new SwarmSalvoDeconfliction.Slot(ShooterUnitId: 10, TargetId: 500, WeaponId: 2),
            new SwarmSalvoDeconfliction.Slot(ShooterUnitId: 20, TargetId: 500, WeaponId: 1),
            new SwarmSalvoDeconfliction.Slot(ShooterUnitId: 5, TargetId: 700, WeaponId: 8),
            new SwarmSalvoDeconfliction.Slot(ShooterUnitId: 5, TargetId: 700, WeaponId: 3),
            new SwarmSalvoDeconfliction.Slot(ShooterUnitId: 99, TargetId: 800, WeaponId: 1),
        };

        var a = SwarmSalvoDeconfliction.Allocate(requests);
        var b = SwarmSalvoDeconfliction.Allocate(requests.Reverse().ToArray());

        Assert.That(a.Count, Is.EqualTo(3));
        Assert.That(a.Any(s => s is { ShooterUnitId: 10, TargetId: 500 }), Is.True);
        Assert.That(a.Any(s => s.TargetId == 500 && s.ShooterUnitId is 20 or 30), Is.False);
        Assert.That(a.Any(s => s is { ShooterUnitId: 5, TargetId: 700, WeaponId: 3 }), Is.True);
        Assert.That(a.Any(s => s is { TargetId: 700, WeaponId: 8 }), Is.False);
        Assert.That(a.Any(s => s is { ShooterUnitId: 99, TargetId: 800 }), Is.True);

        Assert.That(
            b.Select(s => (s.ShooterUnitId, s.TargetId, s.WeaponId)).ToArray(),
            Is.EqualTo(a.Select(s => (s.ShooterUnitId, s.TargetId, s.WeaponId)).ToArray()));
    }
}