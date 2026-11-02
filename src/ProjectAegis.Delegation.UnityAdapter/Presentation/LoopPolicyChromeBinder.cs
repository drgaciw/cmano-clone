using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-251 / LOOP-05: readable loop-policy labels (fog model + personality edit policy) for the
/// Planning and Executing top bar / briefing chrome. Presentation-only (ADR-010 projection side):
/// pure function of the scenario policy, so labels change only when a scenario loads or its policy
/// is rebound. No wall-clock, no sim mutation.
/// </summary>
public static class LoopPolicyChromeBinder
{
    /// <summary>Bind both loop-policy labels from a scenario policy profile.</summary>
    public static LoopPolicyChromePresentation Bind(ScenarioPolicyProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        return Bind(profile.PlayerInfoModel, profile.PersonalityEditPolicy);
    }

    /// <summary>Bind both loop-policy labels from explicit policy values.</summary>
    public static LoopPolicyChromePresentation Bind(PlayerInfoModel playerInfoModel, PersonalityEditPolicy editPolicy) =>
        new(FogModeLabelBinder.Bind(playerInfoModel), BindEditPolicy(editPolicy));

    /// <summary>Bind the personality edit policy label.</summary>
    public static PersonalityEditPolicyPresentation BindEditPolicy(PersonalityEditPolicy policy) =>
        policy switch
        {
            PersonalityEditPolicy.Anytime => new("EDITS: ANYTIME", "c2-topbar-item--edit-anytime"),
            PersonalityEditPolicy.PlanningOnly => new("EDITS: PLANNING ONLY", "c2-topbar-item--edit-planning"),
            PersonalityEditPolicy.TieredRebrief => new("EDITS: TIERED REBRIEF", "c2-topbar-item--edit-tiered"),
            _ => new("EDITS: —", "c2-topbar-item--edit-unknown"),
        };
}

/// <summary>Applied personality edit policy label for briefing/HUD hosts.</summary>
public sealed record PersonalityEditPolicyPresentation(string Label, string CssClass);

/// <summary>Both loop-policy labels for top bar / briefing chrome.</summary>
public sealed record LoopPolicyChromePresentation(FogModePresentation Fog, PersonalityEditPolicyPresentation EditPolicy);
