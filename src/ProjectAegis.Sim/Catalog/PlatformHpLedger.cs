namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;

/// <summary>Deterministic platform HP% ledger for bounded catalog hot-tick damage (ADR-009).</summary>
public sealed class PlatformHpLedger
{
    private readonly Dictionary<string, double> _hpPctByPlatformId;

    public PlatformHpLedger(IReadOnlyDictionary<string, double> seedHpPctByPlatformId)
    {
        _hpPctByPlatformId = new Dictionary<string, double>(seedHpPctByPlatformId, StringComparer.Ordinal);
    }

    public static PlatformHpLedger SeedFromWithdrawTargets(
        IReadOnlyList<ScenarioCatalogWithdrawTarget> targets)
    {
        var seed = targets
            .OrderBy(t => t.PlatformId, StringComparer.Ordinal)
            .ToDictionary(
                t => t.PlatformId,
                t => Math.Clamp(t.CurrentHpPct, 0.0, 100.0),
                StringComparer.Ordinal);
        return new PlatformHpLedger(seed);
    }

    public bool TryGetHpPct(string platformId, out double hpPct) =>
        _hpPctByPlatformId.TryGetValue(platformId, out hpPct);

    public void SetHpPct(string platformId, double hpPct) =>
        _hpPctByPlatformId[platformId] = Math.Clamp(hpPct, 0.0, 100.0);

    public IReadOnlyList<string> GetSortedPlatformIds() =>
        _hpPctByPlatformId.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();

    public ulong ComputeWorldHashMix()
    {
        ulong mix = 0;
        foreach (var platformId in GetSortedPlatformIds())
        {
            if (!_hpPctByPlatformId.TryGetValue(platformId, out var hpPct))
            {
                continue;
            }

            var hpBits = unchecked((ulong)BitConverter.ToInt64(BitConverter.GetBytes(hpPct), 0));
            mix = SimWorldHash.MixLayer(mix, SimWorldHash.Fold(hpBits ^ (ulong)platformId.Length), SimWorldHash.LayerCombatOutcome);
        }

        return mix;
    }
}