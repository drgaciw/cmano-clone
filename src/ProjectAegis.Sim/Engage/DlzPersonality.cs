namespace ProjectAegis.Sim.Engage;

/// <summary>When inside the DLZ band the shooter may fire (req 14 P1 — does not change zone math).</summary>
public enum DlzPersonality
{
    Normal = 0,
    Early = 1,
    Late = 2,
}

public static class DlzPersonalityParser
{
    public static DlzPersonality Parse(string? value) =>
        Enum.TryParse<DlzPersonality>(value, ignoreCase: true, out var parsed)
            ? parsed
            : DlzPersonality.Normal;
}