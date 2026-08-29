using ProjectAegis.Delegation.MissionIntent;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.MissionIntent;

public sealed class MissionIntentProjectionTests
{
    private static MissionIntentInput CompleteHoldIntent() =>
        new(
            GroupId: "grp-patrol-alpha",
            UnitId: string.Empty,
            IntentCode: MissionIntentCode.Hold,
            ActiveConstraints: Array.Empty<string>(),
            AdvisoryRetask: MissionIntentRetaskAdvice.None);

    [Test]
    public void Complete_hold_intent_reports_stable_fingerprint()
    {
        var input = CompleteHoldIntent();
        var snapshot = MissionIntentProjection.Project(input);

        Assert.That(snapshot.Kind, Is.EqualTo(MissionIntentKind.AdvisoryIntent));
        Assert.That(snapshot.IntentCode, Is.EqualTo(MissionIntentCode.Hold));
        Assert.That(snapshot.GroupId, Is.EqualTo("grp-patrol-alpha"));
        Assert.That(snapshot.UnitId, Is.Empty);
        Assert.That(snapshot.Constraints, Is.Empty);
        Assert.That(snapshot.AdvisoryRetask, Is.EqualTo(MissionIntentRetaskAdvice.None));
        Assert.That(snapshot.IsOrder, Is.False);
        Assert.That(snapshot.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.IsAutomaticEngagement, Is.False);
        Assert.That(snapshot.StatusLine, Does.Contain("HOLD").IgnoreCase);
        Assert.That(snapshot.StatusLine, Does.Contain("no orders").IgnoreCase);

        var rerun = MissionIntentProjection.Project(input);
        Assert.That(
            MissionIntentProjection.ComputeFingerprint(snapshot),
            Is.EqualTo(MissionIntentProjection.ComputeFingerprint(rerun)));
    }

    [Test]
    public void Positive_attack_intent_on_unit_reports_stable_fingerprint()
    {
        var input = new MissionIntentInput(
            GroupId: string.Empty,
            UnitId: "u-2001",
            IntentCode: MissionIntentCode.Attack,
            ActiveConstraints: Array.Empty<string>(),
            AdvisoryRetask: MissionIntentRetaskAdvice.None);

        var snapshot = MissionIntentProjection.Project(input);

        Assert.That(snapshot.IntentCode, Is.EqualTo(MissionIntentCode.Attack));
        Assert.That(snapshot.UnitId, Is.EqualTo("u-2001"));
        Assert.That(snapshot.IsOrder, Is.False);
        Assert.That(snapshot.StatusLine, Does.Contain("ATTACK").IgnoreCase);
        Assert.That(snapshot.StatusLine, Does.Contain("no orders").IgnoreCase);

        var rerun = MissionIntentProjection.Project(input);
        Assert.That(
            MissionIntentProjection.ComputeFingerprint(snapshot),
            Is.EqualTo(MissionIntentProjection.ComputeFingerprint(rerun)));
    }

    [Test]
    public void Constrained_withdraw_reports_constraints_and_advisory_retask_without_order()
    {
        var input = new MissionIntentInput(
            GroupId: "grp-strike-bravo",
            UnitId: "u-3102",
            IntentCode: MissionIntentCode.Hold,
            ActiveConstraints: new[]
            {
                MissionIntentConstraintCode.RoeWithhold,
                MissionIntentConstraintCode.Hold,
                MissionIntentConstraintCode.NoStrike,
            },
            AdvisoryRetask: MissionIntentRetaskAdvice.Withdraw);

        var snapshot = MissionIntentProjection.Project(input);

        Assert.That(snapshot.Constraints, Is.EqualTo(new[]
        {
            MissionIntentConstraintCode.Hold,
            MissionIntentConstraintCode.NoStrike,
            MissionIntentConstraintCode.RoeWithhold,
        }));
        Assert.That(snapshot.AdvisoryRetask, Is.EqualTo(MissionIntentRetaskAdvice.Withdraw));
        Assert.That(snapshot.IsOrder, Is.False);
        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.StatusLine, Does.Contain("WITHDRAW").IgnoreCase);
        Assert.That(snapshot.StatusLine, Does.Contain("no orders").IgnoreCase);
        Assert.That(snapshot.StatusLine, Does.Contain(MissionIntentConstraintCode.Hold));
        Assert.That(snapshot.StatusLine, Does.Contain(MissionIntentConstraintCode.NoStrike));
        Assert.That(snapshot.StatusLine, Does.Contain(MissionIntentConstraintCode.RoeWithhold));
    }

    [Test]
    public void Empty_input_returns_empty_snapshot()
    {
        var nullSnapshot = MissionIntentProjection.Project(null);
        Assert.That(nullSnapshot.GroupId, Is.Empty);
        Assert.That(nullSnapshot.UnitId, Is.Empty);
        Assert.That(nullSnapshot.Constraints, Is.Empty);
        Assert.That(nullSnapshot.IsOrder, Is.False);
        Assert.That(MissionIntentProjection.ComputeFingerprint(nullSnapshot), Is.EqualTo("mi:empty"));

        var emptyScope = MissionIntentProjection.Project(
            new MissionIntentInput(
                GroupId: string.Empty,
                UnitId: string.Empty,
                IntentCode: MissionIntentCode.Hold,
                ActiveConstraints: Array.Empty<string>()));

        Assert.That(emptyScope.GroupId, Is.Empty);
        Assert.That(emptyScope.UnitId, Is.Empty);
        Assert.That(MissionIntentProjection.ComputeFingerprint(emptyScope), Is.EqualTo("mi:empty"));
    }
}
