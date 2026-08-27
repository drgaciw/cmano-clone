using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.EngageExplainContract;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.EngageExplainContract;

public sealed class EngageExplainContractProjectionTests
{
    private const string CanFireExplanationRef = "ENGAGE: CLEAR";

    [Test]
    public void Permitted_path_populates_why_permitted_from_authorized_combat_event()
    {
        var snapshot = new EngageExplainCombatEventSnapshot(new[]
        {
            CreateEvent(
                EngageExplainCombatEventPhase.IntentAccepted,
                outcome: "IntentAccepted",
                explanationRef: "engage-assess:intent-accepted",
                correlationId: 42,
                simTime: 1.0),
            CreateEvent(
                EngageExplainCombatEventPhase.Authorized,
                outcome: "Authorized",
                explanationRef: CanFireExplanationRef,
                correlationId: 42,
                simTime: 1.0),
            CreateEvent(
                EngageExplainCombatEventPhase.Firing,
                outcome: "Launch",
                explanationRef: "engage-assess:launch",
                correlationId: 42,
                simTime: 1.5),
            CreateEvent(
                EngageExplainCombatEventPhase.TerminalOutcome,
                outcome: EngagementOutcomeCodes.Kill,
                explanationRef: "outcome:Kill",
                correlationId: 42,
                simTime: 3.0),
        });

        var explain = EngageExplainContractProjection.ProjectFromSnapshot(snapshot);

        Assert.That(explain.WhyPermitted, Is.EqualTo(CanFireExplanationRef));
        Assert.That(explain.WhyWithheld, Is.Null);
        Assert.That(explain.WeaponFamilyId, Is.EqualTo(CatalogWeaponIds.MvpDefault));
        Assert.That(explain.CorrelationId, Is.EqualTo(42UL));
        Assert.That(explain.SimTime, Is.EqualTo(1.0));
    }

    [Test]
    public void Refused_path_populates_why_withheld_from_abort_combat_event_facts()
    {
        var snapshot = new EngageExplainCombatEventSnapshot(new[]
        {
            CreateEvent(
                EngageExplainCombatEventPhase.IntentAccepted,
                outcome: "IntentAccepted",
                explanationRef: "engage-assess:intent-accepted",
                correlationId: 7,
                simTime: 1.0),
            CreateEvent(
                EngageExplainCombatEventPhase.AuthorizationRefused,
                outcome: AbortReasonCatalog.Engage.DLZ_OUT,
                explanationRef: $"abort:{AbortReasonCatalog.Engage.DLZ_OUT}",
                correlationId: 7,
                simTime: 1.0),
        });

        var explain = EngageExplainContractProjection.ProjectFromSnapshot(snapshot);

        Assert.That(explain.WhyPermitted, Is.Null);
        Assert.That(explain.WhyWithheld, Is.EqualTo($"abort:{AbortReasonCatalog.Engage.DLZ_OUT}"));
        Assert.That(explain.WhyWithheld, Does.Contain(AbortReasonCatalog.Engage.DLZ_OUT));
        Assert.That(explain.WeaponFamilyId, Is.EqualTo(CatalogWeaponIds.MvpDefault));
        Assert.That(explain.CorrelationId, Is.EqualTo(7UL));
        Assert.That(explain.SimTime, Is.EqualTo(1.0));
    }

    [Test]
    public void Refused_path_populates_why_withheld_from_policy_combat_event_facts()
    {
        var refused = CreateEvent(
            EngageExplainCombatEventPhase.AuthorizationRefused,
            outcome: nameof(FireAbortReason.RoeHoldFire),
            explanationRef: $"policy:{FireAbortReason.RoeHoldFire}",
            correlationId: 9,
            simTime: 1.2);

        var explain = EngageExplainContractProjection.Project(refused);

        Assert.That(explain.WhyPermitted, Is.Null);
        Assert.That(explain.WhyWithheld, Is.EqualTo($"policy:{FireAbortReason.RoeHoldFire}"));
        Assert.That(explain.WhyWithheld, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Fingerprint_is_identical_for_identical_inputs()
    {
        var snapshot = new EngageExplainCombatEventSnapshot(new[]
        {
            CreateEvent(
                EngageExplainCombatEventPhase.IntentAccepted,
                outcome: "IntentAccepted",
                explanationRef: "engage-assess:intent-accepted",
                correlationId: 100,
                simTime: 4.5),
            CreateEvent(
                EngageExplainCombatEventPhase.Authorized,
                outcome: "Authorized",
                explanationRef: CanFireExplanationRef,
                correlationId: 100,
                simTime: 4.5),
        });

        var first = EngageExplainContractProjection.ProjectFromSnapshot(snapshot);
        var second = EngageExplainContractProjection.ProjectFromSnapshot(snapshot);

        Assert.That(
            EngageExplainContractFingerprint.Compute(first),
            Is.EqualTo(EngageExplainContractFingerprint.Compute(second)));
    }

    [Test]
    public void Dto_surface_omits_ui_derived_truth_fields()
    {
        var uiDerivedNames = new[]
        {
            "Selection",
            "Hover",
            "Camera",
            "Panel",
            "Visible",
            "Chrome",
            "IsSelected",
        };

        foreach (var type in new[]
                 {
                     typeof(EngageExplainContractDto),
                     typeof(EngageExplainCombatEventInput),
                     typeof(EngageExplainCombatEventSnapshot),
                 })
        {
            foreach (var prop in type.GetProperties())
            {
                foreach (var forbidden in uiDerivedNames)
                {
                    Assert.That(
                        prop.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                        Is.False,
                        $"{type.Name}.{prop.Name} must not encode UI-derived truth");
                }
            }
        }
    }

    private static EngageExplainCombatEventInput CreateEvent(
        EngageExplainCombatEventPhase phase,
        string outcome,
        string explanationRef,
        ulong correlationId,
        double simTime,
        ulong simTick = 1) =>
        new(
            phase,
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            Outcome: outcome,
            CorrelationId: correlationId,
            SimTime: simTime,
            SimTick: simTick,
            ExplanationRef: explanationRef);
}
