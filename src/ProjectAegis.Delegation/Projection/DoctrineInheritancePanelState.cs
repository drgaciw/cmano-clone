namespace ProjectAegis.Delegation.Projection;

/// <summary>UI-ready state for doctrine inheritance panel binding (req 13 P0).</summary>
public sealed record DoctrineInheritancePanelState(
    string UnitIdLine,
    string RoeLine,
    string SalvoLine,
    string SourceLine,
    string OverrideLine,
    bool CanOverride,
    IReadOnlyList<RoeLevelOption> RoeOptions);

/// <summary>Selectable ROE level for override dropdown.</summary>
public sealed record RoeLevelOption(string Label, string Value, bool IsCurrent);
