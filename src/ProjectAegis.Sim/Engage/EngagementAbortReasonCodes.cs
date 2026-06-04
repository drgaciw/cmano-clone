namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Glossary;

/// <summary>Stable order-log codes for engagement aborts (doc 14 / order-log-replay).</summary>
public static class EngagementAbortReasonCodes
{
    public const string Launched = "Launched";

    public const string NoResult = "NoResult";

    private static readonly AbortReasonManifest Manifest = AbortReasonManifest.LoadFromEmbeddedOrFile();

    public static string ToLogCode(EngagementAbortReason reason)
    {
        if (reason == EngagementAbortReason.None)
        {
            return "Unknown";
        }

        return Manifest.GetLogCode("Engage", reason);
    }
}