namespace ProjectAegis.Ai.LiveEval;

/// <summary>
/// Hard stop before any paid provider or judge-model call.
/// Does not read or log secret values — only presence of env names.
/// </summary>
public static class PaidProviderGate
{
    /// <summary>Env switch that must be set to <c>1</c> before paid mode is considered.</summary>
    public const string AllowPaidEnvName = "AEGIS_LIVE_EVAL_ALLOW_PAID";

    /// <summary>
    /// Returns false (with reason) unless allow switch is set. Never returns secret material.
    /// </summary>
    public static bool TryAuthorizePaidMode(out string denyReason)
    {
        var allow = Environment.GetEnvironmentVariable(AllowPaidEnvName);
        if (!string.Equals(allow, "1", StringComparison.Ordinal))
        {
            denyReason =
                "Paid LiveEval blocked: set " + AllowPaidEnvName
                + "=1 only after owner budget approval. Default mode is Fake ($0).";
            return false;
        }

        // Credentials may exist, but adapters are not wired in this scaffold PR.
        denyReason =
            "Paid LiveEval blocked: provider adapter not wired in this scaffold. "
            + "Owner must approve provider/model/budget before any spend.";
        return false;
    }

    /// <summary>Throws if paid mode is requested.</summary>
    public static void EnsureFakeOrThrow(LiveEvalMode mode)
    {
        if (mode == LiveEvalMode.Fake)
        {
            return;
        }

        _ = TryAuthorizePaidMode(out var reason);
        throw new InvalidOperationException(reason);
    }
}
