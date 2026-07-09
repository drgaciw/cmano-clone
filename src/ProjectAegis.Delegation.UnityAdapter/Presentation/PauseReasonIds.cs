namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Req 20 P0 Phase 0: canonical pause-reason ids for <see cref="PauseReasonStack"/> (doc 03 multitasker).
/// Sim UI tick runs only when the stack is empty. Tracks push/pop these constants — do not invent ad-hoc ids.
/// </summary>
public static class PauseReasonIds
{
    /// <summary>Manual user pause (space / pause control).</summary>
    public const string User = "user";

    /// <summary>Multitasker bookmark capture/restore holds sim.</summary>
    public const string MultitaskerBookmark = "multitasker_bookmark";

    /// <summary>Critical-tier auto-pause (TR-c2-007 / <c>AutoPauseCommand</c>).</summary>
    public const string AutoPauseSeverity = "auto_pause_severity";

    /// <summary>Agent pause gate (ADR-019 / T4 → T5 stack).</summary>
    public const string AgentGate = "agent_gate";
}
