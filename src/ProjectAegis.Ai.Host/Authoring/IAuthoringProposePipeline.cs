namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>Propose-lane authoring pipeline (fake/scripted drafts only).</summary>
public interface IAuthoringProposePipeline
{
    /// <summary>
    /// Converts a scripted model draft into typed Propose-lane results.
    /// Never submits; never calls paid providers.
    /// </summary>
    AuthoringProposeResult Propose(AuthoringProposeRequest request, AuthoringModelDraft draft);
}
