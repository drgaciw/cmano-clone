namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>
/// Scripted / fake-client model draft. Never a live provider response in this slice.
/// Provider/model/budget remain owner-TBD.
/// </summary>
public sealed record AuthoringModelDraft(
    string ScriptedStatus,
    IReadOnlyList<AuthoringProposedOperation> ProposedOperations,
    string? ClarificationPrompt = null,
    IReadOnlyList<string>? ForbiddenInOutput = null);
