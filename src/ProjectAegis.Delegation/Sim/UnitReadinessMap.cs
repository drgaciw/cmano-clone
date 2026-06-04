namespace ProjectAegis.Delegation.Sim;

/// <summary>Runtime launch readiness by unit id (req 16 → engage world).</summary>
public sealed class UnitReadinessMap
{
    private readonly Dictionary<string, bool> _readyByUnitId;

    public UnitReadinessMap(IReadOnlyDictionary<string, bool> readyByUnitId)
    {
        _readyByUnitId = new Dictionary<string, bool>(readyByUnitId, StringComparer.Ordinal);
    }

    public bool IsReadyForLaunch(string unitId) =>
        !_readyByUnitId.TryGetValue(unitId, out var ready) || ready;
}