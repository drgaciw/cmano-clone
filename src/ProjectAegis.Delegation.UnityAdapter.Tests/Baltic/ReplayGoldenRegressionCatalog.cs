namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>Pinned Baltic replay cases under tests/regression/ (req 17 CI gate).</summary>
public static class ReplayGoldenRegressionCatalog
{
    public sealed record Case(
        string GoldenFile,
        int Seed,
        string PolicyId,
        int Ticks,
        bool MvpEngagement = true,
        params string[] FingerprintMustContain);

    public static readonly Case[] All =
    [
        new(
            "replay-golden-baltic-engage-2026-06-02.txt",
            42,
            "baltic-patrol",
            4,
            MvpEngagement: true,
            "EngagementOutcome|",
            "|Kill|"),
        new(
            "replay-golden-baltic-comms-2026-06-02.txt",
            42,
            "baltic-patrol-comms",
            6,
            MvpEngagement: true,
            "CommsStateChange",
            "CommsDenied"),
        new(
            "replay-golden-baltic-classify-2026-06-02.txt",
            42,
            "baltic-patrol-classify",
            4,
            MvpEngagement: false,
            "|Detected|Classified",
            "|Classified|Identified"),
        new(
            "replay-golden-baltic-stale-2026-06-04.txt",
            11,
            "baltic-patrol-stale",
            3,
            MvpEngagement: false,
            "|Detected|Lost"),
    ];
}