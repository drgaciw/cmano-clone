namespace ProjectAegis.Delegation.Projection;

/// <summary>Display-only OOB panel rows (no Unity dependency).</summary>
public sealed record OobTreePanelState(IReadOnlyList<OobTreeDisplayRow> UnitRows);

public sealed record OobTreeDisplayRow(string UnitId, string DisplayLine, bool IsAlive, bool IsSelected, string StyleClass);