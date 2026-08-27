namespace ProjectAegis.Delegation.EngageExplainContract;

/// <summary>
/// DRG-215: maps combat-event facts into an engagement explanation contract for DRG-168.
/// Pure projection — no Tick hook, orders, or combat resolve.
/// </summary>
public static class EngageExplainContractProjection
{
    /// <summary>Projects one combat-event row when the caller already selected the decisive event.</summary>
    public static EngageExplainContractDto Project(EngageExplainCombatEventInput input) =>
        input.Phase switch
        {
            EngageExplainCombatEventPhase.Authorized => CreatePermitted(input),
            EngageExplainCombatEventPhase.AuthorizationRefused => CreateWithheld(input),
            _ => EngageExplainContractDto.Empty,
        };

    /// <summary>
    /// Projects the explanation for one engage-assess leg from an ordered combat-event snapshot.
    /// Never emits a silent authorization deny when an explicit refusal event is present.
    /// </summary>
    public static EngageExplainContractDto ProjectFromSnapshot(EngageExplainCombatEventSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Events.Count == 0)
        {
            return EngageExplainContractDto.Empty;
        }

        EngageExplainCombatEventInput? refused = null;
        EngageExplainCombatEventInput? authorized = null;
        for (var i = 0; i < snapshot.Events.Count; i++)
        {
            var evt = snapshot.Events[i];
            if (evt.Phase == EngageExplainCombatEventPhase.AuthorizationRefused)
            {
                refused = evt;
            }
            else if (evt.Phase == EngageExplainCombatEventPhase.Authorized)
            {
                authorized = evt;
            }
        }

        if (refused is not null)
        {
            return CreateWithheld(refused);
        }

        if (authorized is not null)
        {
            return CreatePermitted(authorized);
        }

        return EngageExplainContractDto.Empty;
    }

    private static EngageExplainContractDto CreatePermitted(EngageExplainCombatEventInput evt)
    {
        var whyPermitted = ResolveReason(evt.ExplanationRef, evt.Outcome);
        return new EngageExplainContractDto(
            WhyPermitted: whyPermitted,
            WhyWithheld: null,
            WeaponFamilyId: evt.WeaponFamilyId,
            CorrelationId: evt.CorrelationId,
            SimTime: evt.SimTime);
    }

    private static EngageExplainContractDto CreateWithheld(EngageExplainCombatEventInput evt)
    {
        var whyWithheld = ResolveReason(evt.ExplanationRef, evt.Outcome);
        return new EngageExplainContractDto(
            WhyPermitted: null,
            WhyWithheld: whyWithheld,
            WeaponFamilyId: evt.WeaponFamilyId,
            CorrelationId: evt.CorrelationId,
            SimTime: evt.SimTime);
    }

    private static string ResolveReason(string explanationRef, string outcome)
    {
        if (!string.IsNullOrWhiteSpace(explanationRef))
        {
            return explanationRef;
        }

        return string.IsNullOrWhiteSpace(outcome) ? "authorization:refused" : outcome;
    }
}
