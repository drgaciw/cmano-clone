namespace ProjectAegis.Delegation.Skills;

/// <summary>
/// Discoverable Slice A skills (AGC-01). Hosts load this catalog, not a free-form prompt dump.
/// Submit is a host verb (<see cref="SkillIds.Submit"/>), not a catalog row.
/// </summary>
public static class SkillCatalog
{
    public const string SubmitVerbId = SkillIds.Submit;

    public static IReadOnlyList<SkillDescriptor> SliceA { get; } =
    [
        new(
            SkillIds.TrackAssess,
            "Track assessment",
            [SkillLane.Read, SkillLane.Propose],
            ["hold", "set_sensors", "set_emcon"]),
        new(
            SkillIds.DatalinkReason,
            "Data-link reasoning",
            [SkillLane.Read, SkillLane.Propose],
            ["set_emcon", "set_sensors", "hold"]),
        new(
            SkillIds.PairingRecommend,
            "Sensor-to-shooter pairing",
            [SkillLane.Read, SkillLane.Propose],
            ["engage", "hold", "set_sensors"]),
        new(
            SkillIds.Explain,
            "Explanation",
            [SkillLane.Read],
            []),
    ];

    /// <summary>Looks up a Slice A skill. <see cref="SkillIds.Submit"/> is a hard miss by design.</summary>
    public static bool TryGet(string skillId, out SkillDescriptor descriptor)
    {
        foreach (var row in SliceA)
        {
            if (string.Equals(row.SkillId, skillId, StringComparison.Ordinal))
            {
                descriptor = row;
                return true;
            }
        }

        descriptor = null!;
        return false;
    }
}
