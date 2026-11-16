namespace ProjectAegis.Ai.LiveEval;

/// <summary>Evaluation transport. Paid mode is gated and disabled by default.</summary>
public enum LiveEvalMode
{
    /// <summary>Offline fake clients only — zero spend.</summary>
    Fake = 0,

    /// <summary>Real provider — requires owner budget + credentials. Not implemented in scaffold.</summary>
    PaidProvider = 1,
}
