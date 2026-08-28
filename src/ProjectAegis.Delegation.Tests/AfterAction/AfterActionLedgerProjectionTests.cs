using ProjectAegis.Delegation.AfterAction;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.AfterAction;

public sealed class AfterActionLedgerProjectionTests
{
    private static CombatEventRowConsume Terminal(
        string shooterId,
        string targetId,
        string weaponFamilyId,
        string outcome,
        ulong correlationId,
        double simTime,
        ulong simTick = 0,
        string explanationRef = "outcome:Kill") =>
        new(
            CombatEventPhaseConsume.TerminalOutcome,
            shooterId,
            targetId,
            weaponFamilyId,
            outcome,
            correlationId,
            simTime,
            simTick,
            explanationRef);

    private static CombatEventRowConsume InFlight(
        string shooterId,
        string targetId,
        ulong correlationId,
        double simTime) =>
        new(
            CombatEventPhaseConsume.InFlight,
            shooterId,
            targetId,
            "sm-2",
            "InFlight",
            correlationId,
            simTime,
            1,
            "engage-assess:in-flight");

    [Test]
    public void Project_maps_combat_event_fields_without_reconstruction()
    {
        var snapshot = new CombatEventSnapshotConsume(new[]
        {
            Terminal("u1", "hostile-1", "sm-2", "Kill", 42, 3.0, 5),
        });

        var ledger = AfterActionLedgerProjection.Project(snapshot);

        Assert.That(ledger.Entries, Has.Count.EqualTo(1));
        var entry = ledger.Entries[0];
        Assert.That(entry.ShooterId, Is.EqualTo("u1"));
        Assert.That(entry.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(entry.WeaponFamilyId, Is.EqualTo("sm-2"));
        Assert.That(entry.Outcome, Is.EqualTo("Kill"));
        Assert.That(entry.CorrelationId, Is.EqualTo(42UL));
        Assert.That(entry.SimTime, Is.EqualTo(3.0));
        Assert.That(entry.Phase, Is.EqualTo(CombatEventPhaseConsume.TerminalOutcome));
        Assert.That(entry.ExplanationRef, Is.EqualTo("outcome:Kill"));
    }

    [Test]
    public void Filter_by_target_returns_matching_rows_only()
    {
        var snapshot = new CombatEventSnapshotConsume(new[]
        {
            Terminal("u1", "hostile-1", "sm-2", "Kill", 1, 1.0),
            Terminal("u2", "hostile-2", "essm", "Miss", 2, 2.0),
            InFlight("u1", "hostile-1", 3, 1.5),
        });

        var ledger = AfterActionLedgerProjection.Project(snapshot);
        var filtered = AfterActionLedgerProjection.Filter(
            ledger,
            new AfterActionLedgerFilter(TargetId: "hostile-1"));

        Assert.That(filtered.Entries, Has.Count.EqualTo(2));
        Assert.That(filtered.Entries.Select(e => e.TargetId).Distinct(), Is.EqualTo(new[] { "hostile-1" }));
        Assert.That(filtered.Entries.Select(e => e.CorrelationId), Is.EqualTo(new ulong[] { 1, 3 }));
    }

    [Test]
    public void Filter_by_outcome_returns_matching_rows_only()
    {
        var snapshot = new CombatEventSnapshotConsume(new[]
        {
            Terminal("u1", "hostile-1", "sm-2", "Kill", 1, 1.0),
            Terminal("u2", "hostile-2", "essm", "Miss", 2, 2.0),
            Terminal("u3", "hostile-3", "sm-2", "Kill", 3, 3.0),
        });

        var ledger = AfterActionLedgerProjection.Project(snapshot);
        var filtered = AfterActionLedgerProjection.Filter(
            ledger,
            new AfterActionLedgerFilter(Outcome: "Kill"));

        Assert.That(filtered.Entries, Has.Count.EqualTo(2));
        Assert.That(filtered.Entries.All(e => e.Outcome == "Kill"), Is.True);
    }

    [Test]
    public void Filter_by_shooter_and_weapon_family()
    {
        var snapshot = new CombatEventSnapshotConsume(new[]
        {
            Terminal("u1", "hostile-1", "sm-2", "Kill", 1, 1.0),
            Terminal("u1", "hostile-2", "essm", "Hit", 2, 2.0),
            Terminal("u2", "hostile-1", "sm-2", "Miss", 3, 3.0),
        });

        var ledger = AfterActionLedgerProjection.Project(snapshot);
        var filtered = AfterActionLedgerProjection.Filter(
            ledger,
            new AfterActionLedgerFilter(ShooterId: "u1", WeaponFamilyId: "sm-2"));

        Assert.That(filtered.Entries, Has.Count.EqualTo(1));
        Assert.That(filtered.Entries[0].ShooterId, Is.EqualTo("u1"));
        Assert.That(filtered.Entries[0].WeaponFamilyId, Is.EqualTo("sm-2"));
        Assert.That(filtered.Entries[0].CorrelationId, Is.EqualTo(1UL));
        Assert.That(filtered.Entries[0].SimTime, Is.EqualTo(1.0));
    }

    [Test]
    public void Fingerprint_is_identical_for_identical_inputs()
    {
        var snapshot = new CombatEventSnapshotConsume(new[]
        {
            Terminal("u1", "hostile-1", "sm-2", "Kill", 100, 7.5, 8),
            Terminal("u2", "hostile-2", "essm", "Miss", 101, 8.0, 9),
        });

        var first = AfterActionLedgerProjection.Project(snapshot);
        var second = AfterActionLedgerProjection.Project(snapshot);

        Assert.That(
            AfterActionLedgerProjection.ComputeFingerprint(first),
            Is.EqualTo(AfterActionLedgerProjection.ComputeFingerprint(second)));
        Assert.That(AfterActionLedgerProjection.ComputeFingerprint(first), Does.StartWith("aal:e=2|"));
    }

    [Test]
    public void Empty_snapshot_yields_empty_ledger_and_fingerprint()
    {
        var ledger = AfterActionLedgerProjection.Project(CombatEventSnapshotConsume.Empty);
        Assert.That(ledger.Entries, Is.Empty);
        Assert.That(AfterActionLedgerProjection.ComputeFingerprint(ledger), Is.EqualTo("aal:empty"));
    }

    [Test]
    public void Ledger_entry_surface_omits_authorization_and_order_fields()
    {
        var forbidden = new[]
        {
            "IsFireOrder",
            "IsWeaponsReleaseAuthorization",
            "IsAutomaticEngagement",
            "Enqueue",
            "Authorize",
            "Selection",
            "Hover",
            "Camera",
            "Panel",
        };

        foreach (var prop in typeof(AfterActionLedgerEntry).GetProperties())
        {
            foreach (var name in forbidden)
            {
                Assert.That(
                    prop.Name.Contains(name, StringComparison.OrdinalIgnoreCase),
                    Is.False,
                    $"{nameof(AfterActionLedgerEntry)}.{prop.Name} must not encode fire/authorize/UI truth");
            }
        }
    }
}
