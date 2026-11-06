namespace ProjectAegis.Delegation.PlatformDegrade;

/// <summary>Read-only facts for headless own-unit platform degrade (projection input).</summary>
public sealed record PlatformDegradeInput(
    ulong SimTick,
    IReadOnlyList<PlatformDegradeUnitInput>? Units);
