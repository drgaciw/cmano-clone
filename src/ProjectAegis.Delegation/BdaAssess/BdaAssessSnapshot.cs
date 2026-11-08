namespace ProjectAegis.Delegation.BdaAssess;

/// <summary>Ordered, replay-stable BDA assess picture keyed by contact id.</summary>
public sealed record BdaAssessSnapshot(IReadOnlyList<BdaAssessContactState> Contacts)
{
  public static BdaAssessSnapshot Empty { get; } =
    new(Array.Empty<BdaAssessContactState>());
}
