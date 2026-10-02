using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Maps scenario <see cref="PlayerInfoModel"/> to briefing/HUD fog-mode labels (DRG-264).
/// Presentation-only — reads policy projection, never mutates sim state.
/// </summary>
public static class FogModeLabelBinder
{
    /// <summary>Bind fog-mode label text for top-bar / briefing chrome.</summary>
    public static FogModePresentation Bind(PlayerInfoModel model) =>
        new(ResolveLabel(model), ResolveCssClass(model));

    private static string ResolveLabel(PlayerInfoModel model) =>
        model switch
        {
            PlayerInfoModel.FullTransparency => "FOG: FULL PICTURE",
            PlayerInfoModel.DelegationFog => "FOG: DELEGATION",
            PlayerInfoModel.TieredByAutonomy => "FOG: TIERED",
            _ => "FOG: —",
        };

    private static string ResolveCssClass(PlayerInfoModel model) =>
        model switch
        {
            PlayerInfoModel.FullTransparency => "c2-topbar-item--fog-full",
            PlayerInfoModel.DelegationFog => "c2-topbar-item--fog-delegation",
            PlayerInfoModel.TieredByAutonomy => "c2-topbar-item--fog-tiered",
            _ => "c2-topbar-item--fog-unknown",
        };
}

/// <summary>Applied fog-mode label for briefing/HUD hosts.</summary>
public sealed record FogModePresentation(string Label, string CssClass);
