namespace ProjectAegis.Sim.Engage;

/// <summary>
/// Deterministic swarm salvo target allocation (req 14): one shooter per target per weapon slot
/// by sorted (shooterId, targetId, weaponId).
/// </summary>
public static class SwarmSalvoDeconfliction
{
    public readonly record struct Slot(ulong ShooterUnitId, ulong TargetId, ulong WeaponId = 0);

    public static IReadOnlyList<Slot> Allocate(IReadOnlyList<Slot> requests)
    {
        if (requests.Count <= 1)
        {
            return requests;
        }

        var ordered = requests
            .OrderBy(r => r.ShooterUnitId)
            .ThenBy(r => r.TargetId)
            .ThenBy(r => r.WeaponId)
            .ToArray();

        var claimedTargets = new HashSet<ulong>();
        var accepted = new List<Slot>(ordered.Length);
        foreach (var slot in ordered)
        {
            if (claimedTargets.Add(slot.TargetId))
            {
                accepted.Add(slot);
            }
        }

        return accepted;
    }
}