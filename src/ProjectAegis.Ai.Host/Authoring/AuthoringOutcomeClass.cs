namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>Outcome class for Propose-lane authoring (DRG-740 / fixture pack).</summary>
public enum AuthoringOutcomeClass
{
    ValidDraft,
    Diagnostics,
    Conflict,
    IdempotentNoOp,
}
