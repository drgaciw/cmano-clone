namespace ProjectAegis.Delegation.Projection;

public sealed record C2TopBarState(
    string SimTimeLabel,
    string PhaseLabel,
    string CompressionLabel,
    string ModeLabel,
    string CommsLabel,
    string ScoreLabel,
    string ZuluTimeLabel = "",
    string LocalTimeLabel = "",
    string RemainingDurationLabel = "");
