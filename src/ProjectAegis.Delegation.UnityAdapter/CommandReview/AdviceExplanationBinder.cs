namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using System.Text.Json;
using ProjectAegis.Ai.Contracts;

/// <summary>
/// Presentation bind for host-published AdviceFrame explanations (ADR-010 client).
/// Does not write into <see cref="Skills.SkillEnvelope"/> or <see cref="Skills.ReplayProvenance"/>.
/// </summary>
public static class AdviceExplanationBinder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Binds a host transport artifact to the current advice selection.
    /// Late/stale artifacts and unavailable advice fail closed without mutating authority fields.
    /// </summary>
    public static AdviceExplanationPresentation Bind(
        AdviceFrame advice,
        string expectedInvocationKey,
        AiArtifactTransport? transport)
    {
        if (advice is null) throw new ArgumentNullException(nameof(advice));
        if (string.IsNullOrWhiteSpace(expectedInvocationKey))
            throw new ArgumentException("Invocation key is required.", nameof(expectedInvocationKey));

        if (advice.Availability != AdviceAvailability.Available)
        {
            return AdviceExplanationPresentation.Unavailable(
                expectedInvocationKey,
                advice.Availability,
                advice.Fallback,
                discardReason: "advice-" + advice.Availability.ToString().ToLowerInvariant());
        }

        if (transport is null)
        {
            return AdviceExplanationPresentation.Unavailable(
                expectedInvocationKey,
                AdviceAvailability.ModelUnavailable,
                advice.Fallback,
                discardReason: "explanation-unavailable");
        }

        if (transport.Envelope.Status != GenerationStatus.Succeeded)
        {
            return AdviceExplanationPresentation.Unavailable(
                expectedInvocationKey,
                AdviceAvailability.ModelUnavailable,
                advice.Fallback,
                discardReason: "generation-" + transport.Envelope.Status.ToString().ToLowerInvariant());
        }

        if (string.IsNullOrWhiteSpace(transport.ContentText)
            || !TryParseContent(transport.ContentText, out var content)
            || content is null)
        {
            return AdviceExplanationPresentation.Unavailable(
                expectedInvocationKey,
                AdviceAvailability.ModelUnavailable,
                advice.Fallback,
                discardReason: "malformed-explanation");
        }

        if (!string.Equals(content.InvocationKey, expectedInvocationKey, StringComparison.Ordinal))
        {
            return AdviceExplanationPresentation.Unavailable(
                expectedInvocationKey,
                AdviceAvailability.Stale,
                advice.Fallback,
                discardReason: "stale-invocation-key");
        }

        return new AdviceExplanationPresentation(
            InvocationKey: content.InvocationKey,
            Availability: AdviceAvailability.Available,
            Explanation: content.Explanation,
            EvidenceFingerprint: content.EvidenceFingerprint,
            ViewingSide: content.ViewingSide,
            CitedFactKeys: content.CitedFactKeys,
            PromptVersion: content.PromptVersion,
            DiscardReason: null,
            AuthorityPreserved: true);
    }

    private static bool TryParseContent(string json, out AdviceExplanationContent? content)
    {
        try
        {
            content = JsonSerializer.Deserialize<AdviceExplanationContent>(json, JsonOptions);
            return content is not null;
        }
        catch (JsonException)
        {
            content = null;
            return false;
        }
    }
}

/// <summary>Presentation-only explanation bind result for Command Review.</summary>
public sealed record AdviceExplanationPresentation(
    string InvocationKey,
    AdviceAvailability Availability,
    string Explanation,
    string? EvidenceFingerprint,
    ViewingSide? ViewingSide,
    IReadOnlyList<string> CitedFactKeys,
    string? PromptVersion,
    string? DiscardReason,
    bool AuthorityPreserved)
{
    /// <summary>Creates a fail-closed presentation bind.</summary>
    public static AdviceExplanationPresentation Unavailable(
        string invocationKey,
        AdviceAvailability availability,
        string fallback,
        string discardReason) =>
        new(
            InvocationKey: invocationKey,
            Availability: availability,
            Explanation: string.Empty,
            EvidenceFingerprint: null,
            ViewingSide: null,
            CitedFactKeys: Array.Empty<string>(),
            PromptVersion: null,
            DiscardReason: discardReason,
            AuthorityPreserved: true);
}
