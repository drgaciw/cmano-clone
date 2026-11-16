namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>Propose-lane authoring request against an open scenario pin.</summary>
public sealed record AuthoringProposeRequest(
    string Brief,
    string IdempotencyKey,
    int ExpectedEditVersion,
    int CurrentEditVersion,
    IReadOnlyList<string> AllowedUnitIds,
    IReadOnlyList<string> AllowedTargetIds,
    IReadOnlyList<string> SupportedMissionTypes,
    string DbRef = "baltic_patrol",
    IReadOnlyDictionary<string, IReadOnlyList<string>>? UnitCapabilities = null,
    IReadOnlyList<string>? PriorAcceptedIdempotencyKeys = null,
    bool IsRetry = false);
